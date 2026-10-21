using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;
using Restaurant.Api.Hubs;
using System.Security.Claims;

namespace Restaurant.Api.Controllers.Reservations;

[ApiController]
[Route("api/customer")]
public class CustomerReservationController : ControllerBase
{
    private readonly RestaurantDbContext _dbContext;
    private readonly IHubContext<RestaurantHub> _hubContext;

    public CustomerReservationController(RestaurantDbContext dbContext, IHubContext<RestaurantHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Lấy thực đơn trực tuyến cho khách hàng tham khảo
    /// </summary>
    [HttpGet("menu")]
    public async Task<IActionResult> GetCustomerMenu()
    {
        var categories = await _dbContext.FoodCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new
            {
                c.CategoryId,
                c.CategoryName,
                c.Description,
                c.SortOrder,
                Foods = c.Foods
                    .Where(f => f.IsActive && f.FoodStatus == "available")
                    .Select(f => new
                    {
                        f.FoodId,
                        f.FoodName,
                        f.Description,
                        Price = f.MinPrice,
                        f.MinPrice,
                        f.ImageUrl,
                        f.FoodStatus,
                        Variants = f.FoodVariants
                            .Where(v => v.IsActive)
                            .OrderBy(v => v.SortOrder)
                            .Select(v => new
                            {
                                v.VariantId,
                                v.VariantName,
                                v.Price,
                                v.Unit
                            }).ToList()
                    }).ToList()
            })
            .ToListAsync();

        return Ok(new { success = true, data = categories });
    }

