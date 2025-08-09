using StarCitiSync.Client.Models;
using StarCitiSync.Client.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace StarCitiSync.Client.Services
{
  public class MissionTracker : IMissionTracker, ILogLineProcessor
  {
    public bool hasNewEvent;
    public string lastEventDescription;
    public FileLogReader logReader;

    // Max antal events att minnas för dubblettskydd
    private readonly int _maxSeenEvents = 20;
    private readonly Queue<string> _seenOrder = new();
    private readonly HashSet<string> _seenEvents = new();

    public MissionTracker(FileLogReader logReader)
    {
      this.logReader = logReader ?? throw new ArgumentNullException(nameof(logReader));
    }

    public MissionEvent? LastMissionEvent { get; private set; }

    public bool HasNewEvent => hasNewEvent;

    public string GetLastEventDescription() => lastEventDescription;

    private void AddSeenEvent(string eventKey)
    {
      if (_seenEvents.Count >= _maxSeenEvents)
      {
        var oldest = _seenOrder.Dequeue();
        _seenEvents.Remove(oldest);
      }
      _seenEvents.Add(eventKey);
      _seenOrder.Enqueue(eventKey);
    }

    public void ProcessLogLine(object logLine)
    {
      if (logLine == null)
        throw new ArgumentNullException(nameof(logLine));

      string? line = logLine.ToString();

      hasNewEvent = false;
      LastMissionEvent = null;

      if (!string.IsNullOrEmpty(line) &&
         (line.Contains("CreateMissionObjectiveMarker") || line.Contains("<CLocalMissionPhaseMarker::CreateMarker>")))
      {
        // Extract timestamp
        var timeMatch = Regex.Match(line, @"<([0-9\-:T\.Z]+)>");
        var missionIdMatch = Regex.Match(line, @"missionId\s*\[([^\]]+)\]");
        var generatorNameMatch = Regex.Match(line, @"generator name\s*\[([^\]]+)\]");
        var contractMatch = Regex.Match(line, @"contract\s*\[([^\]]+)\]");

        DateTime? timestamp = null;
        if (timeMatch.Success && DateTime.TryParse(timeMatch.Groups[1].Value, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var parsedTime))
          timestamp = parsedTime.ToLocalTime();

        string missionId = missionIdMatch.Success ? missionIdMatch.Groups[1].Value : "N/A";
        string generatorName = generatorNameMatch.Success ? generatorNameMatch.Groups[1].Value : "N/A";
        string contract = contractMatch.Success ? contractMatch.Groups[1].Value : "N/A";

        // Skapa unik nyckel för eventet
        string eventKey = $"Started|{missionId}";

        if (!_seenEvents.Contains(eventKey))
        {
          AddSeenEvent(eventKey);

          hasNewEvent = true;
          lastEventDescription = $"Mission started. Time: {timestamp}, missionId: {missionId}, generator name: {generatorName}, contract: {contract}";
          LastMissionEvent = new MissionEvent
          {
            MissionId = missionId,
            EventType = "Started",
            GeneratorName = generatorName,
            Contract = contract,
            StartTime = timestamp
          };
          var prevColor = Console.ForegroundColor;
          Console.ForegroundColor = ConsoleColor.Blue;
          Console.WriteLine("New mission event detected: " + lastEventDescription);
          Console.ForegroundColor = prevColor;
        }
      }
      else if (!string.IsNullOrEmpty(line) && line.Contains("<EndMission>"))
      {
        // Extract timestamp, missionId, and completionType
        var timeMatch = Regex.Match(line, @"<([0-9\-:T\.Z]+)>");
        var missionIdMatch = Regex.Match(line, @"MissionId\[([^\]]+)\]");
        var completionTypeMatch = Regex.Match(line, @"CompletionType\[([^\]]+)\]");

        DateTime? timestamp = null;
        if (timeMatch.Success && DateTime.TryParse(timeMatch.Groups[1].Value, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var parsedTime))
          timestamp = parsedTime.ToLocalTime();

        string missionId = missionIdMatch.Success ? missionIdMatch.Groups[1].Value : "N/A";
        string completionType = completionTypeMatch.Success ? completionTypeMatch.Groups[1].Value : "N/A";

        // Skapa unik nyckel för eventet
        string eventKey = $"Ended|{missionId}";

        if (!_seenEvents.Contains(eventKey))
        {
          AddSeenEvent(eventKey);

          hasNewEvent = true;
          lastEventDescription = $"Mission ended. Time: {timestamp}, missionId: {missionId}, completionType: {completionType}";
          LastMissionEvent = new MissionEvent
          {
            MissionId = missionId,
            EventType = "Ended",
            CompletionType = completionType,
            EndTime = timestamp
          };
          var prevColor = Console.ForegroundColor;
          Console.ForegroundColor = ConsoleColor.DarkBlue;
          Console.WriteLine("Mission end event detected: " + lastEventDescription);
          Console.ForegroundColor = prevColor;
        }
      }
      else
      {
        hasNewEvent = false;
        LastMissionEvent = null;
      }
    }
  }
}