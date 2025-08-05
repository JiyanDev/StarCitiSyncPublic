using StarCitiSync.Client.Models;
using System.Collections.Generic;

namespace StarCitiSync.Client.Services.Interfaces
{
  public interface IShopTransactionTracker
  {
    IReadOnlyList<ShopTransaction> Transactions { get; }
  }
}