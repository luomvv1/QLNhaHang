namespace Restaurant.Api.Entities;

/// <summary>
/// Immutable recipe context captured when a kitchen issue receipt is created.
/// It prevents later BOM edits from changing reconciliation history.
/// </summary>
public sealed class StockReceiptBomSnapshotEntry
{
    public int FoodId { get; set; }
    public int? VariantId { get; set; }
    public string FoodName { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public decimal Portions { get; set; }
    public decimal QuantityPerUnit { get; set; }
    public decimal WastagePercent { get; set; }
}
