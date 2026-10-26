using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;
using Restaurant.Api.Reporting;
using System.Text.Json;

namespace Restaurant.Api.Controllers.Admin;

[Route("api/admin/reports")]
[Authorize(Roles = "admin,manager")]
[ApiController]
public class AdminReportsController : ControllerBase
{
    private const int VietnamUtcOffsetHours = 7;
    private readonly RestaurantDbContext _dbContext;

    private sealed record ReportPeriod(
        DateOnly FromDate,
        DateOnly ToDate,
        DateTime StartUtc,
        DateTime EndExclusiveUtc);

    private sealed class ProductSaleLine
    {
        public DateTime InvoiceIssuedAt { get; init; }
        public int? FoodId { get; init; }
        public int? VariantId { get; init; }
        public string FoodName { get; init; } = string.Empty;
        public string VariantName { get; init; } = string.Empty;
        public string Unit { get; init; } = string.Empty;
        public int? CategoryId { get; init; }
        public string? CategoryName { get; init; }
        public int Quantity { get; init; }
        public decimal LineTotal { get; init; }
        public decimal InvoiceSubtotal { get; init; }
        public decimal InvoiceDiscount { get; init; }
    }

    private sealed class ProductReportRow
    {
        public int? FoodId { get; init; }
        public int? VariantId { get; init; }
        public string FoodName { get; init; } = string.Empty;
        public string VariantName { get; init; } = string.Empty;
        public string Unit { get; init; } = string.Empty;
        public int? CategoryId { get; init; }
        public string CategoryName { get; init; } = "Chưa phân loại";
        public int SoldQuantity { get; init; }
        public int PreviousSoldQuantity { get; init; }
        public decimal? SoldQuantityChangePercent { get; init; }
        public decimal GrossRevenue { get; init; }
        public decimal PreviousGrossRevenue { get; init; }
        public decimal? GrossRevenueChangePercent { get; init; }
        public decimal Revenue { get; init; }
        public decimal PreviousRevenue { get; init; }
        public decimal? RevenueChangePercent { get; init; }
        public decimal? ContributionPercent { get; set; }
    }

    public AdminReportsController(RestaurantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("sales")]
    public async Task<IActionResult> GetSalesReport(
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(VietnamUtcOffsetHours));
        var startDate = fromDate ?? new DateOnly(today.Year, today.Month, 1);
        var endDate = toDate ?? today;

        if (startDate > endDate)
        {
            return BadRequest(new { success = false, message = "Ngày bắt đầu phải trước hoặc bằng ngày kết thúc." });
        }

        // Stored payment, invoice, and cancellation timestamps are UTC. Interpret the
        // date-only filter as Vietnam local dates so transactions near midnight are
        // included in the day the restaurant staff expects.
        var start = startDate.ToDateTime(TimeOnly.MinValue).AddHours(-VietnamUtcOffsetHours);
        var endExclusive = endDate.AddDays(1).ToDateTime(TimeOnly.MinValue).AddHours(-VietnamUtcOffsetHours);
        var periodLength = endExclusive - start;
        if (periodLength.TotalDays > 366)
        {
            return BadRequest(new { success = false, message = "Khoảng thời gian báo cáo tối đa là 366 ngày." });
        }

        var previousStart = start - periodLength;
        var previousEnd = start;

        var paidInvoices = _dbContext.Invoices
            .AsNoTracking()
            .Where(invoice => invoice.InvoiceStatus == "paid");

        var currentInvoices = paidInvoices
            .Where(invoice => invoice.IssuedAt >= start && invoice.IssuedAt < endExclusive);
        var previousInvoices = paidInvoices
            .Where(invoice => invoice.IssuedAt >= previousStart && invoice.IssuedAt < previousEnd);

        var grossRevenue = await currentInvoices.SumAsync(invoice => (decimal?)invoice.Subtotal) ?? 0m;
        var discountAmount = await currentInvoices.SumAsync(invoice => (decimal?)invoice.DiscountAmount) ?? 0m;
        var revenue = await currentInvoices.SumAsync(invoice => (decimal?)invoice.TotalAmount) ?? 0m;
        var previousRevenue = await previousInvoices.SumAsync(invoice => (decimal?)invoice.TotalAmount) ?? 0m;
        var invoiceCount = await currentInvoices.CountAsync();
        var previousInvoiceCount = await previousInvoices.CountAsync();

        var dailyTotals = await currentInvoices
            .GroupBy(invoice => invoice.IssuedAt.AddHours(VietnamUtcOffsetHours).Date)
            .Select(group => new
            {
                Date = group.Key,
                GrossRevenue = group.Sum(invoice => invoice.Subtotal),
                DiscountAmount = group.Sum(invoice => invoice.DiscountAmount),
                Revenue = group.Sum(invoice => invoice.TotalAmount),
                InvoiceCount = group.Count()
            })
            .ToDictionaryAsync(row => row.Date, row => row.Revenue);

        var hourlyRows = await currentInvoices
            .Select(invoice => new { invoice.IssuedAt, invoice.TotalAmount })
            .ToListAsync();

        var hourlyTrend = Enumerable.Range(0, 24)
            .Select(hour =>
            {
                var rows = hourlyRows.Where(row => row.IssuedAt.AddHours(VietnamUtcOffsetHours).Hour == hour).ToList();
                return new
                {
                    Hour = hour,
                    Revenue = rows.Sum(row => row.TotalAmount),
                    InvoiceCount = rows.Count
                };
            })
            .ToList();

        var dailyTrend = Enumerable.Range(0, (int)periodLength.TotalDays)
            .Select(offset =>
            {
                // Build labels from the requested Vietnam-local dates. `start` is UTC,
                // so deriving DateOnly from it would shift the first day backward.
                var date = startDate.AddDays(offset);
                return new
                {
                    Date = date,
                    Revenue = dailyTotals.GetValueOrDefault(date.ToDateTime(TimeOnly.MinValue), 0m)
                };
            })
            .ToList();

        var topFoods = await _dbContext.InvoiceItems
            .AsNoTracking()
            .Where(item => item.Invoice.InvoiceStatus == "paid" &&
                           item.Invoice.IssuedAt >= start && item.Invoice.IssuedAt < endExclusive)
            .GroupBy(item => new { item.FoodNameSnapshot, item.VariantSnapshot })
            .Select(group => new
            {
                FoodName = group.Key.FoodNameSnapshot,
                Variant = group.Key.VariantSnapshot,
                Quantity = group.Sum(item => item.Quantity),
                Revenue = group.Sum(item => item.LineTotal)
            })
            .OrderByDescending(item => item.Quantity)
            .ThenByDescending(item => item.Revenue)
            .Take(8)
            .ToListAsync();

