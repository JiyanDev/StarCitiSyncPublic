using StarCitiSync.Client.Models;
using StarCitiSync.Client.Services.Interfaces;
using System.Text.RegularExpressions;

namespace StarCitiSync.Client.Services
{
  public class SessionInfoReader : ISessionInfoReader
  {
    public SessionInfo ReadSessionInfo(string logFilePath)
    {
      if (!File.Exists(logFilePath))
        throw new FileNotFoundException($"Log file not found: {logFilePath}");

      var sessionInfo = new SessionInfo();

      var logStartedRegex = new Regex(@"^<([^>]+)>\s+Log started on", RegexOptions.IgnoreCase);
      var characterRegex = new Regex(@"Character: createdAt (\d+) - updatedAt (\d+) - geid (\d+) - accountId (\d+) - name ([^\s]+)", RegexOptions.IgnoreCase);

      using var stream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      using var reader = new StreamReader(stream);

      string? line;
      while ((line = reader.ReadLine()) != null)
      {
        if (sessionInfo.StartDate == null)
        {
          var logStartedMatch = logStartedRegex.Match(line);
          if (logStartedMatch.Success)
          {
            var dateString = logStartedMatch.Groups[1].Value;
            if (DateTime.TryParse(dateString, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var parsedDate))
            {
              var swedishTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
              sessionInfo.StartDate = TimeZoneInfo.ConvertTimeFromUtc(parsedDate, swedishTimeZone);
            }
          }
        }

        if (sessionInfo.CreatedAt == null && sessionInfo.SessionId == null)
        {
          var charMatch = characterRegex.Match(line);
          if (charMatch.Success)
          {
            sessionInfo.SessionId = Guid.NewGuid().ToString();
            sessionInfo.CreatedAt = charMatch.Groups[1].Value;
            sessionInfo.UpdatedAt = charMatch.Groups[2].Value;
            sessionInfo.Geid = charMatch.Groups[3].Value;
            sessionInfo.AccountId = charMatch.Groups[4].Value;
            sessionInfo.Username = charMatch.Groups[5].Value;
          }
        }

        if (sessionInfo.StartDate != null && sessionInfo.SessionId != null)
          break;
      }

      if (sessionInfo.StartDate == null && sessionInfo.Username != null)
      {
        sessionInfo.StartDate = DateTime.Now;
      }
      if (sessionInfo.SessionId == null && sessionInfo.Username != null)
      {
        sessionInfo.SessionId = Guid.NewGuid().ToString();
      }

      return sessionInfo;
    }

    public static DateTime? TryExtractSessionEndDate(string line)
    {
      var systemQuitRegex = new Regex(@"^<([^>]+)>.*<SystemQuit>", RegexOptions.IgnoreCase);
      var fastShutdownRegex = new Regex(@"^<([^>]+)>.*CCIGBroker::FastShutdown", RegexOptions.IgnoreCase);

      string? endDateString = null;
      var systemQuitMatch = systemQuitRegex.Match(line);
      if (systemQuitMatch.Success)
        endDateString = systemQuitMatch.Groups[1].Value;
      else
      {
        var fastShutdownMatch = fastShutdownRegex.Match(line);
        if (fastShutdownMatch.Success)
          endDateString = fastShutdownMatch.Groups[1].Value;
      }

      if (endDateString != null)
      {
        if (DateTime.TryParse(endDateString, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var parsedDate))
        {
          var swedishTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
          return TimeZoneInfo.ConvertTimeFromUtc(parsedDate, swedishTimeZone);
        }
      }
      return null;
    }
  }
}