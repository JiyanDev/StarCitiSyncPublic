namespace StarCitiSync.Client.Models
{
    public class ShopTransaction
    {
        public int Id { get; set; } // Autoincrement primary key
        public string SessionId { get; set; }
        public DateTime Timestamp { get; set; }
        public string ShopName { get; set; }
        public string ShopId { get; set; }
        public string KioskId { get; set; }
        private decimal _clientPrice;
        public decimal ClientPrice
        {
          get => _clientPrice;
          set => _clientPrice = Math.Truncate(value * 100) / 100;
        }
        public string ItemClassGuid { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public string CurrencyType { get; set; }
        public string Result { get; set; }
        public string TransactionType { get; set; }
  }
}