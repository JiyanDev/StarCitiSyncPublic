using StarCitiSync.Client.Data;
using StarCitiSync.Client.Models;
using System.Text.RegularExpressions;

namespace StarCitiSync.Client.Services
{
  public class SessionManager
  {
    private readonly SessionRepository _sessionRepo;
    private readonly SessionInfoReader _sessionInfoReader;

    public SessionManager(SessionRepository sessionRepo, SessionInfoReader sessionInfoReader)
    {
        _sessionRepo = sessionRepo;
        _sessionInfoReader = sessionInfoReader;
    }

    static DateTime TrimMilliseconds(DateTime dt)
    {
      return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Kind);
    }

    public async Task<SessionInfo> EnsureSessionAsync(string logFilePath)
    {
      long initialLength = 0;
      if (File.Exists(logFilePath))
          initialLength = new FileInfo(logFilePath).Length;

      string? firstLine = null;
      while (firstLine == null)
      {
          if (File.Exists(logFilePath))
          {
              using (var stream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
              using (var reader = new StreamReader(stream))
              {
                  if (initialLength > 0)
                      stream.Seek(initialLength, SeekOrigin.Begin);
                  firstLine = await reader.ReadLineAsync();
                  if (string.IsNullOrWhiteSpace(firstLine))
                      firstLine = null;
              }
          }
          if (firstLine == null)
              await Task.Delay(500);
      }

      // Extrahera timestamp
      string? firstTimestamp = null;
      var match = Regex.Match(firstLine, @"^<([^>]+)>");
      if (match.Success)
          firstTimestamp = match.Groups[1].Value;

      DateTime? startDate = null;
      if (DateTime.TryParse(firstTimestamp, out var parsed))
          startDate = parsed;

      SessionInfo? sessionInfo = null;
      if (startDate != null)
      {
          sessionInfo = _sessionRepo.GetByStartDate(startDate.Value);
      }

      if (sessionInfo == null)
      {
        const int maxRetries = 10;
        const int delayMs = 2000;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
          const int maxSessionInfoRetries = 10;
          const int sessionInfoRetryDelayMs = 10000;
          int sessionInfoRetryCount = 0;

          do
          {
            sessionInfo = _sessionInfoReader.ReadSessionInfo(logFilePath);
            if (sessionInfo?.StartDate != null && !string.IsNullOrEmpty(sessionInfo.SessionId))
              break;

            await Task.Delay(sessionInfoRetryDelayMs);
            sessionInfoRetryCount++;
          } while (string.IsNullOrEmpty(sessionInfo?.SessionId));

          if (sessionInfo == null || sessionInfo.StartDate == null || string.IsNullOrEmpty(sessionInfo.SessionId))
            throw new InvalidOperationException("Could not read valid session info from log file after multiple attempts.");
          
          if (sessionInfo?.StartDate != null)
          {
            var existingSession = _sessionRepo.GetLatestSession();
            if (existingSession != null && existingSession.EndDate == null && existingSession.SessionId != null && existingSession.StartDate != null)
            {
              // Compare StartDates
              if (TrimMilliseconds(sessionInfo.StartDate.Value) > TrimMilliseconds(existingSession.StartDate.Value))
              {
                // sessionInfo is newer, use it (insert if needed)
                if(existingSession.EndDate == null)
                {
                  DateTime? endDate = sessionInfo.StartDate;
                  if (existingSession.AppClose != null)
                    endDate = existingSession.AppClose;

                  existingSession.EndDate = endDate;
                  _sessionRepo.Update(existingSession);

                  if (existingSession.SessionId == null)
                    _sessionRepo.DeleteById(existingSession.Id);
                }
                _sessionRepo.Insert(sessionInfo);
                break;
              }
              else if (TrimMilliseconds(sessionInfo.StartDate.Value) == TrimMilliseconds(existingSession.StartDate.Value))
              {
                // StartDates are the same, reuse existing
                sessionInfo = existingSession;
                Console.WriteLine("Session already exists in DB, reusing.");
                break;
              }
              else
              {
                // sessionInfo is older, reuse existing
                sessionInfo = existingSession;
                Console.WriteLine("Session already exists in DB, reusing.");
                break;
              }
            }

            if (existingSession != null && existingSession.SessionId == null)
              _sessionRepo.DeleteById(existingSession.Id);

            _sessionRepo.Insert(sessionInfo);
            break;
          }
          await Task.Delay(delayMs);
        }
      }
      else
      {
        Console.WriteLine("Session already exists in DB, reusing.");
      }

      while (string.IsNullOrEmpty(sessionInfo.SessionId))
      {
          sessionInfo = _sessionInfoReader.ReadSessionInfo(logFilePath);
      }

      if (string.IsNullOrEmpty(sessionInfo.SessionId))
          _sessionRepo.Insert(sessionInfo);

      Console.WriteLine($"Log Start Date: {sessionInfo.StartDate}");
      Console.WriteLine($"Session ID: {sessionInfo.SessionId}");
      Console.WriteLine($"Username: {sessionInfo.Username}");

      return sessionInfo;
    }
  }
}