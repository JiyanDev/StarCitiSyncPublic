using StarCitiSync.Client.Models;
using StarCitiSync.Client.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StarCitiSync.Client.Services
{
  public class ShopTransactionTracker : IShopTransactionTracker, ILogLineProcessor
  {
    // Transactions awaiting their ShopFlowResponse result
    private readonly List<ShopTransaction> _pendingTransactions = new();
    private const int MaxPendingTransactions = 50;

    public ShopTransaction? NewTransaction { get; private set; }
    public ShopTransaction? CompletedTransaction { get; private set; }

    public void ProcessLogLine(object logLine)
    {
      NewTransaction = null;
      CompletedTransaction = null;

      if (logLine == null) return;
      string line = logLine.ToString();

      // Match buy request
      if (line.Contains("SendShopBuyRequest") || line.Contains("SendStandardItemBuyRequest"))
      {
        var timeMatch = Regex.Match(line, @"^<([^>]+)>");
        var shopNameMatch = Regex.Match(line, @"shopName\[([^\]]+)\]");
        var shopIdMatch = Regex.Match(line, @"shopId\[([^\]]+)\]");
        var kioskIdMatch = Regex.Match(line, @"kioskId\[([^\]]+)\]");
        var clientPriceMatch = Regex.Match(line, @"client_price\[([0-9\.]+)\]");
        var itemClassGuidMatch = Regex.Match(line, @"itemClassGUID\[([^\]]+)\]");
        var itemNameMatch = Regex.Match(line, @"itemName\[([^\]]+)\]");
        var quantityMatch = Regex.Match(line, @"quantity\[([0-9]+)\]");
        var currencyTypeMatch = Regex.Match(line, @"currencyType\[([^\]]+)\]");

        DateTime.TryParse(timeMatch.Groups[1].Value, out var timestamp);

        var transaction = new ShopTransaction
        {
          Timestamp = timestamp,
          ShopName = shopNameMatch.Success ? shopNameMatch.Groups[1].Value : null,
          ShopId = shopIdMatch.Success ? shopIdMatch.Groups[1].Value : null,
          KioskId = kioskIdMatch.Success ? kioskIdMatch.Groups[1].Value : null,
          ClientPrice = clientPriceMatch.Success ? decimal.Parse(clientPriceMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0,
          ItemClassGuid = itemClassGuidMatch.Success ? itemClassGuidMatch.Groups[1].Value : null,
          ItemName = itemNameMatch.Success ? itemNameMatch.Groups[1].Value : null,
          Quantity = quantityMatch.Success ? int.Parse(quantityMatch.Groups[1].Value) : 0,
          CurrencyType = currencyTypeMatch.Success ? currencyTypeMatch.Groups[1].Value : "UEC",
          Result = "Pending",
          TransactionType = "Buy"
        };

        _pendingTransactions.Add(transaction);
        if (_pendingTransactions.Count > MaxPendingTransactions)
          _pendingTransactions.RemoveAt(0);

        NewTransaction = transaction;
      }
      // Match confirmation
      else if (line.Contains("ShopFlowResponse"))
      {
        var shopIdMatch = Regex.Match(line, @"shopId\[([^\]]+)\]");
        var resultMatch = Regex.Match(line, @"result\[([^\]]+)\]");

        var shopId = shopIdMatch.Success ? shopIdMatch.Groups[1].Value : null;
        var result = resultMatch.Success ? resultMatch.Groups[1].Value : null;

        // Set result on latest pending with same shopId
        var pending = _pendingTransactions.LastOrDefault(t => t.ShopId == shopId && t.Result == "Pending");
        if (pending != null && result != null)
        {
          pending.Result = result;
          _pendingTransactions.Remove(pending);
          CompletedTransaction = pending;
        }
      }
    }
  }
}