        var paymentMethods = await _dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.PaymentStatus == "completed" &&
                              payment.Invoice.InvoiceStatus == "paid" &&
                              payment.PaidAt >= start && payment.PaidAt < endExclusive)
            .GroupBy(payment => payment.PaymentMethod)
            .Select(group => new
            {
                Method = group.Key,
                PaymentCount = group.Count(),
                Amount = group.Sum(payment => payment.Amount)
            })
            .OrderByDescending(row => row.Amount)
            .ToListAsync();

        var cancelledOrders = await _dbContext.Orders
            .AsNoTracking()
            .CountAsync(order => order.OrderStatus == "cancelled" &&
                                 order.CancelledAt >= start && order.CancelledAt < endExclusive);

        var locationRows = await currentInvoices
            .Select(invoice => new
            {
                invoice.InvoiceId,
                invoice.TotalAmount,
                Table = invoice.Order.OrderTables
                    .OrderByDescending(orderTable => orderTable.IsPrimary)
                    .ThenBy(orderTable => orderTable.AssignedAt)
                    .Select(orderTable => new
                    {
                        orderTable.TableId,
                        orderTable.Table.TableCode,
                        orderTable.Table.TableName,
                        orderTable.Table.AreaName
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        var salesByArea = locationRows
            .GroupBy(row => string.IsNullOrWhiteSpace(row.Table?.AreaName) ? "Chưa gán khu vực" : row.Table!.AreaName!.Trim())
            .Select(group => new
            {
                AreaName = group.Key,
                InvoiceCount = group.Count(),
                Revenue = group.Sum(row => row.TotalAmount),
                AverageInvoice = group.Count() == 0 ? 0m : Math.Round(group.Sum(row => row.TotalAmount) / group.Count(), 0)
            })
            .OrderByDescending(row => row.Revenue)
            .ToList();

        var salesByTable = locationRows
            .GroupBy(row => new
            {
                TableId = row.Table?.TableId,
                TableCode = row.Table?.TableCode,
                TableName = row.Table?.TableName,
                AreaName = row.Table?.AreaName
            })
            .Select(group => new
            {
                group.Key.TableId,
                TableCode = group.Key.TableCode ?? "-",
                TableName = group.Key.TableName ?? "Chưa gán bàn",
                AreaName = string.IsNullOrWhiteSpace(group.Key.AreaName) ? "Chưa gán khu vực" : group.Key.AreaName,
                InvoiceCount = group.Count(),
                Revenue = group.Sum(row => row.TotalAmount),
                AverageInvoice = group.Count() == 0 ? 0m : Math.Round(group.Sum(row => row.TotalAmount) / group.Count(), 0)
            })
            .OrderByDescending(row => row.Revenue)
            .ToList();

        return Ok(new
        {
            success = true,
            data = new
            {
                fromDate = startDate,
                toDate = endDate,
                summary = new
                {
                    grossRevenue,
                    discountAmount,
                    revenue,
                    invoiceCount,
                    averageInvoice = invoiceCount == 0 ? 0m : Math.Round(revenue / invoiceCount, 0),
                    cancelledOrders,
                    previousRevenue,
                    previousInvoiceCount,
                    revenueChangePercent = previousRevenue == 0m
                        ? (decimal?)null
                        : Math.Round((revenue - previousRevenue) / previousRevenue * 100m, 1)
                },
                dailyTrend,
                hourlyTrend,
                topFoods,
                paymentMethods,
                salesByArea,
                salesByTable
            }
        });
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProductsReport(
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null)
    {
        if (!TryResolvePeriod(fromDate, toDate, out var period, out var error))
            return BadRequest(new { success = false, message = error });

        var periodDays = period.ToDate.DayNumber - period.FromDate.DayNumber + 1;
        var previousFromDate = period.FromDate.AddDays(-periodDays);
        var previousToDate = period.FromDate.AddDays(-1);
        var previousStartUtc = period.StartUtc.AddDays(-periodDays);
        var sales = await _dbContext.InvoiceItems
            .AsNoTracking()
            .Where(item => item.Invoice.InvoiceStatus == "paid"
                && item.Invoice.IssuedAt >= previousStartUtc
                && item.Invoice.IssuedAt < period.EndExclusiveUtc)
            .Select(item => new ProductSaleLine
            {
                InvoiceIssuedAt = item.Invoice.IssuedAt,
                FoodId = item.FoodId,
                VariantId = item.VariantId,
                FoodName = item.Food != null ? item.Food.FoodName : item.FoodNameSnapshot,
                VariantName = item.Variant != null ? item.Variant.VariantName : item.VariantSnapshot,
                Unit = item.Variant != null ? item.Variant.Unit : string.Empty,
                CategoryId = item.Food != null ? item.Food.CategoryId : null,
                CategoryName = item.Food != null ? item.Food.Category.CategoryName : null,
                Quantity = item.Quantity,
                LineTotal = item.LineTotal,
                InvoiceSubtotal = item.Invoice.Subtotal,
                InvoiceDiscount = item.Invoice.DiscountAmount
            })
            .ToListAsync();

        var productRows = new List<ProductReportRow>();
        foreach (var group in sales.GroupBy(item => new
            {
                item.FoodId, item.VariantId, item.FoodName, item.VariantName,
                item.Unit, item.CategoryId, item.CategoryName
            }))
        {
            var currentLines = group.Where(item => item.InvoiceIssuedAt >= period.StartUtc).ToList();
            var previousLines = group.Where(item => item.InvoiceIssuedAt < period.StartUtc).ToList();
            var currentGrossRevenue = currentLines.Sum(item => item.LineTotal);
            var previousGrossRevenue = previousLines.Sum(item => item.LineTotal);
            var currentRevenue = ProductNetRevenue(currentLines);
            var previousRevenue = ProductNetRevenue(previousLines);
            var currentQuantity = currentLines.Sum(item => item.Quantity);
            var previousQuantity = previousLines.Sum(item => item.Quantity);

            productRows.Add(new ProductReportRow
            {
                FoodId = group.Key.FoodId,
                VariantId = group.Key.VariantId,
                FoodName = group.Key.FoodName,
                VariantName = group.Key.VariantName,
                Unit = group.Key.Unit,
                CategoryId = group.Key.CategoryId,
                CategoryName = group.Key.CategoryName ?? "Chưa phân loại",
                SoldQuantity = currentQuantity,
                PreviousSoldQuantity = previousQuantity,
                SoldQuantityChangePercent = PercentageChange(currentQuantity, previousQuantity),
                GrossRevenue = currentGrossRevenue,
                PreviousGrossRevenue = previousGrossRevenue,
                GrossRevenueChangePercent = PercentageChange(currentGrossRevenue, previousGrossRevenue),
                Revenue = currentRevenue,
                PreviousRevenue = previousRevenue,
                RevenueChangePercent = PercentageChange(currentRevenue, previousRevenue)
            });
        }

        var totalRevenue = productRows.Sum(row => row.Revenue);
        foreach (var row in productRows)
            row.ContributionPercent = totalRevenue > 0 ? Math.Round(row.Revenue / totalRevenue * 100m, 1) : null;

        productRows = productRows
            .OrderByDescending(row => row.SoldQuantity)
            .ThenByDescending(row => row.Revenue)
            .ToList();

        var categories = productRows
            .GroupBy(row => new { row.CategoryId, row.CategoryName })
            .Select(group => new
            {
                group.Key.CategoryId,
                CategoryName = group.Key.CategoryName,
                Revenue = group.Sum(row => row.Revenue),
                ContributionPercent = totalRevenue > 0
                    ? Math.Round(group.Sum(row => row.Revenue) / totalRevenue * 100m, 1)
                    : (decimal?)null,
                ProductCount = group.Count(row => row.SoldQuantity > 0)
            })
            .Where(category => category.Revenue > 0)
            .OrderByDescending(category => category.Revenue)
            .ToList();

        var previousQuantityTotal = sales.Where(item => item.InvoiceIssuedAt < period.StartUtc).Sum(item => item.Quantity);
        var previousGrossRevenueTotal = sales.Where(item => item.InvoiceIssuedAt < period.StartUtc).Sum(item => item.LineTotal);
        var previousRevenueTotal = ProductNetRevenue(sales.Where(item => item.InvoiceIssuedAt < period.StartUtc));
        var currentGrossRevenueTotal = productRows.Sum(row => row.GrossRevenue);
        var currentQuantityTotal = productRows.Sum(row => row.SoldQuantity);
        var bestSeller = productRows.FirstOrDefault(row => row.SoldQuantity > 0);

        var menuVariantRows = await _dbContext.FoodVariants.AsNoTracking()
            .Where(variant => variant.IsActive && variant.Food.IsActive
                && (variant.Food.FoodStatus == "available" || variant.Food.FoodStatus == "sold_out")
                && variant.Food.Category.IsActive)
            .Select(variant => new ProductReportRow
            {
                FoodId = variant.FoodId,
                VariantId = variant.VariantId,
                FoodName = variant.Food.FoodName,
                VariantName = variant.VariantName,
                Unit = variant.Unit,
                CategoryId = variant.Food.CategoryId,
                CategoryName = variant.Food.Category.CategoryName
            })
            .ToListAsync();
        var menuStandaloneRows = await _dbContext.Foods.AsNoTracking()
            .Where(food => food.IsActive
                && (food.FoodStatus == "available" || food.FoodStatus == "sold_out")
                && food.Category.IsActive
                && !food.FoodVariants.Any(variant => variant.IsActive))
            .Select(food => new ProductReportRow
            {
                FoodId = food.FoodId,
                VariantId = null,
                FoodName = food.FoodName,
                VariantName = "Mặc định",
                Unit = "phần",
                CategoryId = food.CategoryId,
                CategoryName = food.Category.CategoryName
            })
            .ToListAsync();
        var menuItems = menuVariantRows.Concat(menuStandaloneRows).ToList();
        foreach (var menuItem in menuItems)
        {
            if (!productRows.Any(row => row.FoodId == menuItem.FoodId && row.VariantId == menuItem.VariantId))
                productRows.Add(menuItem);
        }
        var menuCounts = menuItems
            .Where(item => item.CategoryId.HasValue)
            .GroupBy(item => item.CategoryId)
            .ToDictionary(group => group.Key!.Value, group => group.Count());
        var categoryCatalog = await _dbContext.FoodCategories.AsNoTracking()
            .Select(category => new { category.CategoryId, category.CategoryName })
            .ToListAsync();
        var menuCategories = categoryCatalog
            .Where(category => menuCounts.ContainsKey(category.CategoryId)
                || productRows.Any(row => row.CategoryId == category.CategoryId))
            .Select(category => new
            {
                CategoryId = (int?)category.CategoryId,
                category.CategoryName,
                MenuItemCount = menuCounts.GetValueOrDefault(category.CategoryId)
            })
            .ToList();
        if (productRows.Any(row => row.CategoryId == null))
            menuCategories.Add(new { CategoryId = (int?)null, CategoryName = "Chưa phân loại", MenuItemCount = 0 });
        menuCategories = menuCategories.OrderBy(category => category.CategoryName).ToList();

        return Ok(new
        {
            success = true,
            data = new
            {
                fromDate = period.FromDate,
                toDate = period.ToDate,
                previousFromDate,
                previousToDate,
                summary = new
                {
                    productCount = productRows.Count(row => row.SoldQuantity > 0),
                    menuItemCount = menuCounts.Values.Sum(),
                    soldQuantity = currentQuantityTotal,
                    previousSoldQuantity = previousQuantityTotal,
                    soldQuantityChangePercent = PercentageChange(currentQuantityTotal, previousQuantityTotal),
                    grossRevenue = currentGrossRevenueTotal,
                    previousGrossRevenue = previousGrossRevenueTotal,
                    grossRevenueChangePercent = PercentageChange(currentGrossRevenueTotal, previousGrossRevenueTotal),
                    revenue = totalRevenue,
                    previousRevenue = previousRevenueTotal,
                    revenueChangePercent = PercentageChange(totalRevenue, previousRevenueTotal),
                    bestSeller = bestSeller == null ? null : new
                    {
                        bestSeller.FoodName,
                        bestSeller.VariantName,
                        bestSeller.Unit,
                        bestSeller.SoldQuantity
                    }
                },
                products = productRows,
                categories,
                menuCategories
            }
        });
    }

    [HttpGet("finance")]
    public async Task<IActionResult> GetFinanceReport(
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null)
    {
        if (!TryResolvePeriod(fromDate, toDate, out var period, out var error))
            return BadRequest(new { success = false, message = error });

        var paidInvoices = await _dbContext.Invoices
            .AsNoTracking()
            .Where(invoice => invoice.InvoiceStatus == "paid"
                && invoice.IssuedAt >= period.StartUtc
                && invoice.IssuedAt < period.EndExclusiveUtc)
            .Select(invoice => new { invoice.TotalAmount })
            .ToListAsync();

        var reconciliation = await _dbContext.KitchenReconciliationLines.AsNoTracking()
            .Where(line => line.ReconciledAt >= period.StartUtc && line.ReconciledAt < period.EndExclusiveUtc)
            .Select(line => new
            {
                line.UsedQuantity, line.ReturnedQuantity, line.WastedQuantity,
                SourceQuantity = line.SourceDetail.Quantity,
                SourceAmount = line.SourceDetail.Subtotal
            })
            .ToListAsync();
        var usedIngredientCost = reconciliation.Sum(line => ReportCalculations.ProportionalAmount(
            line.SourceQuantity, line.SourceAmount, line.UsedQuantity));
        var kitchenWasteValue = reconciliation.Sum(line => ReportCalculations.ProportionalAmount(
            line.SourceQuantity, line.SourceAmount, line.WastedQuantity));
        var returnedIngredientValue = reconciliation.Sum(line => ReportCalculations.ProportionalAmount(
            line.SourceQuantity, line.SourceAmount, line.ReturnedQuantity));

        var pendingDetails = await _dbContext.StockReceiptDetails.AsNoTracking()
            .Where(detail => detail.Receipt.ReceiptType == "export_kitchen"
                && detail.Receipt.Status == "approved"
                && detail.Receipt.ApprovedAt < period.EndExclusiveUtc
                && !_dbContext.KitchenReconciliationLines.Any(line => line.SourceDetailId == detail.DetailId
                    && line.ReconciledAt < period.EndExclusiveUtc))
            .Select(detail => new { detail.Quantity, detail.Subtotal })
            .ToListAsync();
        var pendingIssuedCost = pendingDetails.Sum(detail => detail.Subtotal);
        var missingCostLineCount = reconciliation.Count(line => line.SourceQuantity <= 0
            || (line.SourceAmount <= 0 && line.UsedQuantity + line.WastedQuantity > 0));

        var prepDetails = await _dbContext.StockReceiptDetails
            .AsNoTracking()
            .Where(detail => detail.Receipt.ReceiptType == "import_supplier"
                && detail.Receipt.Status == "approved"
                && detail.Receipt.ApprovedAt >= period.StartUtc
                && detail.Receipt.ApprovedAt < period.EndExclusiveUtc)
            .Select(detail => new
            {
                detail.WastageQuantity,
                GrossQuantity = detail.GrossQuantity ?? detail.Quantity,
                detail.WastagePercent,
                detail.Subtotal,
                detail.InventoryItemId,
                ItemName = detail.InventoryItem.ItemName,
                BaseUnit = detail.InventoryItem.BaseUnit
            })
            .ToListAsync();
        var prepWastageValue = prepDetails.Sum(detail => ReportCalculations.ProportionalAmount(
            detail.GrossQuantity, detail.Subtotal, detail.WastageQuantity));
        var expectedPrepWastageValue = prepDetails.Sum(detail => detail.Subtotal * detail.WastagePercent / 100m);
        var purchaseValue = prepDetails.Sum(detail => detail.Subtotal);
        var prepWastageByItem = prepDetails
            .Where(detail => detail.WastageQuantity > 0)
            .GroupBy(detail => new { detail.InventoryItemId, detail.ItemName, detail.BaseUnit })
            .Select(group => new
            {
                group.Key.InventoryItemId,
                group.Key.ItemName,
                group.Key.BaseUnit,
                Quantity = group.Sum(detail => detail.WastageQuantity),
                GrossQuantity = group.Sum(detail => detail.GrossQuantity),
                ExpectedQuantity = group.Sum(detail => detail.GrossQuantity * detail.WastagePercent / 100m),
                Value = group.Sum(detail => ReportCalculations.ProportionalAmount(
                    detail.GrossQuantity, detail.Subtotal, detail.WastageQuantity)),
                ExcessValue = group.Sum(detail => ReportCalculations.ProportionalAmount(detail.GrossQuantity,
                    detail.Subtotal, Math.Max(0m, detail.WastageQuantity - detail.GrossQuantity * detail.WastagePercent / 100m)))
            })
            .OrderByDescending(row => row.Value)
            .ToList();

        var kitchenWasteReceiptIds = await _dbContext.KitchenReconciliationLines
            .AsNoTracking()
            .Where(line => line.WasteReceiptId.HasValue)
            .Select(line => line.WasteReceiptId!.Value)
            .Distinct()
            .ToListAsync();
        var warehouseWasteReceipts = _dbContext.StockReceipts
            .AsNoTracking()
            .Where(receipt => receipt.ReceiptType == "waste_disposal"
                && receipt.Status == "approved"
                && receipt.ApprovedAt >= period.StartUtc
                && receipt.ApprovedAt < period.EndExclusiveUtc
                && !kitchenWasteReceiptIds.Contains(receipt.ReceiptId));
        var warehouseWasteValue = await warehouseWasteReceipts.SumAsync(receipt => (decimal?)receipt.TotalAmount) ?? 0m;
        var missingWarehouseCostLineCount = await warehouseWasteReceipts
            .SelectMany(receipt => receipt.StockReceiptDetails)
            .CountAsync(detail => detail.Quantity > 0 && detail.Subtotal <= 0);

        var paymentMethods = await _dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.PaymentStatus == "completed"
                && payment.Invoice.InvoiceStatus == "paid"
                && payment.PaidAt >= period.StartUtc
                && payment.PaidAt < period.EndExclusiveUtc)
            .GroupBy(payment => payment.PaymentMethod)
            .Select(group => new
            {
                Method = group.Key,
                PaymentCount = group.Count(),
                Amount = group.Sum(payment => payment.Amount)
            })
            .OrderByDescending(row => row.Amount)
            .ToListAsync();

        var revenue = paidInvoices.Sum(invoice => invoice.TotalAmount);
        var ingredientCost = usedIngredientCost;
        var wasteValue = kitchenWasteValue + warehouseWasteValue;
        var hasCompleteCosts = pendingDetails.Count == 0 && missingCostLineCount == 0 && missingWarehouseCostLineCount == 0
            && (revenue <= 0 || reconciliation.Count > 0);
        decimal? grossProfit = hasCompleteCosts ? revenue - ingredientCost : null;
        return Ok(new
        {
            success = true,
            data = new
            {
                fromDate = period.FromDate,
                toDate = period.ToDate,
                note = "Chi phí dùng, hoàn và hủy bếp ghi theo ngày chốt đối soát. Phiếu chưa chốt theo dõi riêng, không cộng vào chi phí đã dùng. Hao hụt sơ chế đã nằm trong giá vốn lô sạch. Kết quả chưa bao gồm lương, điện và chi phí mặt bằng.",
                summary = new
                {
                    revenue,
                    ingredientCost,
                    usedIngredientCost,
                    pendingIssuedCost,
                    grossProfit,
                    grossMarginPercent = revenue <= 0 || !grossProfit.HasValue
                        ? (decimal?)null : Math.Round(grossProfit.Value / revenue * 100m, 1),
                    kitchenWasteValue,
                    warehouseWasteValue,
                    wasteValue,
                    operatingResultAfterWaste = grossProfit - wasteValue,
                    prepWastageValue,
                    expectedPrepWastageValue,
                    excessPrepWastageValue = prepWastageByItem.Sum(item => item.ExcessValue),
                    prepWastageItemCount = prepWastageByItem.Count,
                    purchaseValue,
                    reconciledLineCount = reconciliation.Count,
                    pendingLineCount = pendingDetails.Count,
                    missingCostLineCount,
                    missingWarehouseCostLineCount,
                    hasCompleteCosts,
                    returnedIngredientValue
                },
                paymentMethods,
                prepWastageByItem
            }
        });
    }

    [HttpGet("inventory-ledger")]
    public async Task<IActionResult> GetInventoryLedgerReport(
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        [FromQuery] int? inventoryItemId = null,
        [FromQuery] string? search = null)
    {
        if (!TryResolvePeriod(fromDate, toDate, out var period, out var error))
            return BadRequest(new { success = false, message = error });

        var cleanSearch = search?.Trim();
        var today = DateTime.UtcNow.AddHours(VietnamUtcOffsetHours).Date;
        var items = await _dbContext.InventoryItems.AsNoTracking()
            .Where(item => (item.IsActive || item.CurrentQuantity != 0
                    || _dbContext.StockMovements.Any(movement => movement.InventoryItemId == item.InventoryItemId
                        && movement.CreatedAt < period.EndExclusiveUtc))
                && (!inventoryItemId.HasValue || item.InventoryItemId == inventoryItemId.Value)
                && (string.IsNullOrWhiteSpace(cleanSearch) || item.ItemName.Contains(cleanSearch)))
            .Select(item => new
            {
                item.InventoryItemId,
                item.ItemName,
                item.BaseUnit,
                item.CurrentQuantity,
                item.MinimumQuantity,
                item.IsActive,
                RemainingBatchCount = item.InventoryBatches.Count(batch => batch.QuantityRemaining > 0),
                UsableBatchQuantity = item.InventoryBatches
                    .Where(batch => batch.Status == "active" && batch.QuantityRemaining > 0
                        && batch.ExpiryDate.Date >= today)
                    .Sum(batch => (decimal?)batch.QuantityRemaining) ?? 0m
            })
            .ToListAsync();

        var movements = await _dbContext.StockMovements.AsNoTracking()
            .Where(movement => (!inventoryItemId.HasValue || movement.InventoryItemId == inventoryItemId.Value)
                && (string.IsNullOrWhiteSpace(cleanSearch) || movement.InventoryItem.ItemName.Contains(cleanSearch)))
            .Select(movement => new
            {
                movement.StockMovementId,
                movement.InventoryItemId,
                ItemName = movement.InventoryItem.ItemName,
                BaseUnit = movement.InventoryItem.BaseUnit,
                movement.MovementType,
                movement.Quantity,
                movement.UnitCost,
                movement.InventoryItem.ConversionRate,
                movement.Reason,
                movement.ReferenceType,
                movement.ReferenceId,
                movement.CreatedAt,
                BatchCode = movement.Batch == null ? null : movement.Batch.BatchCode,
                ExpiryDate = movement.Batch == null ? (DateTime?)null : movement.Batch.ExpiryDate
            })
            .OrderBy(movement => movement.CreatedAt)
            .ThenBy(movement => movement.StockMovementId)
            .ToListAsync();

        var movementsByItem = movements.ToLookup(movement => movement.InventoryItemId);
        var rows = items.Select(item =>
        {
            var itemMovements = movementsByItem[item.InventoryItemId];
            var opening = itemMovements.Where(movement => movement.CreatedAt < period.StartUtc)
                .Sum(movement => ReportCalculations.MovementDelta(movement.MovementType, movement.Quantity, movement.Reason));
            var periodMovements = itemMovements.Where(movement => movement.CreatedAt >= period.StartUtc
                && movement.CreatedAt < period.EndExclusiveUtc).ToList();
            var imports = periodMovements.Where(movement => movement.MovementType == "import"
                && !IsReturnMovement(movement.Reason)).Sum(movement => movement.Quantity);
            var returns = periodMovements.Where(movement => movement.MovementType == "import"
                && IsReturnMovement(movement.Reason)).Sum(movement => movement.Quantity);
            var exports = periodMovements.Where(movement => movement.MovementType == "export").Sum(movement => movement.Quantity);
            var waste = periodMovements.Where(movement => movement.MovementType == "waste").Sum(movement => movement.Quantity);
            var adjustmentIncrease = periodMovements.Where(movement => ReportCalculations.IsPositiveAdjustment(movement.MovementType, movement.Reason))
                .Sum(movement => movement.Quantity);
            var adjustmentDecrease = periodMovements.Where(movement => movement.MovementType == "adjustment"
                && !ReportCalculations.IsPositiveAdjustment(movement.MovementType, movement.Reason)).Sum(movement => movement.Quantity);
            var closing = opening + imports + returns + adjustmentIncrease - exports - waste - adjustmentDecrease;
            var currentLedgerQuantity = itemMovements.Sum(movement => ReportCalculations.MovementDelta(
                movement.MovementType, movement.Quantity, movement.Reason));
            var currentMismatch = Math.Abs(currentLedgerQuantity - item.CurrentQuantity) > 0.0001m;
            var availableQuantity = item.RemainingBatchCount == 0 ? item.CurrentQuantity : item.UsableBatchQuantity;
            return new
            {
                item.InventoryItemId,
                item.ItemName,
                item.BaseUnit,
                OpeningQuantity = decimal.Round(opening, 4),
                ImportQuantity = decimal.Round(imports, 4),
                ReturnQuantity = decimal.Round(returns, 4),
                ExportQuantity = decimal.Round(exports, 4),
                WasteQuantity = decimal.Round(waste, 4),
                AdjustmentIncrease = decimal.Round(adjustmentIncrease, 4),
                AdjustmentDecrease = decimal.Round(adjustmentDecrease, 4),
                ClosingQuantity = decimal.Round(closing, 4),
                CurrentQuantity = item.CurrentQuantity,
                AvailableQuantity = availableQuantity,
                CurrentLedgerQuantity = decimal.Round(currentLedgerQuantity, 4),
                item.MinimumQuantity,
                CurrentMismatch = currentMismatch,
                IsBelowMinimum = item.IsActive && availableQuantity <= item.MinimumQuantity,
                MovementCount = periodMovements.Count
            };
        }).Where(row => row.MovementCount > 0 || row.OpeningQuantity != 0 || row.ClosingQuantity != 0
            || row.CurrentQuantity != 0 || row.CurrentMismatch).ToList();

        var quantitiesByUnit = rows.GroupBy(row => row.BaseUnit)
            .OrderBy(group => group.Key)
            .Select(group => new
            {
                BaseUnit = group.Key,
                ItemCount = group.Count(),
                MovementCount = group.Sum(row => row.MovementCount),
                OpeningQuantity = group.Sum(row => row.OpeningQuantity),
                ClosingQuantity = group.Sum(row => row.ClosingQuantity),
                CurrentQuantity = group.Sum(row => row.CurrentQuantity),
                ImportedQuantity = group.Sum(row => row.ImportQuantity),
                ReturnedQuantity = group.Sum(row => row.ReturnQuantity),
                ExportedQuantity = group.Sum(row => row.ExportQuantity),
                WastedQuantity = group.Sum(row => row.WasteQuantity),
                AdjustmentIncrease = group.Sum(row => row.AdjustmentIncrease),
                AdjustmentDecrease = group.Sum(row => row.AdjustmentDecrease),
                AdjustmentQuantity = group.Sum(row => row.AdjustmentIncrease - row.AdjustmentDecrease)
            }).ToList();

        var periodMovementsQuery = movements.Where(movement => movement.CreatedAt >= period.StartUtc
            && movement.CreatedAt < period.EndExclusiveUtc);
        var dailyTrend = periodMovementsQuery.Select(movement => new
            {
                Date = DateOnly.FromDateTime(movement.CreatedAt.AddHours(VietnamUtcOffsetHours)),
                movement.MovementType,
                IsReturn = IsReturnMovement(movement.Reason),
                Value = movement.UnitCost > 0
                    ? InventoryCostHelper.CalculateAmount(movement.Quantity, movement.UnitCost.Value, movement.ConversionRate)
                    : 0m,
                MissingCost = (movement.MovementType == "import" || movement.MovementType == "export"
                    || movement.MovementType == "waste") && movement.UnitCost is null or <= 0
            })
            .GroupBy(movement => movement.Date)
            .OrderBy(group => group.Key)
            .Select(group => new
            {
                Date = group.Key,
                ImportValue = group.Where(movement => movement.MovementType == "import" && !movement.IsReturn)
                    .Sum(movement => movement.Value),
                ReturnValue = group.Where(movement => movement.IsReturn).Sum(movement => movement.Value),
                ExportValue = group.Where(movement => movement.MovementType == "export").Sum(movement => movement.Value),
                WasteValue = group.Where(movement => movement.MovementType == "waste").Sum(movement => movement.Value),
                MovementCount = group.Count(),
                MissingCostCount = group.Count(movement => movement.MissingCost)
            })
            .ToList();
        var periodMovementRows = periodMovementsQuery
            .Select(movement => new
            {
                movement.StockMovementId,
                movement.InventoryItemId,
                movement.ItemName,
                movement.BaseUnit,
                movement.MovementType,
                MovementLabel = MovementLabel(movement.MovementType, movement.Reason),
                Quantity = movement.Quantity,
                QuantityDelta = ReportCalculations.MovementDelta(movement.MovementType, movement.Quantity, movement.Reason),
                movement.UnitCost,
                movement.Reason,
                movement.ReferenceType,
                movement.ReferenceId,
                movement.CreatedAt,
                movement.BatchCode,
                movement.ExpiryDate
            })
            .OrderByDescending(movement => movement.CreatedAt)
            .ThenByDescending(movement => movement.StockMovementId)
            .Take(1000)
            .ToList();

        return Ok(new
        {
            success = true,
            data = new
            {
                fromDate = period.FromDate,
                toDate = period.ToDate,
                cutoffUtc = period.EndExclusiveUtc,
                summary = new
                {
                    itemCount = rows.Count,
                    movementCount = periodMovementsQuery.Count(),
                    belowMinimumCount = rows.Count(row => row.IsBelowMinimum),
                    mismatchCount = rows.Count(row => row.CurrentMismatch),
                    quantitiesByUnit
                },
                warnings = rows.Where(row => row.CurrentMismatch || row.IsBelowMinimum).Select(row => new
                {
                    row.InventoryItemId,
                    row.ItemName,
                    row.BaseUnit,
                    row.CurrentQuantity,
                    row.AvailableQuantity,
                    row.CurrentLedgerQuantity,
                    row.ClosingQuantity,
                    row.MinimumQuantity,
                    Type = row.CurrentMismatch ? "ledger_mismatch" : "below_minimum",
                    Message = row.CurrentMismatch
                        ? "Tồn tính từ sổ biến động không khớp CurrentQuantity; cần kiểm tra lịch sử hoặc kiểm kê."
                        : "Tồn lô còn dùng được thấp hơn hoặc bằng ngưỡng tối thiểu."
                }).ToList(),
                items = rows,
                movements = periodMovementRows,
                dailyTrend
            }
        });
    }

    [HttpGet("kitchen-reconciliation-summary")]
    public async Task<IActionResult> GetKitchenReconciliationSummary(
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        [FromQuery] string? status = null)
    {
        if (!TryResolvePeriod(fromDate, toDate, out var period, out var error))
            return BadRequest(new { success = false, message = error });

        var exportDetails = await _dbContext.StockReceiptDetails.AsNoTracking()
            .Where(detail => detail.Receipt.ReceiptType == "export_kitchen"
                && detail.Receipt.Status == "approved"
                && detail.Receipt.ApprovedAt.HasValue
                && detail.Receipt.ApprovedAt < period.EndExclusiveUtc)
            .Select(detail => new
            {
                detail.DetailId,
                detail.ReceiptId,
                ReceiptCode = detail.Receipt.ReceiptCode,
                ApprovedAt = detail.Receipt.ApprovedAt!.Value,
                detail.InventoryItemId,
                ItemName = detail.InventoryItem.ItemName,
                BaseUnit = detail.InventoryItem.BaseUnit,
                detail.BatchId,
                BatchCode = detail.Batch == null ? null : detail.Batch.BatchCode,
                BatchExpiryDate = detail.Batch == null ? (DateTime?)null : detail.Batch.ExpiryDate,
                SourceQuantity = detail.Quantity,
                SourceAmount = detail.Subtotal,
                detail.UnitCost,
                detail.BomSnapshotJson
            })
            .ToListAsync();

        var reconciliation = await _dbContext.KitchenReconciliationLines.AsNoTracking()
            .Where(line => line.ReconciledAt < period.EndExclusiveUtc)
            .Select(line => new
            {
                line.SourceDetailId,
                line.UsedQuantity,
                line.ReturnedQuantity,
                line.WastedQuantity,
                line.WasteReason,
                line.ReconciledAt,
                ReturnReceiptCode = line.ReturnReceipt == null ? null : line.ReturnReceipt.ReceiptCode,
                WasteReceiptCode = line.WasteReceipt == null ? null : line.WasteReceipt.ReceiptCode
            })
            .ToListAsync();
        var reconciliationByDetail = reconciliation.ToDictionary(line => line.SourceDetailId);

        var kitchenRows = exportDetails
            .Where(detail => reconciliationByDetail.TryGetValue(detail.DetailId, out var line)
                && line.ReconciledAt >= period.StartUtc
                && (string.IsNullOrWhiteSpace(status) || status.Equals("closed", StringComparison.OrdinalIgnoreCase)))
            .Select(detail =>
            {
                var line = reconciliationByDetail[detail.DetailId];
                return new
                {
                    detail.DetailId,
                    detail.ReceiptCode,
                    detail.ApprovedAt,
                    detail.InventoryItemId,
                    detail.ItemName,
                    detail.BaseUnit,
                    detail.BatchCode,
                    detail.BatchExpiryDate,
                    detail.SourceQuantity,
                    line.UsedQuantity,
                    line.ReturnedQuantity,
                    line.WastedQuantity,
                    line.WasteReason,
                    line.ReconciledAt,
                    line.ReturnReceiptCode,
                    line.WasteReceiptCode,
                    IssuedPortions = SnapshotPortions(detail.BomSnapshotJson),
                    UsedValue = ReportCalculations.ProportionalAmount(detail.SourceQuantity, detail.SourceAmount, line.UsedQuantity),
                    ReturnedValue = ReportCalculations.ProportionalAmount(detail.SourceQuantity, detail.SourceAmount, line.ReturnedQuantity),
                    WasteValue = ReportCalculations.ProportionalAmount(detail.SourceQuantity, detail.SourceAmount, line.WastedQuantity)
                };
            })
            .Where(row => string.IsNullOrWhiteSpace(status) || status.Equals("closed", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var pending = exportDetails
            .Where(detail => !reconciliationByDetail.ContainsKey(detail.DetailId)
                && (string.IsNullOrWhiteSpace(status) || status.Equals("pending", StringComparison.OrdinalIgnoreCase)))
            .Select(detail => new
            {
                detail.DetailId,
                detail.ReceiptCode,
                detail.ApprovedAt,
                detail.InventoryItemId,
                detail.ItemName,
                detail.BaseUnit,
                detail.BatchCode,
                detail.BatchExpiryDate,
                IssuedQuantity = detail.SourceQuantity,
                IssuedValue = detail.SourceAmount,
                IssuedPortions = SnapshotPortions(detail.BomSnapshotJson)
            })
            .ToList();

        var prepDetails = await _dbContext.StockReceiptDetails.AsNoTracking()
            .Where(detail => detail.Receipt.ReceiptType == "import_supplier"
                && detail.Receipt.Status == "approved"
                && detail.Receipt.ApprovedAt >= period.StartUtc
                && detail.Receipt.ApprovedAt < period.EndExclusiveUtc)
            .Select(detail => new
            {
                detail.InventoryItemId,
                ItemName = detail.InventoryItem.ItemName,
                BaseUnit = detail.InventoryItem.BaseUnit,
                GrossQuantity = detail.GrossQuantity ?? detail.Quantity,
                ActualQuantity = detail.Quantity,
                detail.WastageQuantity,
                detail.WastagePercent,
                detail.Subtotal,
                detail.AdjustmentNote
            })
            .ToListAsync();
        var prepByItem = prepDetails.GroupBy(detail => new { detail.InventoryItemId, detail.ItemName, detail.BaseUnit })
            .Select(group =>
            {
                var gross = group.Sum(detail => detail.GrossQuantity);
                var expected = group.Sum(detail => detail.GrossQuantity * detail.WastagePercent / 100m);
                var actual = group.Sum(detail => detail.WastageQuantity);
                var excess = group.Sum(detail => Math.Max(detail.WastageQuantity
                    - detail.GrossQuantity * detail.WastagePercent / 100m, 0m));
                return new
                {
                    group.Key.InventoryItemId,
                    group.Key.ItemName,
                    group.Key.BaseUnit,
                    GrossQuantity = gross,
                    ExpectedWastageQuantity = expected,
                    ActualWastageQuantity = actual,
                    ExcessQuantity = excess,
                    ExpectedUsableQuantity = gross - expected,
                    ActualUsableQuantity = group.Sum(detail => detail.ActualQuantity),
                    ExcessValue = group.Sum(detail => ReportCalculations.ProportionalAmount(
                        detail.GrossQuantity, detail.Subtotal, Math.Max(detail.WastageQuantity
                            - detail.GrossQuantity * detail.WastagePercent / 100m, 0m))),
                    Reasons = group.Where(detail => !string.IsNullOrWhiteSpace(detail.AdjustmentNote))
                        .Select(detail => detail.AdjustmentNote).Distinct().ToList()
                };
            })
            .OrderByDescending(row => row.ExcessQuantity)
            .ToList();

        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(VietnamUtcOffsetHours));
        var liveBatchWarningsQuery = _dbContext.InventoryBatches.AsNoTracking()
            .Where(batch => batch.QuantityRemaining > 0
                && (batch.ExpiryDate < today.ToDateTime(TimeOnly.MinValue).AddDays(8)
                    || batch.BatchCode.StartsWith("TH-BEP-")))
            .Select(batch => new
            {
                batch.BatchId,
                batch.BatchCode,
                batch.InventoryItemId,
                ItemName = batch.InventoryItem.ItemName,
                BaseUnit = batch.InventoryItem.BaseUnit,
                batch.QuantityRemaining,
                batch.ExpiryDate,
                batch.ReceivedDate,
                IsExpired = batch.ExpiryDate.Date < today.ToDateTime(TimeOnly.MinValue).Date,
                IsExpiringSoon = batch.ExpiryDate.Date >= today.ToDateTime(TimeOnly.MinValue).Date
                    && batch.ExpiryDate.Date <= today.ToDateTime(TimeOnly.MinValue).Date.AddDays(7),
                IsReturnBatch = batch.BatchCode.StartsWith("TH-BEP-")
            });
        var expiredBatchCount = await liveBatchWarningsQuery.CountAsync(batch => batch.IsExpired);
        var expiringBatchCount = await liveBatchWarningsQuery.CountAsync(batch => batch.IsExpiringSoon);
        var returnUseNextDayCount = await liveBatchWarningsQuery.CountAsync(batch => batch.IsReturnBatch
            && batch.ReceivedDate.AddHours(VietnamUtcOffsetHours).Date == today.ToDateTime(TimeOnly.MinValue).Date);
        var liveBatchWarnings = await liveBatchWarningsQuery
            .OrderBy(batch => batch.ExpiryDate)
            .ThenBy(batch => batch.BatchId)
            .Take(100)
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                fromDate = period.FromDate,
                toDate = period.ToDate,
                currentWarningDate = today,
                summary = new
                {
                    prepItemCount = prepByItem.Count,
                    prepExcessItemCount = prepByItem.Count(row => row.ExcessQuantity > 0),
                    prepExcessByUnit = prepByItem.GroupBy(row => row.BaseUnit).OrderBy(group => group.Key)
                        .Select(group => new
                        {
                            BaseUnit = group.Key,
                            ItemCount = group.Count(row => row.ExcessQuantity > 0),
                            ExcessQuantity = group.Sum(row => row.ExcessQuantity)
                        }).ToList(),
                    kitchenClosedLineCount = kitchenRows.Count,
                    usedValue = kitchenRows.Sum(row => row.UsedValue),
                    returnedValue = kitchenRows.Sum(row => row.ReturnedValue),
                    wastedValue = kitchenRows.Sum(row => row.WasteValue),
                    pendingLineCount = pending.Count,
                    pendingValue = pending.Sum(row => row.IssuedValue),
                    expiredBatchCount,
                    expiringBatchCount,
                    returnUseNextDayCount
                },
                prepByItem,
                kitchenRows,
                pending,
                liveBatchWarnings
            }
        });
    }

    [HttpGet("payment-reconciliation")]
    public async Task<IActionResult> GetPaymentReconciliationReport(
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        [FromQuery] string? status = null,
        [FromQuery] string? paymentMethod = null,
        [FromQuery] string? search = null)
    {
        if (!TryResolvePeriod(fromDate, toDate, out var period, out var error))
            return BadRequest(new { success = false, message = error });

        var invoices = await _dbContext.Invoices.AsNoTracking()
            .Where(invoice => invoice.IssuedAt >= period.StartUtc && invoice.IssuedAt < period.EndExclusiveUtc)
            .Select(invoice => new
            {
                invoice.InvoiceId,
                invoice.InvoiceCode,
                invoice.IssuedAt,
                invoice.InvoiceStatus,
                invoice.Subtotal,
                invoice.DiscountAmount,
                invoice.PointsUsed,
                invoice.TotalAmount,
                invoice.OrderId,
                OrderStatus = invoice.Order.OrderStatus
            })
            .ToListAsync();
        var invoiceIds = invoices.Select(invoice => invoice.InvoiceId).ToList();
        var payments = await _dbContext.Payments.AsNoTracking()
            .Where(payment => (invoiceIds.Contains(payment.InvoiceId) && payment.PaidAt < period.EndExclusiveUtc)
                || (payment.PaidAt >= period.StartUtc && payment.PaidAt < period.EndExclusiveUtc))
            .Select(payment => new
            {
                payment.PaymentId,
                payment.InvoiceId,
                payment.PaymentMethod,
                payment.Amount,
                payment.TransactionCode,
                payment.PaymentStatus,
                payment.PaidAt,
                InvoiceCode = payment.Invoice.InvoiceCode,
                OrderId = payment.Invoice.OrderId,
                OrderStatus = payment.Invoice.Order.OrderStatus,
                TableDisplay = string.Join(", ", payment.Invoice.Order.OrderTables.Select(orderTable => orderTable.Table.TableName)),
                CustomerName = payment.Invoice.Customer != null ? payment.Invoice.Customer.FullName : "Khách vãng lai",
                CustomerPhone = payment.Invoice.Customer != null ? payment.Invoice.Customer.Phone : null
            })
            .ToListAsync();

        var duplicateCodes = payments.Where(payment => !string.IsNullOrWhiteSpace(payment.TransactionCode))
            .GroupBy(payment => payment.TransactionCode!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var cleanSearch = search?.Trim();
        var rows = invoices.Select(invoice =>
        {
            var linkedPayments = payments.Where(payment => payment.InvoiceId == invoice.InvoiceId).ToList();
            var successfulAmount = linkedPayments.Where(payment => payment.PaymentStatus == "completed").Sum(payment => payment.Amount);
            var difference = successfulAmount - invoice.TotalAmount;
            var duplicate = linkedPayments.Any(payment => !string.IsNullOrWhiteSpace(payment.TransactionCode)
                && duplicateCodes.Contains(payment.TransactionCode.Trim()));
            var mismatch = invoice.InvoiceStatus == "paid" && invoice.OrderStatus is "cancelled" or "merged";
            var rowStatus = invoice.InvoiceStatus != "paid" ? "invoice_not_paid"
                : successfulAmount == 0m ? "missing_payment"
                : difference < -0.01m ? "underpaid"
                : difference > 0.01m ? "overpaid"
                : duplicate ? "duplicate_transaction" : mismatch ? "order_mismatch" : "balanced";
            return new
            {
                invoice.InvoiceId,
                invoice.InvoiceCode,
                invoice.IssuedAt,
                invoice.InvoiceStatus,
                invoice.Subtotal,
                invoice.DiscountAmount,
                invoice.PointsUsed,
                invoice.TotalAmount,
                invoice.OrderId,
                invoice.OrderStatus,
                SuccessfulPaymentAmount = successfulAmount,
                Difference = difference,
                PaymentCount = linkedPayments.Count,
                PaymentMethods = linkedPayments.Select(payment => payment.PaymentMethod).Distinct().ToList(),
                ReconciliationStatus = rowStatus,
                HasDuplicateTransaction = duplicate,
                HasOrderMismatch = mismatch
            };
        })
        .Where(row => (string.IsNullOrWhiteSpace(status) || row.ReconciliationStatus.Equals(status, StringComparison.OrdinalIgnoreCase))
            && (string.IsNullOrWhiteSpace(paymentMethod) || row.PaymentMethods.Contains(paymentMethod, StringComparer.OrdinalIgnoreCase))
            && (string.IsNullOrWhiteSpace(cleanSearch) || row.InvoiceCode.Contains(cleanSearch, StringComparison.OrdinalIgnoreCase)))
        .OrderByDescending(row => row.IssuedAt)
        .ToList();

        var periodPayments = payments.Where(payment => payment.PaidAt >= period.StartUtc
                && payment.PaidAt < period.EndExclusiveUtc
                && payment.PaymentStatus == "completed")
            .ToList();
        var periodPaymentCount = payments.Count(payment => payment.PaidAt >= period.StartUtc
            && payment.PaidAt < period.EndExclusiveUtc);
        var paymentEvents = payments.Where(payment => payment.PaidAt >= period.StartUtc
                && payment.PaidAt < period.EndExclusiveUtc)
            .OrderByDescending(payment => payment.PaidAt)
            .Take(1000)
            .Select(payment => new
            {
                payment.PaymentId,
                payment.InvoiceId,
                payment.InvoiceCode,
                payment.OrderId,
                payment.OrderStatus,
                payment.TableDisplay,
                payment.CustomerName,
                payment.CustomerPhone,
                payment.PaymentMethod,
                payment.Amount,
                payment.TransactionCode,
                payment.PaymentStatus,
                payment.PaidAt,
                IsDuplicateTransaction = !string.IsNullOrWhiteSpace(payment.TransactionCode)
                    && duplicateCodes.Contains(payment.TransactionCode.Trim())
            })
            .ToList();
        var orphanPaidOrders = await _dbContext.Orders.AsNoTracking()
            .Where(order => order.PaidAt >= period.StartUtc && order.PaidAt < period.EndExclusiveUtc
                && order.Invoice == null)
            .Select(order => new { order.OrderId, order.OrderStatus, order.PaidAt, order.TotalAmount })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                fromDate = period.FromDate,
                toDate = period.ToDate,
                cutoffUtc = period.EndExclusiveUtc,
                summary = new
                {
                    invoiceCount = rows.Count,
                    balancedCount = rows.Count(row => row.ReconciliationStatus == "balanced"),
                    underpaidCount = rows.Count(row => row.ReconciliationStatus == "underpaid" || row.ReconciliationStatus == "missing_payment"),
                    overpaidCount = rows.Count(row => row.ReconciliationStatus == "overpaid"),
                    duplicateTransactionCount = duplicateCodes.Count,
                    orderMismatchCount = rows.Count(row => row.HasOrderMismatch),
                    orphanPaidOrderCount = orphanPaidOrders.Count,
                    paymentEventCount = periodPaymentCount,
                    paymentEventsTruncated = periodPaymentCount > paymentEvents.Count,
                    successfulPaymentAmount = periodPayments.Sum(payment => payment.Amount),
                    cashAmount = periodPayments.Where(payment => payment.PaymentMethod == "cash").Sum(payment => payment.Amount),
                    transferAmount = periodPayments.Where(payment => payment.PaymentMethod == "transfer" || payment.PaymentMethod == "bank_transfer" || payment.PaymentMethod == "banktransfer").Sum(payment => payment.Amount)
                },
                rows,
                paymentEvents,
                orphanPaidOrders,
                duplicateTransactionCodes = duplicateCodes.ToList()
            }
        });
    }

    [HttpGet("operations/reservations")]
    public async Task<IActionResult> GetReservationOperationsReport([FromQuery] DateOnly? fromDate = null, [FromQuery] DateOnly? toDate = null)
    {
        if (!TryResolvePeriod(fromDate, toDate, out var period, out var error))
            return BadRequest(new { success = false, message = error });
        var localStart = period.FromDate.ToDateTime(TimeOnly.MinValue);
        var localEnd = period.ToDate.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var nowLocal = DateTime.UtcNow.AddHours(VietnamUtcOffsetHours);
        var reservations = await _dbContext.Reservations.AsNoTracking()
            .Where(reservation => reservation.ReservationDate >= localStart && reservation.ReservationDate < localEnd)
            .Select(reservation => new
            {
                reservation.ReservationId,
                reservation.ReservationDate,
                reservation.GuestCount,
                reservation.ReservationStatus,
                TableCount = reservation.TableId.HasValue ? 1 : 0
            })
            .ToListAsync();
        var completedStatuses = new[] { "checked_in", "completed", "no_show" };
        var eligible = reservations.Where(reservation => reservation.ReservationDate <= nowLocal
            && completedStatuses.Contains(reservation.ReservationStatus)).ToList();
        var daily = reservations.GroupBy(reservation => DateOnly.FromDateTime(reservation.ReservationDate.Date))
            .OrderBy(group => group.Key)
            .Select(group => new
            {
                Date = group.Key,
                BookingCount = group.Count(),
                GuestCount = group.Sum(reservation => reservation.GuestCount),
                CheckedInCount = group.Count(reservation => reservation.ReservationStatus == "checked_in"),
                CompletedCount = group.Count(reservation => reservation.ReservationStatus == "completed"),
                CancelledCount = group.Count(reservation => reservation.ReservationStatus is "cancelled" or "rejected"),
                NoShowCount = group.Count(reservation => reservation.ReservationStatus == "no_show"),
                PendingCount = group.Count(reservation => reservation.ReservationStatus is "pending" or "confirmed")
            }).ToList();
        var noShowDenominator = eligible.Count;
        return Ok(new
        {
            success = true,
            data = new
            {
                fromDate = period.FromDate, toDate = period.ToDate,
                summary = new
                {
                    bookingCount = reservations.Count,
                    guestCount = reservations.Sum(reservation => reservation.GuestCount),
                    checkedInCount = reservations.Count(reservation => reservation.ReservationStatus == "checked_in"),
                    completedCount = reservations.Count(reservation => reservation.ReservationStatus == "completed"),
                    cancelledCount = reservations.Count(reservation => reservation.ReservationStatus is "cancelled" or "rejected"),
                    noShowCount = reservations.Count(reservation => reservation.ReservationStatus == "no_show"),
                    pendingCount = reservations.Count(reservation => reservation.ReservationStatus is "pending" or "confirmed"),
                    noShowRate = noShowDenominator == 0 ? (decimal?)null : Math.Round((decimal)eligible.Count(reservation => reservation.ReservationStatus == "no_show") / noShowDenominator * 100m, 1),
                    noShowDefinition = "no-show / (check-in + hoàn tất + no-show) trên lịch đã đến giờ và có kết quả"
                },
                daily
            }
        });
    }

    [HttpGet("operations/kitchen-time")]
    public async Task<IActionResult> GetKitchenTimeOperationsReport([FromQuery] DateOnly? fromDate = null, [FromQuery] DateOnly? toDate = null)
    {
        if (!TryResolvePeriod(fromDate, toDate, out var period, out var error))
            return BadRequest(new { success = false, message = error });
        var items = await _dbContext.OrderItems.AsNoTracking()
            .Where(item => item.ReadyAt.HasValue && item.ReadyAt >= period.StartUtc && item.ReadyAt < period.EndExclusiveUtc)
            .Select(item => new
            {
                item.FoodNameSnapshot,
                item.VariantSnapshot,
                item.Quantity,
                item.ItemStatus,
                item.ConfirmedAt,
                item.ReadyAt
            })
            .ToListAsync();
        var active = items.Where(item => item.ItemStatus != "cancelled").ToList();
        var rows = active.GroupBy(item => new { item.FoodNameSnapshot, item.VariantSnapshot })
            .Select(group =>
            {
                var valid = group.Where(item => item.ConfirmedAt.HasValue && item.ReadyAt >= item.ConfirmedAt.Value).ToList();
                return new
                {
                    FoodName = group.Key.FoodNameSnapshot,
                    VariantName = group.Key.VariantSnapshot,
                    CompletedLineCount = group.Count(),
                    ServedQuantity = group.Sum(item => item.Quantity),
                    ValidSampleCount = valid.Count,
                    MissingOrInvalidSampleCount = group.Count() - valid.Count,
                    AverageMinutes = valid.Count == 0 ? (decimal?)null : Math.Round((decimal)valid.Average(item => (item.ReadyAt!.Value - item.ConfirmedAt!.Value).TotalMinutes), 1)
                };
            })
            .OrderByDescending(row => row.ServedQuantity)
            .ToList();
        return Ok(new
        {
            success = true,
            data = new
            {
                fromDate = period.FromDate, toDate = period.ToDate,
                summary = new { completedLineCount = active.Count, servedQuantity = active.Sum(item => item.Quantity), validSampleCount = active.Count(item => item.ConfirmedAt.HasValue && item.ReadyAt >= item.ConfirmedAt.Value), missingSampleCount = active.Count(item => !item.ConfirmedAt.HasValue || item.ReadyAt < item.ConfirmedAt!.Value) },
                rows,
                definition = "Thời gian từ ConfirmedAt đến ReadyAt; không phải toàn bộ thời gian nấu hoặc phục vụ khách."
            }
        });
    }

    [HttpGet("operations/promotions")]
    public async Task<IActionResult> GetPromotionOperationsReport([FromQuery] DateOnly? fromDate = null, [FromQuery] DateOnly? toDate = null)
    {
        if (!TryResolvePeriod(fromDate, toDate, out var period, out var error))
            return BadRequest(new { success = false, message = error });
        var usages = await _dbContext.PromotionUsages.AsNoTracking()
            .Where(usage => usage.Invoice.InvoiceStatus == "paid"
                && usage.Invoice.IssuedAt >= period.StartUtc && usage.Invoice.IssuedAt < period.EndExclusiveUtc)
            .Select(usage => new
            {
                usage.PromotionId,
                usage.InvoiceId,
                usage.DiscountAmount,
                InvoiceRevenue = usage.Invoice.TotalAmount,
                usage.UsedAt,
                PromotionCode = usage.Promotion.PromotionCode,
                PromotionName = usage.Promotion.PromotionName
            })
            .ToListAsync();
        var rows = usages.GroupBy(usage => new { usage.PromotionId, usage.PromotionCode, usage.PromotionName })
            .Select(group => new
            {
                group.Key.PromotionId,
                group.Key.PromotionCode,
                group.Key.PromotionName,
                InvoiceCount = group.Select(usage => usage.InvoiceId).Distinct().Count(),
                DiscountAmount = group.Sum(usage => usage.DiscountAmount),
                RevenueAfterDiscount = group.Where(usage => usage.InvoiceRevenue >= 0).GroupBy(usage => usage.InvoiceId).Sum(invoice => invoice.First().InvoiceRevenue),
                DuplicateUsageCount = group.GroupBy(usage => usage.InvoiceId).Count(invoice => invoice.Count() > 1)
            })
            .OrderByDescending(row => row.DiscountAmount).ToList();
        return Ok(new
        {
            success = true,
            data = new
            {
                fromDate = period.FromDate, toDate = period.ToDate,
                summary = new { voucherCount = rows.Count, invoiceCount = usages.Select(usage => usage.InvoiceId).Distinct().Count(), discountAmount = rows.Sum(row => row.DiscountAmount), duplicateUsageCount = rows.Sum(row => row.DuplicateUsageCount) },
                rows,
                definition = "Doanh thu gắn voucher; không phải doanh thu tăng thêm hay ROI chiến dịch."
            }
        });
    }

    [HttpGet("operations/customers")]
    public async Task<IActionResult> GetCustomerOperationsReport([FromQuery] DateOnly? fromDate = null, [FromQuery] DateOnly? toDate = null)
    {
        if (!TryResolvePeriod(fromDate, toDate, out var period, out var error))
            return BadRequest(new { success = false, message = error });
        var inPeriod = await _dbContext.Invoices.AsNoTracking()
            .Where(invoice => invoice.InvoiceStatus == "paid" && invoice.IssuedAt >= period.StartUtc && invoice.IssuedAt < period.EndExclusiveUtc)
            .Select(invoice => new { invoice.InvoiceId, invoice.CustomerId, invoice.IssuedAt, invoice.TotalAmount, CustomerName = invoice.Customer == null ? null : invoice.Customer.FullName })
            .ToListAsync();
        var customerIds = inPeriod.Where(invoice => invoice.CustomerId.HasValue).Select(invoice => invoice.CustomerId!.Value).Distinct().ToList();
        var priorCustomerIds = await _dbContext.Invoices.AsNoTracking()
            .Where(invoice => invoice.InvoiceStatus == "paid" && invoice.CustomerId.HasValue && customerIds.Contains(invoice.CustomerId.Value) && invoice.IssuedAt < period.StartUtc)
            .Select(invoice => invoice.CustomerId!.Value)
            .Distinct().ToListAsync();
        var priorSet = priorCustomerIds.ToHashSet();
        var customerRows = inPeriod.Where(invoice => invoice.CustomerId.HasValue)
            .GroupBy(invoice => new { CustomerId = invoice.CustomerId!.Value, invoice.CustomerName })
            .Select(group => new
            {
                group.Key.CustomerId,
                CustomerName = group.Key.CustomerName ?? "Khách hàng đã ẩn",
                InvoiceCount = group.Count(),
                Revenue = group.Sum(invoice => invoice.TotalAmount),
                LastPurchaseAt = group.Max(invoice => invoice.IssuedAt),
                Segment = priorSet.Contains(group.Key.CustomerId) ? "returning" : "new_buyer"
            })
            .OrderByDescending(row => row.Revenue).ToList();
        var points = customerIds.Count == 0 ? new List<dynamic>() : (await _dbContext.PointTransactions.AsNoTracking()
            .Where(transaction => customerIds.Contains(transaction.CustomerId) && transaction.CreatedAt >= period.StartUtc && transaction.CreatedAt < period.EndExclusiveUtc)
            .Select(transaction => new { transaction.Points, transaction.TransactionType })
            .ToListAsync()).Cast<dynamic>().ToList();
        return Ok(new
        {
            success = true,
            data = new
            {
                fromDate = period.FromDate, toDate = period.ToDate,
                summary = new
                {
                    buyerCount = customerRows.Count,
                    newBuyerCount = customerRows.Count(row => row.Segment == "new_buyer"),
                    returningBuyerCount = customerRows.Count(row => row.Segment == "returning"),
                    anonymousInvoiceCount = inPeriod.Count(invoice => !invoice.CustomerId.HasValue),
                    revenue = inPeriod.Sum(invoice => invoice.TotalAmount),
                    pointsEarned = points.Where(point => ReportCalculations.IsEarnedPointType((string)point.TransactionType))
                        .Sum(point => Math.Max((int)point.Points, 0)),
                    pointsUsed = points.Where(point => ReportCalculations.IsRedeemedPointType((string)point.TransactionType))
                        .Sum(point => Math.Abs((int)point.Points))
                },
                rows = customerRows,
                definition = "Khách mới là người có hóa đơn paid đầu tiên trong lịch sử nằm trong kỳ; khách quay lại có hóa đơn paid trước kỳ và trong kỳ."
            }
        });
    }

    private static bool IsReturnMovement(string? reason)
        => !string.IsNullOrWhiteSpace(reason) && reason.Contains("Hoan bep", StringComparison.OrdinalIgnoreCase);

    private static string MovementLabel(string movementType, string? reason)
        => movementType switch
        {
            "import" when IsReturnMovement(reason) => "Hoàn từ bếp",
            "import" => "Nhập thực nhận",
            "export" => "Xuất bếp",
            "waste" => "Hủy tại kho/bếp",
            "adjustment" when ReportCalculations.IsPositiveAdjustment(movementType, reason) => "Điều chỉnh tăng",
            "adjustment" => "Điều chỉnh giảm",
            _ => movementType
        };

    private static decimal SnapshotPortions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return 0m;
        try
        {
            var snapshot = JsonSerializer.Deserialize<List<StockReceiptBomSnapshotEntry>>(json);
            return snapshot?.Sum(entry => entry.Portions) ?? 0m;
        }
        catch (JsonException)
        {
            return 0m;
        }
    }

    private static decimal ProductNetRevenue(IEnumerable<ProductSaleLine> lines)
    {
        return lines.Sum(item => item.LineTotal - (item.InvoiceSubtotal <= 0
            ? 0m
            : item.InvoiceDiscount * item.LineTotal / item.InvoiceSubtotal));
    }

    private static decimal? PercentageChange(decimal current, decimal previous)
    {
        return previous == 0m ? null : Math.Round((current - previous) / previous * 100m, 1);
    }

    private static bool TryResolvePeriod(DateOnly? fromDate, DateOnly? toDate, out ReportPeriod period, out string error)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(VietnamUtcOffsetHours));
        var startDate = fromDate ?? new DateOnly(today.Year, today.Month, 1);
        var endDate = toDate ?? today;
        if (startDate > endDate)
        {
            period = null!;
            error = "Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.";
            return false;
        }

        var start = startDate.ToDateTime(TimeOnly.MinValue).AddHours(-VietnamUtcOffsetHours);
        var endExclusive = endDate.AddDays(1).ToDateTime(TimeOnly.MinValue).AddHours(-VietnamUtcOffsetHours);
        if ((endExclusive - start).TotalDays > 366)
        {
            period = null!;
            error = "Khoảng thời gian báo cáo tối đa là 366 ngày.";
            return false;
        }

        period = new ReportPeriod(startDate, endDate, start, endExclusive);
        error = string.Empty;
        return true;
    }

}
