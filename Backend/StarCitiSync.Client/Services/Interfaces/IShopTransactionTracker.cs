using StarCitiSync.Client.Models;

namespace StarCitiSync.Client.Services.Interfaces
{
  public interface IShopTransactionTracker
  {
    ShopTransaction? NewTransaction { get; }
    ShopTransaction? CompletedTransaction { get; }
  }
}
