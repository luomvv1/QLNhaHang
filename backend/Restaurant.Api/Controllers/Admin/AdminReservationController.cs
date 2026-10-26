using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;
using Restaurant.Api.Hubs;
using System.Security.Claims;

namespace Restaurant.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/reservations")]
[Authorize(Roles = "staff,manager,admin")]
public class AdminReservationController : ControllerBase
{
    private readonly RestaurantDbContext _dbContext;
    private readonly IHubContext<RestaurantHub> _hubContext;

    public AdminReservationController(RestaurantDbContext dbContext, IHubContext<RestaurantHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    private int GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out var userId) ? userId : 1;
    }

    /// <summary>
    /// Danh sách phiếu đặt bàn kèm bộ lọc ngày, trạng thái, tìm kiếm và thẻ thống kê
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetReservations(
        [FromQuery] string? date = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null)
    {
        var query = _dbContext.Reservations
            .Include(r => r.Table)
            .Include(r => r.Customer)
            .Include(r => r.ConfirmedByNavigation)
            .AsQueryable();

        // 1. Lọc theo ngày
        if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var parsedDate))
        {
            var start = parsedDate.Date;
            var end = start.AddDays(1);
            query = query.Where(r => r.ReservationDate >= start && r.ReservationDate < end);
        }

        // 2. Lọc theo trạng thái
        if (!string.IsNullOrWhiteSpace(status) && status.ToLower() != "all")
        {
            var stClean = status.Trim().ToLower();
            query = query.Where(r => r.ReservationStatus.ToLower() == stClean);
        }

        // 3. Tìm kiếm theo tên hoặc SĐT
        if (!string.IsNullOrWhiteSpace(search))
        {
            var sClean = search.Trim();
            query = query.Where(r => r.CustomerName.Contains(sClean) || r.CustomerPhone.Contains(sClean));
        }

        var items = await query
            .OrderBy(r => r.ReservationDate)
            .Select(r => new
            {
                r.ReservationId,
                r.CustomerId,
                CustomerPoints = r.Customer.PointsBalance,
                CustomerTotalSpent = r.Customer.TotalSpent,
                r.CustomerName,
                r.CustomerPhone,
                r.ReservationDate,
                r.GuestCount,
                r.Note,
                r.ReservationStatus,
                r.ConfirmedBy,
                ConfirmedByName = r.ConfirmedByNavigation != null ? r.ConfirmedByNavigation.FullName : null,
                r.CreatedAt,
                r.UpdatedAt,
                Table = r.Table != null ? new
                {
                    r.Table.TableId,
                    r.Table.TableCode,
                    r.Table.TableName,
                    r.Table.AreaName,
                    r.Table.Capacity,
                    r.Table.TableStatus
                } : null
            })
            .ToListAsync();

        // Thống kê nhanh theo ngày được chọn (hoặc toàn bộ nếu không lọc theo ngày)
        DateTime? statTargetDate = null;
        if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var parsedStat))
        {
            statTargetDate = parsedStat.Date;
        }

        var statsQuery = _dbContext.Reservations.AsQueryable();
        if (statTargetDate.HasValue)
        {
            var sStart = statTargetDate.Value;
            var sEnd = sStart.AddDays(1);
            statsQuery = statsQuery.Where(r => r.ReservationDate >= sStart && r.ReservationDate < sEnd);
        }

        var statList = await statsQuery
            .Select(r => r.ReservationStatus)
            .ToListAsync();

        var stats = new
        {
            totalToday = statList.Count, // Giữ tên totalToday để tương thích ngược
            total = statList.Count,
            pendingCount = statList.Count(s => s == "pending"),
            confirmedCount = statList.Count(s => s == "confirmed"),
            checkedInCount = statList.Count(s => s == "checked_in"),
            cancelledCount = statList.Count(s => s == "cancelled" || s == "rejected" || s == "no_show"),
            completedCount = statList.Count(s => s == "completed"),
            date = statTargetDate.HasValue ? statTargetDate.Value.ToString("yyyy-MM-dd") : "all"
        };

        return Ok(new { success = true, data = items, stats });
    }

    /// <summary>
    /// Lấy danh sách bàn ăn khả dụng để phục vụ xếp bàn, có lọc và kiểm tra tình trạng theo ngày hẹn của phiếu
    /// </summary>
    [HttpGet("tables")]
    public async Task<IActionResult> GetTablesForReservation(
        [FromQuery] string? date = null,
        [FromQuery] int? excludeReservationId = null)
    {
        DateTime? targetDate = null;
        if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var parsed))
        {
            targetDate = parsed.Date;
        }

        // Tìm các bàn đã được gán cho các phiếu đặt bàn khác trong cùng ngày targetDate
        var bookedTableMap = new Dictionary<int, string>();
        if (targetDate.HasValue)
        {
            var start = targetDate.Value;
            var end = start.AddDays(1);
            var queryOther = _dbContext.Reservations
                .Where(r => r.ReservationDate >= start && r.ReservationDate < end &&
                            (r.ReservationStatus == "confirmed" || r.ReservationStatus == "checked_in"));

            if (excludeReservationId.HasValue && excludeReservationId.Value > 0)
            {
                queryOther = queryOther.Where(r => r.ReservationId != excludeReservationId.Value);
            }

            var otherReservations = await queryOther.ToListAsync();
            foreach (var r in otherReservations)
            {
                var bookerInfo = $"{r.CustomerName} ({r.ReservationDate:HH:mm})";
                if (r.TableId.HasValue)
                {
                    bookedTableMap[r.TableId.Value] = bookerInfo;
                }

                if (!string.IsNullOrWhiteSpace(r.Note) && r.Note.Contains("MergedTableIds:"))
                {
                    try
                    {
                        var marker = "MergedTableIds:";
                        var sIdx = r.Note.IndexOf(marker) + marker.Length;
                        var eIdx = r.Note.IndexOf("]", sIdx);
                        var rawIds = (eIdx > sIdx ? r.Note.Substring(sIdx, eIdx - sIdx) : r.Note.Substring(sIdx)).Split(',');
                        foreach (var idStr in rawIds)
                        {
                            if (int.TryParse(idStr.Trim(), out var parsedId))
                            {
                                bookedTableMap[parsedId] = bookerInfo;
                            }
                        }
                    }
                    catch { }
                }
            }
        }

        var isToday = !targetDate.HasValue || targetDate.Value == DateTime.Today;

        var tables = await _dbContext.DiningTables
            .Where(t => t.IsActive && t.TableStatus != "disabled" && t.TableStatus != "maintenance")
            .OrderBy(t => t.AreaName)
            .ThenBy(t => t.TableCode)
            .ToListAsync();

        var result = tables.Select(t =>
        {
            var isBooked = bookedTableMap.ContainsKey(t.TableId);
            var bookedBy = isBooked ? bookedTableMap[t.TableId] : null;

            // Trạng thái của bàn vào ngày hẹn:
            // 1. Nếu ngày đó đã có khách đặt: "booked"
            // 2. Nếu ngày đó là hôm nay: lấy TableStatus thực tế của hôm nay (empty, serving, reserved)
            // 3. Nếu ngày đó là ngày tương lai (ngày mai...): nếu chưa ai đặt vào ngày đó thì bàn đó là "empty" (khả dụng)
            string statusOnDate;
            if (isBooked)
            {
                statusOnDate = "booked";
            }
            else if (isToday)
            {
                statusOnDate = t.TableStatus;
            }
            else
            {
                statusOnDate = "empty";
            }

            return new
            {
                t.TableId,
                t.TableCode,
                t.TableName,
                t.AreaName,
                t.Capacity,
                RealtimeStatus = t.TableStatus,
                TableStatus = statusOnDate, // Trạng thái tính theo ngày hẹn
                IsBookedOnDate = isBooked,
                BookedBy = bookedBy,
                TargetDate = targetDate?.ToString("yyyy-MM-dd")
            };
        }).ToList();

        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Quản lý / Staff tạo phiếu đặt bàn cho khách gọi điện hotline
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> CreateReservation([FromBody] AdminCreateReservationRequest request)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });

        if (string.IsNullOrWhiteSpace(request.CustomerName))
            return BadRequest(new { success = false, message = "Vui lòng nhập tên khách hàng." });

        if (string.IsNullOrWhiteSpace(request.CustomerPhone))
            return BadRequest(new { success = false, message = "Vui lòng nhập số điện thoại." });

        if (request.GuestCount <= 0)
            return BadRequest(new { success = false, message = "Số lượng khách phải lớn hơn 0." });

        var phoneClean = request.CustomerPhone.Trim();
        var nameClean = request.CustomerName.Trim();

        // Tìm hoặc tạo Customer
        var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Phone == phoneClean);
        if (customer == null)
        {
            customer = new Entities.Customer
            {
                FullName = nameClean,
                Phone = phoneClean,
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                PointsBalance = 0,
                TotalSpent = 0,
                VisitCount = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Customers.Add(customer);
            await _dbContext.SaveChangesAsync();
        }

        var reservation = new Reservation
        {
            CustomerId = customer.CustomerId,
            TableId = null,
            CustomerName = nameClean,
            CustomerPhone = phoneClean,
            ReservationDate = request.ReservationDate,
            GuestCount = request.GuestCount,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            ReservationStatus = "pending",
            ConfirmedBy = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Reservations.Add(reservation);
        await _dbContext.SaveChangesAsync();

        // Gửi SignalR
        try
        {
            await _hubContext.PublishAsync(_dbContext, "ReservationUpdated", new { reservationId = reservation.ReservationId, eventType = "created" });
        }
        catch { }

        return Ok(new
        {
            success = true,
            message = "Tạo phiếu đặt bàn thành công.",
            data = new { reservationId = reservation.ReservationId }
        });
    }

    /// <summary>
    /// Xác nhận nhà hàng tiếp nhận lịch đặt. Bước này chưa phân bàn.
    /// </summary>
    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> ConfirmReservation(int id)
    {
        var reservation = await _dbContext.Reservations.FirstOrDefaultAsync(r => r.ReservationId == id);
        if (reservation == null)
            return NotFound(new { success = false, message = "Không tìm thấy phiếu đặt bàn này." });

        if (reservation.ReservationStatus != "pending")
            return BadRequest(new { success = false, message = "Chỉ phiếu đang chờ xác nhận mới có thể được duyệt." });

        reservation.ReservationStatus = "confirmed";
        reservation.ConfirmedBy = GetCurrentUserId();
        reservation.UpdatedAt = DateTime.UtcNow;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = GetCurrentUserId(),
            ActionName = "reservation_confirmed",
            EntityName = "Reservation",
            EntityId = reservation.ReservationId.ToString(),
            OldData = System.Text.Json.JsonSerializer.Serialize(new { status = "pending" }),
            NewData = System.Text.Json.JsonSerializer.Serialize(new {
                status = "confirmed",
                customerName = reservation.CustomerName,
                phone = reservation.CustomerPhone,
                guestCount = reservation.GuestCount,
                reservationDate = reservation.ReservationDate
            }),
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();

        try
        {
            await _hubContext.PublishAsync(_dbContext, "ReservationUpdated", new { reservationId = id, eventType = "confirmed" });
        }
        catch { }

        return Ok(new { success = true, message = $"Đã xác nhận lịch đặt bàn của khách {reservation.CustomerName}." });
    }

    /// <summary>
    /// Phân bàn cho phiếu đã được xác nhận và chuyển bàn sang reserved.
    /// </summary>
    /// <summary>
    /// Phân bàn hoặc ghép nhiều bàn cho phiếu đã được xác nhận.
    /// Tự động nhận diện bàn trống -> chuyển sang reserved, bàn đang phục vụ -> giữ nguyên serving.
    /// Không bắt buộc phân biệt chính phụ, chỉ cần chọn bàn là hệ thống tự xử lý.
    /// </summary>
    [HttpPost("{id}/assign-table")]
    public async Task<IActionResult> AssignTable(int id, [FromBody] AssignTableRequest request)
    {
        var reservation = await _dbContext.Reservations.Include(r => r.Table).FirstOrDefaultAsync(r => r.ReservationId == id);
        if (reservation == null)
            return NotFound(new { success = false, message = "Không tìm thấy phiếu đặt bàn này." });

        if (reservation.ReservationStatus != "confirmed")
            return BadRequest(new { success = false, message = "Vui lòng xác nhận phiếu đặt bàn trước khi phân bàn." });

        // Gom danh sách ID các bàn được chọn
        var targetIds = request.TableIds != null && request.TableIds.Count > 0
            ? request.TableIds.Distinct().ToList()
            : (request.TableId.HasValue && request.TableId.Value > 0 ? new List<int> { request.TableId.Value } : new List<int>());

        if (targetIds.Count == 0)
            return BadRequest(new { success = false, message = "Vui lòng chọn ít nhất 1 bàn ăn." });

        var tables = await _dbContext.DiningTables
            .Where(t => targetIds.Contains(t.TableId) && t.IsActive)
            .OrderBy(t => t.TableCode)
            .ToListAsync();

        if (tables.Count != targetIds.Count)
            return BadRequest(new { success = false, message = "Một số bàn ăn không tồn tại hoặc đã ngừng hoạt động." });

        var areas = tables
            .Select(table => table.AreaName?.Trim() ?? string.Empty)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (areas.Count > 1)
            return BadRequest(new { success = false, message = "Chỉ được gộp bàn trong cùng khu vực. Vui lòng bỏ chọn bàn khác khu vực." });

        // KIỂM TRA TRÙNG LỊCH THEO NGÀY HẸN CỦA PHIẾU
        var targetDate = reservation.ReservationDate.Date;
        var start = targetDate;
        var end = start.AddDays(1);
        var isToday = targetDate == DateTime.UtcNow.AddHours(7).Date;

        // Tìm các phiếu khác trong cùng ngày targetDate
        var sameDayReservations = await _dbContext.Reservations
            .Where(r => r.ReservationId != id &&
                        r.ReservationDate >= start && r.ReservationDate < end &&
                        (r.ReservationStatus == "confirmed" || r.ReservationStatus == "checked_in"))
            .ToListAsync();

        foreach (var otherRes in sameDayReservations)
        {
            var otherTableIds = new List<int>();
            if (otherRes.TableId.HasValue) otherTableIds.Add(otherRes.TableId.Value);
            if (!string.IsNullOrWhiteSpace(otherRes.Note) && otherRes.Note.Contains("MergedTableIds:"))
            {
                try
                {
                    var marker = "MergedTableIds:";
                    var sIdx = otherRes.Note.IndexOf(marker) + marker.Length;
                    var eIdx = otherRes.Note.IndexOf("]", sIdx);
                    var rawIds = (eIdx > sIdx ? otherRes.Note.Substring(sIdx, eIdx - sIdx) : otherRes.Note.Substring(sIdx)).Split(',');
                    foreach (var idStr in rawIds)
                    {
                        if (int.TryParse(idStr.Trim(), out var parsedId))
                            otherTableIds.Add(parsedId);
                    }
                }
                catch { }
            }

            var overlap = targetIds.Intersect(otherTableIds).ToList();
            if (overlap.Any())
            {
                var conflictTable = tables.FirstOrDefault(t => t.TableId == overlap.First());
                return BadRequest(new
                {
                    success = false,
                    message = $"Bàn '{conflictTable?.TableName ?? overlap.First().ToString()}' đã được xếp cho khách '{otherRes.CustomerName}' vào ngày {reservation.ReservationDate:dd/MM/yyyy} ({otherRes.ReservationDate:HH:mm}). Vui lòng chọn bàn khác."
                });
            }
        }

        if (tables.Any(t => t.TableStatus == "maintenance"))
        {
            return BadRequest(new { success = false, message = "Bàn đang trong quá trình sửa chữa / bảo trì, không thể xếp khách vào bàn này." });
        }

        // Nếu là ngày HÔM NAY: Không cho phép xếp vào bàn đang phục vụ
        if (isToday && tables.Any(t => t.TableStatus == "serving"))
        {
            return BadRequest(new { success = false, message = "Bàn đang phục vụ không thể dùng để giữ chỗ cho khách đặt trong ngày hôm nay. Vui lòng chọn bàn đang trống." });
        }

        int totalCapacity = tables.Sum(t => t.Capacity);
        var now = DateTime.UtcNow;

        // Chỉ chuyển trạng thái trên sơ đồ bàn sang 'reserved' nếu ngày hẹn là HÔM NAY.
        // Nếu ngày hẹn là tương lai (ngày mai...): giữ nguyên trạng thái sơ đồ hôm nay để đón khách vãng lai.
        if (isToday)
        {
            foreach (var t in tables)
            {
                if (t.TableStatus == "empty")
                {
                    t.TableStatus = "reserved";
                    t.UpdatedAt = now;
                }
            }
        }

        // Bàn đầu tiên đại diện lưu vào reservation.TableId để thỏa mãn khóa ngoại
        var primaryTable = tables[0];
        reservation.TableId = primaryTable.TableId;
        reservation.UpdatedAt = now;

        // Lưu thông tin các bàn ghép vào Note (kèm hidden tag để khi check-in tự động nhận diện tất cả các bàn)
        var idsStr = string.Join(",", targetIds);
        var tableNamesStr = string.Join(" + ", tables.Select(t => t.TableName));
        var mergeTag = tables.Count > 1 
            ? $"[Gộp bàn: {tableNamesStr} (Tổng {totalCapacity} chỗ)|MergedTableIds:{idsStr}]" 
            : null;

        var currentNote = reservation.Note ?? "";
        if (currentNote.Contains("[Gộp bàn:"))
        {
            var startIdx = currentNote.IndexOf("[Gộp bàn:");
            var endIdx = currentNote.IndexOf("]", startIdx);
            if (endIdx > startIdx)
            {
                currentNote = currentNote.Remove(startIdx, endIdx - startIdx + 1).Trim();
                currentNote = currentNote.Trim(' ', '-');
            }
        }

        if (mergeTag != null)
        {
            reservation.Note = string.IsNullOrWhiteSpace(currentNote) ? mergeTag : $"{currentNote} - {mergeTag}";
        }
        else
        {
            reservation.Note = string.IsNullOrWhiteSpace(currentNote) ? null : currentNote;
        }

        await _dbContext.SaveChangesAsync();

        try
        {
            await _hubContext.PublishAsync(_dbContext, "ReservationUpdated", new { reservationId = id, eventType = "table_assigned" });
            await _hubContext.PublishAsync(_dbContext, "StaffOrderUpdated");
        }
        catch { }

        var emptyCount = tables.Count(t => t.TableStatus == "reserved" || t.TableStatus == "empty");
        var servingCount = tables.Count(t => t.TableStatus == "serving");
        var detailMsg = tables.Count > 1
            ? $"Đã gộp {tables.Count} bàn ({tableNamesStr}) cho khách {reservation.CustomerName} thành công (Tổng {totalCapacity} chỗ · {emptyCount} bàn giữ chỗ, {servingCount} bàn đang phục vụ)!"
            : $"Đã xếp {primaryTable.TableName} cho khách {reservation.CustomerName} thành công!";

        return Ok(new
        {
            success = true,
            message = detailMsg,
            data = new
            {
                tableId = primaryTable.TableId,
                tableName = primaryTable.TableName,
                tableIds = targetIds,
                totalCapacity,
                mergedTables = tables.Select(t => new { t.TableId, t.TableName, t.Capacity, t.TableStatus })
            }
        });
    }

    /// <summary>
    /// Check-in khi khách đến nhà hàng: Tự động chuyển checked_in, tất cả bàn gộp sang serving, tạo Order mở mới liên thông!
    /// </summary>
    [HttpPost("{id}/check-in")]
    public async Task<IActionResult> CheckIn(int id, [FromBody] AdminCheckInRequest? request = null)
    {
        var reservation = await _dbContext.Reservations
            .Include(r => r.Table)
            .Include(r => r.Customer)
            .FirstOrDefaultAsync(r => r.ReservationId == id);

        if (reservation == null)
            return NotFound(new { success = false, message = "Không tìm thấy phiếu đặt bàn." });

        if (reservation.ReservationStatus == "checked_in")
            return BadRequest(new { success = false, message = "Khách đã được check-in trước đó." });

        if (reservation.ReservationStatus != "confirmed")
            return BadRequest(new { success = false, message = "Phiếu phải được xác nhận trước khi check-in." });

        // Thu thập toàn bộ bàn cần check-in: từ request hoặc từ MergedTableIds trong Note hoặc TableId
        var targetTableIds = new List<int>();
        if (request?.TableIds != null && request.TableIds.Count > 0)
        {
            targetTableIds.AddRange(request.TableIds);
        }
        else if (!string.IsNullOrWhiteSpace(reservation.Note) && reservation.Note.Contains("MergedTableIds:"))
        {
            try
            {
                var marker = "MergedTableIds:";
                var start = reservation.Note.IndexOf(marker) + marker.Length;
                var end = reservation.Note.IndexOf("]", start);
                var rawIds = (end > start ? reservation.Note.Substring(start, end - start) : reservation.Note.Substring(start)).Split(',');
                foreach (var idStr in rawIds)
                {
                    if (int.TryParse(idStr.Trim(), out var parsedId))
                        targetTableIds.Add(parsedId);
                }
            }
            catch { }
        }

        if (targetTableIds.Count == 0 && reservation.TableId.HasValue && reservation.TableId.Value > 0)
        {
            targetTableIds.Add(reservation.TableId.Value);
        }

        if (targetTableIds.Count == 0)
            return BadRequest(new { success = false, message = "Vui lòng phân bàn trước khi thực hiện check-in." });

        var tables = await _dbContext.DiningTables
            .Where(t => targetTableIds.Contains(t.TableId) && t.IsActive)
            .ToListAsync();

        if (tables.Count == 0)
            return BadRequest(new { success = false, message = "Bàn ăn không tồn tại hoặc đã ngừng hoạt động." });

        var primaryTable = tables.FirstOrDefault(t => t.TableId == reservation.TableId) ?? tables[0];

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        int createdOrderId = 0;

        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var now = DateTime.UtcNow;

                // 1. Cập nhật reservation
                reservation.TableId = primaryTable.TableId;
                reservation.ReservationStatus = "checked_in";
                reservation.UpdatedAt = now;

                // 2. Chuyển tất cả các bàn trong tiệc sang serving
                foreach (var tbl in tables)
                {
                    tbl.TableStatus = "serving";
                    tbl.UpdatedAt = now;
                }

                // 3. Tìm xem có bàn nào đã có Order open nào đang mở sẵn không
                var existingActiveOrder = await _dbContext.OrderTables
                    .Include(ot => ot.Order)
                    .Where(ot => targetTableIds.Contains(ot.TableId) && ot.Order.OrderStatus == "open")
                    .Select(ot => ot.Order)
                    .FirstOrDefaultAsync();

                if (existingActiveOrder != null)
                {
                    if (!existingActiveOrder.CustomerId.HasValue)
                    {
                        existingActiveOrder.CustomerId = reservation.CustomerId;
                        existingActiveOrder.UpdatedAt = now;
                    }
                    createdOrderId = existingActiveOrder.OrderId;
                }
                else
                {
                    // Tạo Order mới
                    var order = new Order
                    {
                        CustomerId = reservation.CustomerId,
                        CreatedBy = GetCurrentUserId(),
                        OrderStatus = "open",
                        Subtotal = 0,
                        DiscountAmount = 0,
                        TotalAmount = 0,
                        Note = !string.IsNullOrWhiteSpace(reservation.Note)
                            ? $"[Khách đặt bàn #{reservation.ReservationId} - {reservation.CustomerName}]: {reservation.Note}"
                            : $"[Khách đặt bàn #{reservation.ReservationId} - {reservation.CustomerName}]",
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    _dbContext.Orders.Add(order);
                    await _dbContext.SaveChangesAsync();
                    createdOrderId = order.OrderId;
                }

                // 4. Gắn TẤT CẢ các bàn vào OrderTables của Order này
                var currentLinks = await _dbContext.OrderTables
                    .Where(ot => ot.OrderId == createdOrderId)
                    .Select(ot => ot.TableId)
                    .ToListAsync();

                foreach (var tbl in tables)
                {
                    if (!currentLinks.Contains(tbl.TableId))
                    {
                        _dbContext.OrderTables.Add(new OrderTable
                        {
                            OrderId = createdOrderId,
                            TableId = tbl.TableId,
                            IsPrimary = (tbl.TableId == primaryTable.TableId),
                            AssignedAt = now
                        });
                    }
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });

        // 5. Phát sóng SignalR
        try
        {
            await _hubContext.PublishAsync(_dbContext, "StaffOrderUpdated", new
            {
                orderId = createdOrderId,
                tableId = primaryTable.TableId,
                eventType = "check_in"
            });
            await _hubContext.PublishAsync(_dbContext, "ReservationUpdated", new
            {
                reservationId = id,
                eventType = "checked_in"
            });
        }
        catch { }

        var names = string.Join(" + ", tables.Select(t => t.TableName));
        return Ok(new
        {
            success = true,
            message = tables.Count > 1
                ? $"Đã check-in đón khách {reservation.CustomerName} vào cụm bàn gộp ({names}) thành công! Order #{createdOrderId} đã tự động kích hoạt."
                : $"Check-in thành công cho khách {reservation.CustomerName}! {primaryTable.TableName} đã được kích hoạt.",
            data = new
            {
                orderId = createdOrderId,
                tableId = primaryTable.TableId,
                tableCode = primaryTable.TableCode,
                tableName = primaryTable.TableName,
                tableIds = targetTableIds,
                customerName = reservation.CustomerName
            }
        });
    }

    /// <summary>
    /// Hủy phiếu đặt bàn và hoàn trả bàn về empty nếu đang reserved
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelReservation(int id, [FromBody] AdminCancelReservationRequest? request)
    {
        var reservation = await _dbContext.Reservations.Include(r => r.Table).FirstOrDefaultAsync(r => r.ReservationId == id);
        if (reservation == null)
            return NotFound(new { success = false, message = "Không tìm thấy phiếu đặt bàn này." });

        if (reservation.ReservationStatus == "checked_in" || reservation.ReservationStatus == "completed")
            return BadRequest(new { success = false, message = "Khách đã check-in hoặc hoàn tất, không thể hủy." });

        reservation.ReservationStatus = "cancelled";
        reservation.Note = string.IsNullOrWhiteSpace(request?.Reason)
            ? reservation.Note
            : $"{reservation.Note} | [Hủy bởi quản trị: {request.Reason}]";
        reservation.UpdatedAt = DateTime.UtcNow;

        if (reservation.Table != null && reservation.Table.TableStatus == "reserved")
        {
            var hasOther = await _dbContext.Reservations.AnyAsync(r =>
                r.ReservationId != id &&
                r.TableId == reservation.TableId &&
                r.ReservationStatus == "confirmed");

            if (!hasOther)
            {
                reservation.Table.TableStatus = "empty";
                reservation.Table.UpdatedAt = DateTime.UtcNow;
            }
        }

        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = GetCurrentUserId(),
            ActionName = "reservation_cancelled",
            EntityName = "Reservation",
            EntityId = reservation.ReservationId.ToString(),
            OldData = System.Text.Json.JsonSerializer.Serialize(new { status = "pending" }),
            NewData = System.Text.Json.JsonSerializer.Serialize(new {
                status = "cancelled",
                customerName = reservation.CustomerName,
                reason = request?.Reason
            }),
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();

        try
        {
            await _hubContext.PublishAsync(_dbContext, "ReservationUpdated", new { reservationId = id, eventType = "cancelled" });
            await _hubContext.PublishAsync(_dbContext, "StaffOrderUpdated");
        }
        catch { }

        return Ok(new { success = true, message = "Đã hủy phiếu đặt bàn thành công." });
    }

    /// <summary>
    /// Đánh dấu khách vắng mặt (No-show)
    /// </summary>
    [HttpPost("{id}/no-show")]
    public async Task<IActionResult> MarkNoShow(int id)
    {
        var reservation = await _dbContext.Reservations.Include(r => r.Table).FirstOrDefaultAsync(r => r.ReservationId == id);
        if (reservation == null)
            return NotFound(new { success = false, message = "Không tìm thấy phiếu đặt bàn này." });

        reservation.ReservationStatus = "no_show";
        reservation.UpdatedAt = DateTime.UtcNow;

        if (reservation.Table != null && reservation.Table.TableStatus == "reserved")
        {
            var hasOther = await _dbContext.Reservations.AnyAsync(r =>
                r.ReservationId != id &&
                r.TableId == reservation.TableId &&
                r.ReservationStatus == "confirmed");

            if (!hasOther)
            {
                reservation.Table.TableStatus = "empty";
                reservation.Table.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _dbContext.SaveChangesAsync();

        try
        {
            await _hubContext.PublishAsync(_dbContext, "ReservationUpdated", new { reservationId = id, eventType = "no_show" });
            await _hubContext.PublishAsync(_dbContext, "StaffOrderUpdated");
        }
        catch { }

        return Ok(new { success = true, message = "Đã đánh dấu khách vắng mặt (No-show)." });
    }
}

public class AdminCreateReservationRequest
{
    public string CustomerName { get; set; } = null!;
    public string CustomerPhone { get; set; } = null!;
    public string? Email { get; set; }
    public DateTime ReservationDate { get; set; }
    public int GuestCount { get; set; }
    public string? Note { get; set; }
}

public class AssignTableRequest
{
    public int? TableId { get; set; }
    public List<int>? TableIds { get; set; }
}

public class AdminCheckInRequest
{
    public List<int>? TableIds { get; set; }
}

public class AdminCancelReservationRequest
{
    public string? Reason { get; set; }
}

