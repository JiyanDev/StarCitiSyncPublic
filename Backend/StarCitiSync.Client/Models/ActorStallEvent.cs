namespace StarCitiSync.Client.Models
{
    public class ActorStallEvent
    {
        public int Id { get; set; }
        public string Player { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public double Length { get; set; }
        public DateTime Timestamp { get; set; }
        public string SessionId { get; set; } = string.Empty;
  }
}