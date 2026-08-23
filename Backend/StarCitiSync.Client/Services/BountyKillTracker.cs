using StarCitiSync.Client.Models;
using StarCitiSync.Client.Services.Interfaces;
using System;
using System.Text.RegularExpressions;

namespace StarCitiSync.Client.Services
{
  // Infers kills from HUD notifications since the game no longer writes
  // <Actor Death>/<Vehicle Destruction> lines to Game.log (SC 4.9+).
  public class BountyKillTracker : ILogLineProcessor
  {
    private static readonly Regex TimeRegex = new(@"^<([^>]+)>", RegexOptions.Compiled);
    private static readonly Regex BountyCompleteRegex = new(
      @"<SHUDEvent_OnNotification> Added notification ""Contract Complete:\s*(?:Verified\s+)?Bounty:\s*([^""]+?)\s*:?\s*""",
      RegexOptions.Compiled);
    private static readonly Regex IncapacitatedRegex = new(
      @"<SHUDEvent_OnNotification> Added notification ""Incapacitated:",
      RegexOptions.Compiled);

    public bool HasNewKill { get; private set; }
    public KillEvent? LastKillEvent { get; private set; }

    public void ProcessLogLine(object logLine)
    {
      HasNewKill = false;
      LastKillEvent = null;

      string? line = logLine?.ToString();
      if (string.IsNullOrEmpty(line) || !line.Contains("SHUDEvent_OnNotification"))
        return;

      DateTime? timestamp = null;
      var timeMatch = TimeRegex.Match(line);
      if (timeMatch.Success && DateTime.TryParse(timeMatch.Groups[1].Value, null,
          System.Globalization.DateTimeStyles.AdjustToUniversal, out var parsedTime))
        timestamp = parsedTime.ToLocalTime();

      var bountyMatch = BountyCompleteRegex.Match(line);
      if (bountyMatch.Success)
      {
        HasNewKill = true;
        LastKillEvent = new KillEvent
        {
          Timestamp = timestamp,
          EntityName = bountyMatch.Groups[1].Value.Trim(),
          EntityId = "N/A",
          Killer = null, // filled in with the session username
          Weapon = "N/A",
          DamageType = "Bounty",
          DestroyLevel = null,
          EntityType = "Vehicle"
        };
        Console.WriteLine($"Bounty kill detected: {LastKillEvent.EntityName} at {timestamp}");
      }
      else if (IncapacitatedRegex.IsMatch(line))
      {
        HasNewKill = true;
        LastKillEvent = new KillEvent
        {
          Timestamp = timestamp,
          EntityName = null, // filled in with the session username
          EntityId = "N/A",
          Killer = "Unknown",
          Weapon = "N/A",
          DamageType = "Incapacitated",
          DestroyLevel = null,
          EntityType = "Person"
        };
        Console.WriteLine($"Player incapacitated at {timestamp}");
      }
    }
  }
}
