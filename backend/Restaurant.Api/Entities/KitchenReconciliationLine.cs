namespace Restaurant.Api.Entities;

public class KitchenReconciliationLine
{
    public int ReconciliationLineId { get; set; }
    public int SourceDetailId { get; set; }
    public decimal IssuedQuantity { get; set; }
    public decimal UsedQuantity { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public decimal WastedQuantity { get; set; }
    public string? WasteReason { get; set; }
    public int? ReturnReceiptId { get; set; }
    public int? WasteReceiptId { get; set; }
    public int ReconciledBy { get; set; }
    public DateTime ReconciledAt { get; set; }

    public StockReceiptDetail SourceDetail { get; set; } = null!;
    public StockReceipt? ReturnReceipt { get; set; }
    public StockReceipt? WasteReceipt { get; set; }
}
