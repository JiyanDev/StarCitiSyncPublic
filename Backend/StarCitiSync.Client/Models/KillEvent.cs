namespace StarCitiSync.Client.Models
{
  public class KillEvent
  {
    public int Id { get; set; }
    public string SessionId { get; set; }
    public DateTime? Timestamp { get; set; }
    public string EntityName { get; set; }
    public string EntityId { get; set; }
    public string Killer { get; set; }
    public string Weapon { get; set; }
    public string DamageType { get; set; }
    public int? DestroyLevel { get; set; }
    public string EntityType { get; set; }
  }
}