using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Restaurant.Api.Controllers.Ai;

// ==============================================================
// DTOs CHO TRỢ LÝ AI TƯ VẤN MÓN ĂN (GIAI ĐOẠN 7 - M14-R2)
// ==============================================================

public class AiChatRequest
{
    public int? ConversationId { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? ConversationToken { get; set; }
}

public class RecommendedFoodDto
{
    public int FoodId { get; set; }
    public string FoodName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal MinPrice { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public string FoodStatus { get; set; } = "available";
}

[Route("api/customer/ai")]
[ApiController]
public class CustomerAiController : ControllerBase
{
    private readonly RestaurantDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDataProtector _guestProtector;

    public CustomerAiController(
        RestaurantDbContext dbContext,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory, IDataProtectionProvider dataProtection)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _guestProtector = dataProtection.CreateProtector("Restaurant.Ai.GuestConversation.v1");
    }

    private int? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out var userId) ? userId : null;
    }

    /// <summary>
    /// Gửi tin nhắn tới trợ lý AI tư vấn món ăn Hương Sen (M14-R2 / UC-E06)
    /// </summary>
    [HttpPost("chat")]
    [AllowAnonymous]
    public async Task<IActionResult> Chat([FromBody] AiChatRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { success = false, message = "Vui lòng nhập nội dung cần tư vấn." });

        var userMessage = request.Message.Trim();
        var currentUserId = GetCurrentUserId();

        // 1. Xác định Customer liên kết nếu có
        int? customerId = currentUserId.HasValue
            ? await _dbContext.Customers.Where(customer => customer.UserId == currentUserId.Value)
                .Select(customer => (int?)customer.CustomerId).FirstOrDefaultAsync() : null;
        if (request.CustomerId.HasValue && request.CustomerId != customerId) return Forbid();

        // 2. Tìm hoặc tạo mới cuộc trò chuyện AiConversation
        AiConversation? conversation = null;
        if (request.ConversationId.HasValue && request.ConversationId.Value > 0)
        {
            conversation = await _dbContext.AiConversations
                .Include(c => c.AiMessages)
                .FirstOrDefaultAsync(c => c.ConversationId == request.ConversationId.Value);
            if (conversation == null) return NotFound();
            if (!CanAccessConversation(conversation, customerId, request.ConversationToken)) return Forbid();
        }

        if (conversation == null)
        {
            var title = userMessage.Length > 40 ? userMessage.Substring(0, 37) + "..." : userMessage;
            conversation = new AiConversation
            {
                CustomerId = customerId,
                Title = title,
                ConversationStatus = "active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.AiConversations.Add(conversation);
            await _dbContext.SaveChangesAsync();
        }

        // 3. Lưu tin nhắn của khách vào AiMessages
        var customerMsgRecord = new AiMessage
        {
            ConversationId = conversation.ConversationId,
            SenderType = "customer",
            Content = userMessage,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.AiMessages.Add(customerMsgRecord);
        await _dbContext.SaveChangesAsync();

        // 4. Lấy thực đơn thực tế đang bán từ database để làm cơ sở tri thức (Grounding)
        var activeFoods = await _dbContext.Foods
            .Include(f => f.Category)
            .Include(f => f.FoodVariants.Where(v => v.IsActive))
            .Where(f => f.IsActive && f.FoodStatus == "available")
            .ToListAsync();

        // 5. Thử gọi Google Gemini API nếu có cấu hình API Key
        string? geminiApiKey = _configuration["Gemini:ApiKey"]
            ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        string replyText = string.Empty;
        List<int> recommendedFoodIds = new();

        bool geminiSuccess = false;
        if (!string.IsNullOrWhiteSpace(geminiApiKey))
        {
            try
            {
                var geminiResult = await CallGeminiApiAsync(geminiApiKey, userMessage, activeFoods);
                if (!string.IsNullOrWhiteSpace(geminiResult.Reply))
                {
                    replyText = geminiResult.Reply;
                    recommendedFoodIds = geminiResult.FoodIds;
                    geminiSuccess = true;
                }
            }
            catch
            {
                geminiSuccess = false;
            }
        }

        // 6. Nếu không có key hoặc API lỗi, sử dụng Bộ xử lý tư vấn ẩm thực thông minh chuyên biệt (Rule & Heuristic Engine)
        if (!geminiSuccess || string.IsNullOrWhiteSpace(replyText))
        {
            var heuristicResult = ProcessHeuristicRecommendation(userMessage, activeFoods);
            replyText = heuristicResult.Reply;
            recommendedFoodIds = heuristicResult.FoodIds;
        }

        // 7. Lưu phản hồi AI vào DB
        var aiMsgRecord = new AiMessage
        {
            ConversationId = conversation.ConversationId,
            SenderType = "ai",
            Content = replyText,
            RecommendedFoodIds = recommendedFoodIds.Any() ? string.Join(",", recommendedFoodIds) : null,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.AiMessages.Add(aiMsgRecord);
        conversation.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        // 8. Lấy chi tiết các món ăn được đề xuất
        var recommendedFoodDetails = activeFoods
            .Where(f => recommendedFoodIds.Contains(f.FoodId))
            .Select(f => new RecommendedFoodDto
            {
                FoodId = f.FoodId,
                FoodName = f.FoodName,
                CategoryName = f.Category?.CategoryName ?? "Món ngon",
                MinPrice = f.MinPrice,
                ImageUrl = f.ImageUrl,
                Description = f.Description,
                FoodStatus = f.FoodStatus
            })
            .ToList();

        return Ok(new
        {
            success = true,
            data = new
            {
                conversationId = conversation.ConversationId,
                conversationToken = customerId == null ? _guestProtector.Protect(conversation.ConversationId.ToString()) : null,
                title = conversation.Title,
                reply = replyText,
                recommendedFoods = recommendedFoodDetails,
                createdAt = aiMsgRecord.CreatedAt
            }
        });
    }

    /// <summary>
    /// Lấy lịch sử tin nhắn của một cuộc hội thoại
    /// </summary>
    [HttpGet("conversations/{id}/messages")]
    [AllowAnonymous]
    public async Task<IActionResult> GetConversationMessages(int id)
    {
        var conversation = await _dbContext.AiConversations
            .Include(c => c.AiMessages)
            .FirstOrDefaultAsync(c => c.ConversationId == id);

        if (conversation == null)
            return NotFound(new { success = false, message = "Không tìm thấy cuộc hội thoại." });
        var userId = GetCurrentUserId();
        var customerId = userId.HasValue ? await _dbContext.Customers.Where(customer => customer.UserId == userId.Value)
            .Select(customer => (int?)customer.CustomerId).FirstOrDefaultAsync() : null;
        if (!CanAccessConversation(conversation, customerId, Request.Headers["X-Conversation-Token"])) return Forbid();

        var foodIdsInConv = conversation.AiMessages
            .Where(m => !string.IsNullOrEmpty(m.RecommendedFoodIds))
            .SelectMany(m => m.RecommendedFoodIds!.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(s => int.TryParse(s, out var fId) ? fId : 0)
            .Where(fId => fId > 0)
            .Distinct()
            .ToList();

        var foodsDict = await _dbContext.Foods
            .Include(f => f.Category)
            .Where(f => foodIdsInConv.Contains(f.FoodId))
            .ToDictionaryAsync(f => f.FoodId, f => new RecommendedFoodDto
            {
                FoodId = f.FoodId,
                FoodName = f.FoodName,
                CategoryName = f.Category?.CategoryName ?? "",
                MinPrice = f.MinPrice,
                ImageUrl = f.ImageUrl,
                Description = f.Description,
                FoodStatus = f.FoodStatus
            });

        var messages = conversation.AiMessages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new
            {
                m.MessageId,
                m.SenderType,
                m.Content,
                RecommendedFoods = !string.IsNullOrEmpty(m.RecommendedFoodIds)
                    ? m.RecommendedFoodIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s, out var id) && foodsDict.ContainsKey(id) ? foodsDict[id] : null)
                        .Where(f => f != null)
                        .ToList()
                    : new List<RecommendedFoodDto?>(),
                m.CreatedAt
            });

        return Ok(new
        {
            success = true,
            data = new
            {
                conversation.ConversationId,
                conversation.Title,
                conversation.CreatedAt,
                messages
            }
        });
    }

    /// <summary>
    /// Lấy danh sách các cuộc hội thoại gần đây
    /// </summary>
    [HttpGet("conversations")]
    [Authorize(Roles = "customer")]
    public async Task<IActionResult> GetConversations([FromQuery] int? customerId = null)
    {
        var currentUserId = GetCurrentUserId();
        var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.UserId == currentUserId);
        if (customer == null || (customerId.HasValue && customerId != customer.CustomerId)) return Forbid();
        var query = _dbContext.AiConversations.Where(c => c.CustomerId == customer.CustomerId);

        var conversations = await query
            .OrderByDescending(c => c.UpdatedAt)
            .Take(20)
            .Select(c => new
            {
                c.ConversationId,
                c.Title,
                c.ConversationStatus,
                c.CreatedAt,
                c.UpdatedAt,
                MessageCount = c.AiMessages.Count
            })
            .ToListAsync();

        return Ok(new { success = true, data = conversations });
    }

    // ==============================================================
    // HELPER: GỌI GOOGLE GEMINI API
    // ==============================================================
    private bool CanAccessConversation(AiConversation conversation, int? customerId, string? guestToken)
    {
        if (conversation.CustomerId.HasValue) return customerId == conversation.CustomerId;
        if (string.IsNullOrWhiteSpace(guestToken)) return false;
        try { return _guestProtector.Unprotect(guestToken) == conversation.ConversationId.ToString(); }
        catch (System.Security.Cryptography.CryptographicException) { return false; }
    }

    private async Task<(string Reply, List<int> FoodIds)> CallGeminiApiAsync(
        string apiKey,
        string userMessage,
        List<Food> foods)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(15);

        // Tạo context thực đơn phong phú
        var menuSummary = string.Join("\n", foods.Select(f =>
            $"- [Mã: {f.FoodId}] {f.FoodName} (Danh mục: {f.Category?.CategoryName}, Giá từ: {f.MinPrice:N0}đ): {f.Description}"));

        var systemPrompt = $@"Bạn là Trợ Lý Ẩm Thực AI thông minh và thân thiện của Nhà Hàng Hương Sen.
