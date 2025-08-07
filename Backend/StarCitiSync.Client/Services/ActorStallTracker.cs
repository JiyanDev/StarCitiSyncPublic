using StarCitiSync.Client.Services.Interfaces;
using StarCitiSync.Client.Models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace StarCitiSync.Client.Services
{
  public class ActorStallTracker : IActorStallTracker, ILogLineProcessor
  {
    public bool hasNewStall;
    public string? lastStallDescription;
    public ActorStallEvent? LastStallEvent { get; private set; }
    public bool HasNewStall => hasNewStall;
    public string? GetLastStallDescription() => lastStallDescription;
    public void ProcessLogLine(object logLine)
    {
      if (logLine == null) throw new ArgumentNullException(nameof(logLine));
      string? line = logLine.ToString();
      if (string.IsNullOrEmpty(line))
      {
        hasNewStall = false;
        return;
      }

      if (line.Contains("<Actor stall>"))
      {
        // Extrahera timestamp
        var timeMatch = Regex.Match(line, @"^<([^>]+)>");
        DateTime? timestamp = null;
        if (timeMatch.Success && DateTime.TryParse(timeMatch.Groups[1].Value, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var parsedTime))
          timestamp = parsedTime.ToLocalTime();

        // Extrahera Player, Type, Length
        var playerMatch = Regex.Match(line, @"Player:\s*([^,]+)");
        var typeMatch = Regex.Match(line, @"Type:\s*([^,]+)");
        var lengthMatch = Regex.Match(line, @"Length:\s*([0-9.]+)");

        string player = playerMatch.Success ? playerMatch.Groups[1].Value.Trim() : "N/A";
        string type = typeMatch.Success ? typeMatch.Groups[1].Value.Trim() : "N/A";
        string lengthStr = lengthMatch.Success ? lengthMatch.Groups[1].Value.Trim().TrimEnd('.') : "0";
        double length = double.TryParse(
          lengthStr,
          System.Globalization.NumberStyles.Any,
          System.Globalization.CultureInfo.InvariantCulture,
          out var l) ? l : 0.0;

        hasNewStall = true;
        lastStallDescription = $"Stall! Time: {timestamp}, Player: {player}, Type: {type}, Length: {length}";
        Console.WriteLine(lastStallDescription);
        LastStallEvent = new ActorStallEvent
        {
          Timestamp = timestamp ?? DateTime.Now,
          Player = player,
          Type = type,
          Length = length
        };
      }
    }
  }
}
