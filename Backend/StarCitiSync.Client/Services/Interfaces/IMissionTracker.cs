namespace StarCitiSync.Client.Services.Interfaces
{
    public interface IMissionTracker : ILogLineProcessor
    {
        bool HasNewEvent { get; }
        string GetLastEventDescription();
    }
}