Dưới đây là thực đơn hiện có sẵn của nhà hàng:
{menuSummary}

Nhiệm vụ của bạn:
1. Tư vấn món ăn dựa theo yêu cầu của khách hàng (số lượng người, sở thích cay/ngọt/chay/hải sản, ngân sách, dịp tiệc...).
2. Chỉ gợi ý các món ăn có trong danh sách thực đơn trên.
3. Giọng điệu ấm cúng, lịch thiệp, chuyên nghiệp của nhà hàng Hương Sen.
4. Ở cuối câu trả lời, hãy đính kèm thẻ món gợi ý theo định dạng: [RECOMMENDED_IDS: 1, 2, 3] với các số là mã món FoodId bạn khuyên dùng.";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = systemPrompt + "\n\nKhách hàng hỏi: " + userMessage }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.7,
                maxOutputTokens = 800
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
        var response = await client.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
            return (string.Empty, new List<int>());

        var responseString = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseString);
        var candidates = doc.RootElement.GetProperty("candidates");
        if (candidates.GetArrayLength() == 0) return (string.Empty, new List<int>());

        var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";

        // Trích xuất mã món từ thẻ [RECOMMENDED_IDS: ...]
        var foodIds = new List<int>();
        var marker = "[RECOMMENDED_IDS:";
        var idx = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            var endIdx = text.IndexOf(']', idx);
            if (endIdx > idx)
            {
                var idPart = text.Substring(idx + marker.Length, endIdx - (idx + marker.Length));
                foreach (var s in idPart.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(s, out var fid) && foods.Any(f => f.FoodId == fid))
                    {
                        foodIds.Add(fid);
                    }
                }
                text = text.Substring(0, idx).Trim();
            }
        }

        return (text, foodIds);
    }

    // ==============================================================
    // HELPER: CƠ CHẾ GỢI Ý ẨM THỰC THÔNG MINH (RULE & HEURISTIC ENGINE)
    // ==============================================================
    private (string Reply, List<int> FoodIds) ProcessHeuristicRecommendation(
        string message,
        List<Food> foods)
    {
        var lower = message.ToLower();
        var selected = new List<Food>();
        var replySb = new StringBuilder();

        // 1. Phân loại theo nhu cầu
        if (lower.Contains("chay") || lower.Contains("thanh đạm") || lower.Contains("rau") || lower.Contains("nấm"))
        {
            selected = foods.Where(f =>
                f.FoodName.ToLower().Contains("nấm") ||
                f.FoodName.ToLower().Contains("rau") ||
                f.FoodName.ToLower().Contains("chay") ||
                f.FoodName.ToLower().Contains("đậu") ||
                (f.Category?.CategoryName.ToLower().Contains("chay") ?? false)
            ).Take(3).ToList();

            replySb.AppendLine("Chào bạn! Nếu bạn đang tìm kiếm các món thanh đạm, tốt cho sức khỏe hoặc ăn chay nhẹ nhàng, Hương Sen xin trân trọng gợi ý các món sau:");
        }
        else if (lower.Contains("lẩu") || lower.Contains("nước lẩu") || lower.Contains("ấm"))
        {
            selected = foods.Where(f =>
                f.FoodName.ToLower().Contains("lẩu") ||
                (f.Category?.CategoryName.ToLower().Contains("lẩu") ?? false)
            ).Take(3).ToList();

            replySb.AppendLine("Chào bạn! Nhà hàng Hương Sen nổi tiếng với các món Lẩu thanh ngọt, đậm vị tự nhiên từ nước hầm xương và hải sản tươi rói. Bạn có thể thưởng thức:");
        }
        else if (lower.Contains("hải sản") || lower.Contains("tôm") || lower.Contains("mực") || lower.Contains("cá") || lower.Contains("cua"))
        {
            selected = foods.Where(f =>
                f.FoodName.ToLower().Contains("tôm") ||
                f.FoodName.ToLower().Contains("mực") ||
                f.FoodName.ToLower().Contains("cá") ||
                f.FoodName.ToLower().Contains("hải sản")
            ).Take(3).ToList();

            replySb.AppendLine("Hương Sen sử dụng nguồn hải sản tươi ngon nhập mới mỗi ngày. Dưới đây là những món hải sản best-seller rất được thực khách ưa chuộng:");
        }
        else if (lower.Contains("bò") || lower.Contains("thịt") || lower.Contains("nướng"))
        {
            selected = foods.Where(f =>
                f.FoodName.ToLower().Contains("bò") ||
                f.FoodName.ToLower().Contains("nướng") ||
                f.FoodName.ToLower().Contains("gà")
            ).Take(3).ToList();

            replySb.AppendLine("Nếu bạn là tín đồ của các món thịt nướng thơm lừng và đậm đà gia vị truyền thống, bạn nhất định không nên bỏ qua:");
        }
        else if (lower.Contains("khai vị") || lower.Contains("nhẹ") || lower.Contains("gỏi") || lower.Contains("salad"))
        {
            selected = foods.Where(f =>
                (f.Category?.CategoryName.ToLower().Contains("khai vị") ?? false) ||
                f.FoodName.ToLower().Contains("gỏi") ||
                f.FoodName.ToLower().Contains("salad")
            ).Take(3).ToList();

            replySb.AppendLine("Để khởi đầu một bữa tiệc hoàn hảo, các món khai vị thanh mát sẽ kích thích vị giác tuyệt vời:");
        }
        else if (lower.Contains("uống") || lower.Contains("nước") || lower.Contains("trà") || lower.Contains("bia"))
        {
            selected = foods.Where(f =>
                (f.Category?.CategoryName.ToLower().Contains("uống") ?? false) ||
                (f.Category?.CategoryName.ToLower().Contains("đồ uống") ?? false) ||
                f.FoodName.ToLower().Contains("trà") ||
                f.FoodName.ToLower().Contains("nước")
            ).Take(3).ToList();

            replySb.AppendLine("Nhà hàng có thực đơn đồ uống và trà thanh nhiệt phong phú giúp bữa ăn thêm trọn vẹn:");
        }
        else if (lower.Contains("4 người") || lower.Contains("gia đình") || lower.Contains("đông") || lower.Contains("nhóm"))
        {
            // Combo gợi ý: 1 khai vị, 2 món chính, 1 lẩu
            var khaiVi = foods.FirstOrDefault(f => f.Category?.CategoryName.ToLower().Contains("khai vị") ?? false);
            var lau = foods.FirstOrDefault(f => f.FoodName.ToLower().Contains("lẩu"));
            var monChinh = foods.Where(f => f != khaiVi && f != lau).Take(2).ToList();

            if (khaiVi != null) selected.Add(khaiVi);
            selected.AddRange(monChinh);
            if (lau != null) selected.Add(lau);

            replySb.AppendLine("Dạ, đối với nhóm đi 3-5 người, Hương Sen xin gợi ý một set ăn hài hòa gồm món khai vị nhẹ nhàng, món nướng/xào đặc trưng và một nồi lẩu ấm cúng để mọi người cùng quây quần:");
        }
        else
        {
            // Mặc định: gợi ý các món đặc sản / tiêu biểu nhất của nhà hàng
            selected = foods.Take(3).ToList();
            replySb.AppendLine("Chào mừng quý khách đến với Nhà Hàng Hương Sen! Dưới đây là những món ăn đặc sắc nhất trong thực đơn hôm nay mà đầu bếp của chúng tôi gợi ý cho bạn:");
        }

        // Nếu danh sách lọc được bị trống, lấy các món đầu tiên
        if (!selected.Any())
        {
            selected = foods.Take(3).ToList();
        }

        foreach (var item in selected)
        {
            replySb.AppendLine($"• **{item.FoodName}** ({item.Category?.CategoryName}) - Giá từ: **{item.MinPrice:N0}đ**");
            if (!string.IsNullOrWhiteSpace(item.Description))
            {
                replySb.AppendLine($"  _{item.Description}_");
            }
        }

        replySb.AppendLine("\nBạn có thể nhấn vào các món bên dưới để xem chi tiết hoặc đặt bàn ngay để nhà hàng chuẩn bị chu đáo nhất nhé! Chúc bạn có một bữa ăn ngon miệng ❤️");

        return (replySb.ToString(), selected.Select(f => f.FoodId).ToList());
    }
}
