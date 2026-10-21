namespace Restaurant.Api.Controllers.Admin;

/// <summary>
/// Inventory quantities are stored in BaseUnit, while purchase costs are stored
/// per PurchaseUnit. This keeps the conversion rule in one place for valuation.
/// </summary>
internal static class InventoryCostHelper
{
    public static decimal ToBaseUnitCost(decimal purchaseUnitCost, decimal conversionRate)
    {
        return conversionRate > 0 ? purchaseUnitCost / conversionRate : purchaseUnitCost;
    }

    public static decimal CalculateAmount(decimal baseQuantity, decimal purchaseUnitCost, decimal conversionRate)
    {
        return Math.Round(baseQuantity * ToBaseUnitCost(purchaseUnitCost, conversionRate), 2);
    }
}