    /// <summary>
    /// Khách hàng gửi yêu cầu đặt bàn trực tuyến
    /// </summary>
    [HttpPost("reservations/book")]
    public async Task<IActionResult> BookReservation([FromBody] CustomerBookingRequest request)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "Dữ liệu gửi lên không hợp lệ." });

        if (request.GuestCount <= 0)
            return BadRequest(new { success = false, message = "Số lượng khách phải lớn hơn 0." });

        if (request.ReservationDate <= DateTime.Now.AddMinutes(-10))
            return BadRequest(new { success = false, message = "Thời gian đặt bàn phải ở thời điểm tương lai." });

        var currentUserId = GetCurrentUserId();
        var phoneClean = request.CustomerPhone?.Trim() ?? string.Empty;
        var nameClean = request.CustomerName?.Trim() ?? string.Empty;
        var emailClean = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();

        // Khi đã đăng nhập, thông tin trong token và hồ sơ Customer là nguồn chính.
        // Không cho phép payload đổi sang số điện thoại hoặc tên của người khác.
        User? currentUser = null;
        Customer? customer = null;
        if (currentUserId.HasValue)
        {
            currentUser = await _dbContext.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == currentUserId.Value);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "Không tìm thấy tài khoản đăng nhập." });

            if (!string.Equals(currentUser.Role?.RoleCode, "customer", StringComparison.OrdinalIgnoreCase))
                return Forbid();

            customer = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.UserId == currentUserId.Value);

            if (customer == null)
            {
                if (string.IsNullOrWhiteSpace(currentUser.Phone))
                    return BadRequest(new { success = false, message = "Vui lòng cập nhật số điện thoại trong tài khoản trước khi đặt bàn." });

                phoneClean = currentUser.Phone.Trim();
                nameClean = currentUser.FullName.Trim();
                emailClean = currentUser.Email.Trim();
                customer = await _dbContext.Customers
                    .FirstOrDefaultAsync(c => c.UserId == null && c.Phone == phoneClean);

                if (customer == null)
                {
                    customer = new Customer
                    {
                        UserId = currentUser.UserId,
                        FullName = nameClean,
                        Phone = phoneClean,
                        Email = emailClean,
                        PointsBalance = 0,
                        TotalSpent = 0,
                        VisitCount = 0,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _dbContext.Customers.Add(customer);
                }
                else
                {
                    customer.UserId = currentUser.UserId;
                    customer.FullName = nameClean;
                    customer.Email = emailClean;
                    customer.UpdatedAt = DateTime.UtcNow;
                }
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                phoneClean = customer.Phone;
                nameClean = customer.FullName;
                emailClean = customer.Email ?? currentUser.Email;
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.CustomerName))
                return BadRequest(new { success = false, message = "Vui lòng nhập họ và tên quý khách." });

            if (string.IsNullOrWhiteSpace(request.CustomerPhone))
                return BadRequest(new { success = false, message = "Vui lòng nhập số điện thoại liên hệ." });

            customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Phone == phoneClean);
        }

        // 1. Tìm hoặc tạo bản ghi Customer cho khách chưa đăng nhập.
        if (customer == null)
        {
            customer = new Customer
            {
                FullName = nameClean,
                Phone = phoneClean,
                Email = emailClean,
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
        else
        {
            // Cập nhật tên nếu trước đây chưa có
            if (string.IsNullOrWhiteSpace(customer.FullName) || customer.FullName == "Khách vãng lai")
            {
                customer.FullName = nameClean;
                customer.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }
        }

        // 2. Tạo bản ghi Reservation
        var reservation = new Reservation
        {
            CustomerId = customer.CustomerId,
            TableId = null, // Chờ Quản lý / Staff xếp bàn
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

        // 3. Phát thông báo SignalR đến Admin/Staff
        try
        {
            await _hubContext.Clients.All.SendAsync("ReservationUpdated", new
            {
                reservationId = reservation.ReservationId,
                customerName = reservation.CustomerName,
                customerPhone = reservation.CustomerPhone,
                guestCount = reservation.GuestCount,
                reservationDate = reservation.ReservationDate.ToString("yyyy-MM-dd HH:mm"),
                eventType = "created"
            });
        }
        catch { }

        return Ok(new
        {
            success = true,
            message = "Đặt bàn thành công! Nhà hàng Hương Sen sẽ sớm liên hệ xác nhận và chuẩn bị bàn cho quý khách.",
            data = new
            {
                reservationId = reservation.ReservationId,
                customerName = reservation.CustomerName,
                customerPhone = reservation.CustomerPhone,
                guestCount = reservation.GuestCount,
                reservationDate = reservation.ReservationDate,
                reservationStatus = reservation.ReservationStatus
            }
        });
    }

    /// <summary>
    /// Tra cứu danh sách phiếu đặt bàn theo số điện thoại
    /// </summary>
    [HttpGet("reservations/lookup")]
    public async Task<IActionResult> LookupBookings([FromQuery] string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return BadRequest(new { success = false, message = "Vui lòng cung cấp số điện thoại để tra cứu." });

        var phoneClean = phone.Trim();

        var bookings = await _dbContext.Reservations
            .Include(r => r.Table)
            .Where(r => r.CustomerPhone == phoneClean)
            .OrderByDescending(r => r.ReservationDate)
            .Take(15)
            .Select(r => new
            {
                r.ReservationId,
                r.CustomerName,
                r.CustomerPhone,
                r.GuestCount,
                r.ReservationDate,
                r.ReservationStatus,
                r.Note,
                r.CreatedAt,
                AssignedTable = r.Table != null ? new
                {
                    r.Table.TableId,
                    r.Table.TableCode,
                    r.Table.TableName,
                    r.Table.AreaName
                } : null
            })
            .ToListAsync();

        return Ok(new { success = true, data = bookings });
    }

    /// <summary>
    /// Khách hàng yêu cầu hủy lịch đặt bàn
    /// </summary>
    [HttpPost("reservations/{id}/cancel")]
    public async Task<IActionResult> CancelBooking(int id, [FromBody] CancelBookingRequest? request)
    {
        var reservation = await _dbContext.Reservations.Include(r => r.Table).FirstOrDefaultAsync(r => r.ReservationId == id);
        var currentCustomer = await GetCurrentCustomerAsync();
        if (currentCustomer != null && reservation != null && reservation.CustomerId != currentCustomer.CustomerId)
            return Forbid();
        if (currentCustomer == null && reservation != null &&
            (string.IsNullOrWhiteSpace(request?.Phone) ||
             !string.Equals(reservation.CustomerPhone, request.Phone.Trim(), StringComparison.OrdinalIgnoreCase)))
            return Unauthorized(new { success = false, message = "Phone confirmation is required." });
        if (reservation == null)
            return NotFound(new { success = false, message = "Không tìm thấy thông tin đặt bàn này." });

        if (reservation.ReservationStatus == "checked_in" || reservation.ReservationStatus == "completed")
            return BadRequest(new { success = false, message = "Phiếu đặt bàn đã đón khách hoặc đã hoàn thành, không thể hủy." });

        if (reservation.ReservationStatus == "cancelled" || reservation.ReservationStatus == "rejected")
            return BadRequest(new { success = false, message = "Phiếu đặt bàn này đã được hủy trước đó." });

        if (reservation.ReservationDate <= DateTime.Now)
            return BadRequest(new { success = false, message = "Cannot cancel a reservation after its arrival time." });

        reservation.ReservationStatus = "cancelled";
        reservation.Note = string.IsNullOrWhiteSpace(request?.Reason)
            ? reservation.Note
            : $"{reservation.Note} | [Khách hủy: {request.Reason}]";
        reservation.UpdatedAt = DateTime.UtcNow;

        // Nếu bàn đã xếp và đang ở trạng thái reserved, trả về empty
        if (reservation.Table != null && reservation.Table.TableStatus == "reserved")
        {
            // Kiểm tra xem còn reservation nào khác đang confirmed trên bàn này không
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
            await _hubContext.Clients.All.SendAsync("ReservationUpdated", new { reservationId = id, eventType = "cancelled" });
            await _hubContext.Clients.All.SendAsync("StaffOrderUpdated");
        }
        catch { }

        return Ok(new { success = true, message = "Đã hủy lịch đặt bàn thành công." });
    }

    /// <summary>
    /// Tra cứu thông tin hồ sơ thành viên, điểm thưởng và lịch sử
    /// </summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetCustomerProfile([FromQuery] string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return BadRequest(new { success = false, message = "Vui lòng cung cấp số điện thoại để tra cứu." });

        var phoneClean = phone.Trim();
        var customer = await _dbContext.Customers
            .Include(c => c.PointTransactions)
            .FirstOrDefaultAsync(c => c.Phone == phoneClean);

        var bookings = await _dbContext.Reservations
            .Include(r => r.Table)
            .Where(r => r.CustomerPhone == phoneClean)
            .OrderByDescending(r => r.ReservationDate)
            .Take(15)
            .Select(r => new
            {
                r.ReservationId,
                r.CustomerName,
                r.CustomerPhone,
                r.GuestCount,
                r.ReservationDate,
                r.ReservationStatus,
                r.Note,
                r.CreatedAt,
                AssignedTable = r.Table != null ? new
                {
                    r.Table.TableId,
                    r.Table.TableCode,
                    r.Table.TableName,
                    r.Table.AreaName
                } : null
            })
            .ToListAsync();

        if (customer == null && bookings.Count == 0)
        {
            return NotFound(new { success = false, message = "Không tìm thấy hồ sơ với số điện thoại này. Quý khách vui lòng đặt bàn để trở thành thành viên." });
        }

        var pointHistory = customer?.PointTransactions?
            .OrderByDescending(p => p.CreatedAt)
            .Take(10)
            .Select(p => new
            {
                p.PointTransactionId,
                p.TransactionType,
                p.Points,
                p.BalanceAfter,
                p.Note,
                p.CreatedAt
            }).ToList() ?? new();

        var invoices = customer == null ? new() : await _dbContext.Invoices
            .Include(i => i.InvoiceItems)
            .Include(i => i.IssuedByNavigation)
            .Include(i => i.Payments)
            .Include(i => i.Order)
                .ThenInclude(o => o.OrderTables)
                    .ThenInclude(ot => ot.Table)
            .Where(i => i.CustomerId == customer.CustomerId || (i.Order != null && i.Order.CustomerId == customer.CustomerId))
            .OrderByDescending(i => i.IssuedAt)
            .Take(20)
            .Select(i => new
            {
                i.InvoiceId,
                i.InvoiceCode,
                i.IssuedAt,
                i.Subtotal,
                i.DiscountAmount,
                i.PointsUsed,
                i.PointsEarned,
                i.TotalAmount,
                i.InvoiceStatus,
                cashierName = i.IssuedByNavigation != null ? i.IssuedByNavigation.FullName : "Thu ngân",
                tableName = i.Order != null && i.Order.OrderTables.Any()
                    ? string.Join(", ", i.Order.OrderTables.Select(ot => ot.Table.TableName))
                    : "Bàn phục vụ",
                areaName = i.Order != null && i.Order.OrderTables.Any()
                    ? i.Order.OrderTables.FirstOrDefault()!.Table.AreaName
                    : "Khu vực",
                paymentMethod = i.Payments.OrderByDescending(p => p.PaidAt).Select(p => p.PaymentMethod).FirstOrDefault() ?? "cash",
                paymentStatus = i.Payments.OrderByDescending(p => p.PaidAt).Select(p => p.PaymentStatus).FirstOrDefault() ?? "completed",
                items = i.InvoiceItems.Select(item => new
                {
                    item.InvoiceItemId,
                    item.FoodNameSnapshot,
                    item.VariantSnapshot,
                    item.UnitPrice,
                    item.Quantity,
                    item.LineTotal,
                    item.Note
                }).ToList()
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                customerId = customer?.CustomerId ?? 0,
                fullName = customer?.FullName ?? (bookings.FirstOrDefault()?.CustomerName ?? "Khách hàng"),
                phone = phoneClean,
                email = customer?.Email,
                pointsBalance = customer?.PointsBalance ?? 0,
                totalSpent = customer?.TotalSpent ?? 0,
                visitCount = customer?.VisitCount ?? bookings.Count(b => b.ReservationStatus == "completed"),
                createdAt = customer?.CreatedAt ?? bookings.LastOrDefault()?.CreatedAt ?? DateTime.UtcNow,
                bookings,
                pointHistory,
                invoices
            }
        });
    }

    /// <summary>
    /// Tra cứu chi tiết một hóa đơn theo mã (cho chế độ in bill hoặc quét QR)
    /// </summary>
    [HttpGet("invoices/{invoiceCode}")]
    public async Task<IActionResult> GetInvoiceByCode(string invoiceCode)
    {
        if (string.IsNullOrWhiteSpace(invoiceCode))
            return BadRequest(new { success = false, message = "Vui lòng nhập mã hóa đơn." });

        var codeClean = invoiceCode.Trim();
        var invoice = await _dbContext.Invoices
            .Include(i => i.Customer)
            .Include(i => i.InvoiceItems)
            .Include(i => i.IssuedByNavigation)
            .Include(i => i.Payments)
            .Include(i => i.Order)
                .ThenInclude(o => o.OrderTables)
                    .ThenInclude(ot => ot.Table)
            .FirstOrDefaultAsync(i => i.InvoiceCode == codeClean || i.InvoiceId.ToString() == codeClean);

        if (invoice == null)
            return NotFound(new { success = false, message = "Không tìm thấy hóa đơn với mã này." });

        return Ok(new
        {
            success = true,
            data = new
            {
                invoice.InvoiceId,
                invoice.InvoiceCode,
                invoice.IssuedAt,
                invoice.Subtotal,
                invoice.DiscountAmount,
                invoice.PointsUsed,
                invoice.PointsEarned,
                invoice.TotalAmount,
                invoice.InvoiceStatus,
                customerName = invoice.Customer != null ? invoice.Customer.FullName : "Khách hàng",
                customerPhone = invoice.Customer != null ? invoice.Customer.Phone : null,
                cashierName = invoice.IssuedByNavigation != null ? invoice.IssuedByNavigation.FullName : "Thu ngân",
                tableName = invoice.Order != null && invoice.Order.OrderTables.Any()
                    ? string.Join(", ", invoice.Order.OrderTables.Select(ot => ot.Table.TableName))
                    : "Bàn phục vụ",
                areaName = invoice.Order != null && invoice.Order.OrderTables.Any()
                    ? invoice.Order.OrderTables.FirstOrDefault()!.Table.AreaName
                    : "Khu vực",
                paymentMethod = invoice.Payments.OrderByDescending(p => p.PaidAt).Select(p => p.PaymentMethod).FirstOrDefault() ?? "cash",
                paymentStatus = invoice.Payments.OrderByDescending(p => p.PaidAt).Select(p => p.PaymentStatus).FirstOrDefault() ?? "completed",
                items = invoice.InvoiceItems.Select(item => new
                {
                    item.InvoiceItemId,
                    item.FoodNameSnapshot,
                    item.VariantSnapshot,
                    item.UnitPrice,
                    item.Quantity,
                    item.LineTotal,
                    item.Note
                }).ToList()
            }
        });
    }

    [Authorize(Roles = "customer,staff,manager,admin")]
    [HttpGet("account")]
    public async Task<IActionResult> GetAccount()
    {
        var customer = await GetCurrentCustomerAsync();
        if (customer == null)
        {
            var userId = GetCurrentUserId();
            var user = userId.HasValue ? await _dbContext.Users.FindAsync(userId.Value) : null;
            if (user == null)
                return Unauthorized(new { success = false, message = "Authenticated user was not found." });

            return Ok(new
            {
                success = true,
                data = new
                {
                    account = new
                    {
                        CustomerId = 0,
                        user.FullName,
                        user.Phone,
                        user.Email,
                        user.AvatarUrl,
                        PointsBalance = 0,
                        TotalSpent = 0m,
                        VisitCount = 0,
                        user.CreatedAt
                    },
                    reservations = Array.Empty<object>(),
                    invoices = Array.Empty<object>(),
                    pointHistory = Array.Empty<object>()
                }
            });
        }

        var reservations = await _dbContext.Reservations
            .Include(r => r.Table)
            .Where(r => r.CustomerId == customer.CustomerId)
            .OrderByDescending(r => r.ReservationDate)
            .Take(30)
            .ToListAsync();

        var invoices = await _dbContext.Invoices
            .Include(i => i.InvoiceItems)
            .Include(i => i.IssuedByNavigation)
            .Include(i => i.Payments)
            .Include(i => i.Order)
                .ThenInclude(o => o.OrderTables)
                    .ThenInclude(ot => ot.Table)
            .Where(i => i.CustomerId == customer.CustomerId ||
                        (i.Order != null && i.Order.CustomerId == customer.CustomerId))
            .OrderByDescending(i => i.IssuedAt)
            .Take(30)
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                account = new
                {
                    customer.CustomerId,
                    customer.FullName,
                    customer.Phone,
                    customer.Email,
                    customer.AvatarUrl,
                    customer.PointsBalance,
                    customer.TotalSpent,
                    customer.VisitCount,
                    customer.CreatedAt
                },
                reservations = reservations.Select(r => new
                {
                    r.ReservationId,
                    r.GuestCount,
                    r.ReservationDate,
                    r.ReservationStatus,
                    r.Note,
                    r.CreatedAt,
                    tableName = r.Table == null ? null : r.Table.TableName,
                    areaName = r.Table == null ? null : r.Table.AreaName
                }),
                invoices = invoices.Select(i => new
                {
                    i.InvoiceId,
                    i.InvoiceCode,
                    i.IssuedAt,
                    i.Subtotal,
                    i.DiscountAmount,
                    i.PointsUsed,
                    i.PointsEarned,
                    i.TotalAmount,
                    i.InvoiceStatus,
                    cashierName = i.IssuedByNavigation != null ? i.IssuedByNavigation.FullName : "Thu ngân",
                    tableName = i.Order != null && i.Order.OrderTables.Any()
                        ? string.Join(", ", i.Order.OrderTables.Select(ot => ot.Table.TableName))
                        : "Bàn phục vụ",
                    areaName = i.Order != null && i.Order.OrderTables.Any()
                        ? i.Order.OrderTables.FirstOrDefault()!.Table.AreaName
                        : "Khu vực",
                    paymentMethod = i.Payments.OrderByDescending(p => p.PaidAt).Select(p => p.PaymentMethod).FirstOrDefault() ?? "cash",
                    paymentStatus = i.Payments.OrderByDescending(p => p.PaidAt).Select(p => p.PaymentStatus).FirstOrDefault() ?? "completed",
                    transactionCode = i.Payments.OrderByDescending(p => p.PaidAt).Select(p => p.TransactionCode).FirstOrDefault(),
                    paidAt = i.Payments.OrderByDescending(p => p.PaidAt).Select(p => p.PaidAt).FirstOrDefault(),
                    items = i.InvoiceItems.Select(item => new
                    {
                        item.InvoiceItemId,
                        item.FoodNameSnapshot,
                        item.VariantSnapshot,
                        item.UnitPrice,
                        item.Quantity,
                        item.LineTotal,
                        item.Note
                    })
                }),
                pointHistory = customer.PointTransactions
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(20)
                    .Select(p => new { p.PointTransactionId, p.TransactionType, p.Points, p.BalanceAfter, p.Note, p.CreatedAt })
            }
        });
    }

    [Authorize(Roles = "customer,staff,manager,admin")]
    [HttpPut("account")]
    public async Task<IActionResult> UpdateAccount([FromBody] UpdateCustomerAccountRequest request)
    {
        var customer = await GetCurrentCustomerAsync();
        var userId = GetCurrentUserId();
        var user = userId.HasValue ? await _dbContext.Users.FindAsync(userId.Value) : null;
        if (user == null)
            return Unauthorized(new { success = false, message = "Authenticated user was not found." });

        var fullName = request.FullName?.Trim();
        var phone = request.Phone?.Trim();
        var email = request.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(email))
            return BadRequest(new { success = false, message = "Full name, phone and email are required." });

        var currentCustomerId = customer?.CustomerId ?? 0;
        if (await _dbContext.Users.AnyAsync(u => u.UserId != user.UserId && u.Email == email))
            return Conflict(new { success = false, message = "Email is already in use." });
        if (await _dbContext.Customers.AnyAsync(c => c.CustomerId != currentCustomerId && c.Phone == phone))
            return Conflict(new { success = false, message = "Phone is already in use." });

        if (customer == null)
        {
            customer = new Customer
            {
                UserId = user.UserId,
                CreatedAt = DateTime.UtcNow,
                PointsBalance = 0,
                TotalSpent = 0,
                VisitCount = 0,
                IsActive = true
            };
            _dbContext.Customers.Add(customer);
        }

        customer.FullName = fullName;
        customer.Phone = phone;
        customer.Email = email;
        customer.UpdatedAt = DateTime.UtcNow;
        user.FullName = fullName;
        user.Phone = phone;
        user.Email = email;
        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Ok(new { success = true, message = "Account information updated." });
    }

    private int? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out var userId) ? userId : null;
    }

    private async Task<Customer?> GetCurrentCustomerAsync()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return null;

        return await _dbContext.Customers
            .Include(c => c.User)
            .Include(c => c.PointTransactions)
            .FirstOrDefaultAsync(c => c.UserId == userId.Value);
    }
}

public class CustomerBookingRequest
{
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? Email { get; set; }
    public DateTime ReservationDate { get; set; }
    public int GuestCount { get; set; }
    public string? Note { get; set; }
}

public class CancelBookingRequest
{
    public string? Reason { get; set; }
    public string? Phone { get; set; }
}

public class UpdateCustomerAccountRequest
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}
