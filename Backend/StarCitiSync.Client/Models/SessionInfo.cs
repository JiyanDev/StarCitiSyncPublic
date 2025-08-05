namespace StarCitiSync.Client.Models
{
  public class SessionInfo
  {
    public string Id { get; set; }
    public string SessionId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Geid { get; set; }
    public string Username { get; set; }
    public string CreatedAt { get; set; }
    public string UpdatedAt { get; set; }
    public string AccountId { get; set; }
    public DateTime? AppClose { get; set; }
  }
}