namespace StarCitiSync.Client.Models
{
    public class MissionEvent
    {
        public int Id { get; set; }
        public string SessionId { get; set; }
        public string MissionId { get; set; }
        public string EventType { get; set; }
        public string GeneratorName { get; set; }
        public string Contract { get; set; }
        public string CompletionType { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
  }
}