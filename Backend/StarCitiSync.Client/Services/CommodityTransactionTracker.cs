using StarCitiSync.Client.Models;
using StarCitiSync.Client.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;

namespace StarCitiSync.Client.Services
{
  public class CommodityTransactionTracker : ILogLineProcessor
  {
    private readonly List<CommodityBoxTransaction> _transactions = new();
    public IReadOnlyList<CommodityBoxTransaction> Transactions => _transactions;
    public void ProcessLogLine(object logLine)
    {
      if (logLine == null) return;
      string line = logLine.ToString();

      if (line.Contains("SendCommodityBuyRequest"))
      {
        var timeMatch = Regex.Match(line, @"^<([^>]+)>");
        var playerIdMatch = Regex.Match(line, @"playerId\[([^\]]+)\]");
        var shopNameMatch = Regex.Match(line, @"shopName\[([^\]]+)\]");
        var shopIdMatch = Regex.Match(line, @"shopId\[([^\]]+)\]");
        var kioskIdMatch = Regex.Match(line, @"kioskId\[([^\]]+)\]");
        var priceMatch = Regex.Match(line, @"price\[([0-9\.]+)\]");
        var shopPricePerCentiSCUMatch = Regex.Match(line, @"shopPricePerCentiSCU\[([0-9\.]+)\]");
        var resourceGuidMatch = Regex.Match(line, @"resourceGUID\[([^\]]+)\]");
        var autoLoadingMatch = Regex.Match(line, @"autoLoading\[([0-9]+)\]");
        var quantityMatch = Regex.Match(line, @"quantity\[([0-9\.]+)\s*([a-zA-Z]+)?\]");
        var commodityNameMatch = Regex.Match(line, @"commodityName\[([^\]]*)\]");
        var cargoBoxDataMatch = Regex.Match(line, @"Cargo Box Data:\s*(.*?)(?:\s*\[Team_|$)");
        DateTime.TryParse(timeMatch.Groups[1].Value, out var timestamp);

        bool anyBox = false;
        if (cargoBoxDataMatch.Success)
        {
          var boxPairs = Regex.Matches(cargoBoxDataMatch.Groups[1].Value, @"boxSize\[([0-9\.]+)\]\s*\|\s*unitAmount\[([0-9]+)\]");
          foreach (Match m in boxPairs)
          {
            anyBox = true;
            _transactions.Add(new CommodityBoxTransaction
            {
              Timestamp = timestamp,
              EventType = "SendCommodityBuyRequest",
              PlayerId = playerIdMatch.Success ? playerIdMatch.Groups[1].Value : null,
              ShopId = shopIdMatch.Success ? shopIdMatch.Groups[1].Value : null,
              ShopName = shopNameMatch.Success ? shopNameMatch.Groups[1].Value : null,
              KioskId = kioskIdMatch.Success ? kioskIdMatch.Groups[1].Value : null,
              Price = priceMatch.Success ? decimal.Parse(priceMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : null,
              ShopPricePerCentiSCU = shopPricePerCentiSCUMatch.Success ? decimal.Parse(shopPricePerCentiSCUMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : null,
              ResourceGuid = resourceGuidMatch.Success ? resourceGuidMatch.Groups[1].Value : null,
              AutoLoading = autoLoadingMatch.Success ? int.Parse(autoLoadingMatch.Groups[1].Value) : null,
              Quantity = quantityMatch.Success ? decimal.Parse(quantityMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : null,
              QuantityUnit = quantityMatch.Success ? quantityMatch.Groups[2].Value : null,
              CommodityName = commodityNameMatch.Success ? commodityNameMatch.Groups[1].Value : null,
              BoxSize = int.TryParse(m.Groups[1].Value.Split('.')[0], out int boxSize) ? boxSize : null,
              UnitAmount = int.TryParse(m.Groups[2].Value, out int unitAmount) ? unitAmount : null
            });
          }
        }
        if (!anyBox)
        {
          _transactions.Add(new CommodityBoxTransaction
          {
            Timestamp = timestamp,
            EventType = "SendCommodityBuyRequest",
            PlayerId = playerIdMatch.Success ? playerIdMatch.Groups[1].Value : null,
            ShopId = shopIdMatch.Success ? shopIdMatch.Groups[1].Value : null,
            ShopName = shopNameMatch.Success ? shopNameMatch.Groups[1].Value : null,
            KioskId = kioskIdMatch.Success ? kioskIdMatch.Groups[1].Value : null,
            Price = priceMatch.Success ? decimal.Parse(priceMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : null,
            ShopPricePerCentiSCU = shopPricePerCentiSCUMatch.Success ? decimal.Parse(shopPricePerCentiSCUMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : null,
            ResourceGuid = resourceGuidMatch.Success ? resourceGuidMatch.Groups[1].Value : null,
            AutoLoading = autoLoadingMatch.Success ? int.Parse(autoLoadingMatch.Groups[1].Value) : null,
            Quantity = quantityMatch.Success ? decimal.Parse(quantityMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : null,
            QuantityUnit = quantityMatch.Success ? quantityMatch.Groups[2].Value : null,
            CommodityName = commodityNameMatch.Success ? commodityNameMatch.Groups[1].Value : null,
            BoxSize = null,
            UnitAmount = null
          });
        }
      }
      else if (line.Contains("SendCommoditySellRequest"))
      {
        var timeMatch = Regex.Match(line, @"^<([^>]+)>");
        var playerIdMatch = Regex.Match(line, @"playerId\[([^\]]+)\]");
        var shopNameMatch = Regex.Match(line, @"shopName\[([^\]]+)\]");
        var shopIdMatch = Regex.Match(line, @"shopId\[([^\]]+)\]");
        var kioskIdMatch = Regex.Match(line, @"kioskId\[([^\]]+)\]");
        var amountMatch = Regex.Match(line, @"amount\[([0-9\.]+)\]");
        var resourceGuidMatch = Regex.Match(line, @"resourceGUID\[([^\]]+)\]");
        var autoLoadingMatch = Regex.Match(line, @"autoLoading\[([0-9]+)\]");
        var quantityMatch = Regex.Match(line, @"quantity\[([0-9\.]+)\]");
        var commodityNameMatch = Regex.Match(line, @"commodityName\[([^\]]*)\]");
        var cargoBoxDataMatch = Regex.Match(line, @"Cargo Box Data:\s*([^\[]+)");
        DateTime.TryParse(timeMatch.Groups[1].Value, out var timestamp);

        bool anyBox = false;
        if (cargoBoxDataMatch.Success)
        {
          var boxPairs = Regex.Matches(cargoBoxDataMatch.Groups[1].Value, @"boxSize\[([0-9\.]+)\]\s*\|\s*unitAmount\[([0-9]+)\]");
          foreach (Match m in boxPairs)
          {
            anyBox = true;
            _transactions.Add(new CommodityBoxTransaction
            {
              Timestamp = timestamp,
              EventType = "SendCommoditySellRequest",
              PlayerId = playerIdMatch.Success ? playerIdMatch.Groups[1].Value : null,
              ShopId = shopIdMatch.Success ? shopIdMatch.Groups[1].Value : null,
              ShopName = shopNameMatch.Success ? shopNameMatch.Groups[1].Value : null,
              KioskId = kioskIdMatch.Success ? kioskIdMatch.Groups[1].Value : null,
              Price = amountMatch.Success ? decimal.Parse(amountMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : null,
              ShopPricePerCentiSCU = null,
              ResourceGuid = resourceGuidMatch.Success ? resourceGuidMatch.Groups[1].Value : null,
              AutoLoading = autoLoadingMatch.Success ? int.Parse(autoLoadingMatch.Groups[1].Value) : null,
              Quantity = quantityMatch.Success ? decimal.Parse(quantityMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : null,
              QuantityUnit = null,
              CommodityName = commodityNameMatch.Success ? commodityNameMatch.Groups[1].Value : null,
              BoxSize = int.TryParse(m.Groups[1].Value.Split('.')[0], out int boxSize) ? boxSize : null,
              UnitAmount = int.TryParse(m.Groups[2].Value, out int unitAmount) ? unitAmount : null
            });
          }
        }
        if (!anyBox)
        {
          _transactions.Add(new CommodityBoxTransaction
          {
            Timestamp = timestamp,
            EventType = "SendCommoditySellRequest",
            PlayerId = playerIdMatch.Success ? playerIdMatch.Groups[1].Value : null,
            ShopId = shopIdMatch.Success ? shopIdMatch.Groups[1].Value : null,
            ShopName = shopNameMatch.Success ? shopNameMatch.Groups[1].Value : null,
            KioskId = kioskIdMatch.Success ? kioskIdMatch.Groups[1].Value : null,
            Price = amountMatch.Success ? decimal.Parse(amountMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : null,
            ShopPricePerCentiSCU = null,
            ResourceGuid = resourceGuidMatch.Success ? resourceGuidMatch.Groups[1].Value : null,
            AutoLoading = autoLoadingMatch.Success ? int.Parse(autoLoadingMatch.Groups[1].Value) : null,
            Quantity = quantityMatch.Success ? decimal.Parse(quantityMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : null,
            QuantityUnit = null,
            CommodityName = commodityNameMatch.Success ? commodityNameMatch.Groups[1].Value : null,
            BoxSize = null,
            UnitAmount = null
          });
        }
      }
    }
  }
}
