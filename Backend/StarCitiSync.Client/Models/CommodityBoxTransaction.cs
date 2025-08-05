public class CommodityBoxTransaction
{
    public int Id { get; set; } 
    public string SessionId { get; set; } 
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; }
    public string PlayerId { get; set; }
    public string ShopId { get; set; }
    public string ShopName { get; set; }
    public string KioskId { get; set; }
    public decimal? Price { get; set; }
    public decimal? ShopPricePerCentiSCU { get; set; }
    public string ResourceGuid { get; set; }
    public int? AutoLoading { get; set; }
    public decimal? Quantity { get; set; }
    public string QuantityUnit { get; set; }
    public string CommodityName { get; set; }
    public int? BoxSize { get; set; }
    public int? UnitAmount { get; set; }
}