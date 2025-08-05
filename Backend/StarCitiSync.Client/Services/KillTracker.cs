using StarCitiSync.Client.Services.Interfaces;
using StarCitiSync.Client.Models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace StarCitiSync.Client.Services
{
  public class KillTracker : IKillTracker, ILogLineProcessor
  {
    public bool hasNewKill;
    public string? lastKillDescription;
    public KillEvent? LastKillEvent { get; private set; }

    public bool HasNewKill => hasNewKill;
    public string? GetLastKillDescription() => lastKillDescription;

    public Dictionary<string, int> vehicleDestroyLevels = new();

    public void ProcessLogLine(object logLine)
    {
      if (logLine == null) throw new ArgumentNullException(nameof(logLine));
      string? line = logLine.ToString();
      if (string.IsNullOrEmpty(line))
      {
        hasNewKill = false;
        return;
      }

      var entityMatch = Regex.Match(line, @"'([^']+)'\s*\[([^\]]+)\]");
      string entityName = entityMatch.Success ? entityMatch.Groups[1].Value : "N/A";
      string entityId = entityMatch.Success ? entityMatch.Groups[2].Value : "N/A";

      if (line.Contains("<Actor Death>"))
      {
        var timeMatch = Regex.Match(line, @"^<([^>]+)>");
        var killedByMatch = Regex.Match(line, @"killed by '([^']+)'");
        var weaponMatch = Regex.Match(line, @"using '([^']+)'");
        var damageTypeMatch = Regex.Match(line, @"damage type '([^']+)'");

        DateTime? timestamp = null;
        if (timeMatch.Success && DateTime.TryParse(timeMatch.Groups[1].Value, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var parsedTime))
          timestamp = parsedTime;

        string killer = killedByMatch.Success ? killedByMatch.Groups[1].Value : "N/A";
        string weapon = weaponMatch.Success ? weaponMatch.Groups[1].Value : "N/A";
        string damageType = damageTypeMatch.Success ? damageTypeMatch.Groups[1].Value : "N/A";

        hasNewKill = true;
        lastKillDescription = $"Kill! Time: {timestamp}, Entity: {entityName} [{entityId}], Killer: {killer}, Weapon: {weapon}, DamageType: {damageType}";
        LastKillEvent = new KillEvent
        {
          Timestamp = timestamp,
          EntityName = entityName,
          EntityId = entityId,
          Killer = killer,
          Weapon = weapon,
          DamageType = damageType,
          DestroyLevel = null,
          EntityType = "Person"
        };
      }
      else if (line.Contains("<Vehicle Destruction>"))
      {
        var timeMatch = Regex.Match(line, @"^<([^>]+)>");
        var destroyLevelMatch = Regex.Match(line, @"destroy level (\d+) to (\d+)");
        var causedByMatch = Regex.Match(line, @"caused by '([^']+)'");
        var withMatch = Regex.Match(line, @"with '([^']+)'");

        DateTime? timestamp = null;
        if (timeMatch.Success && DateTime.TryParse(timeMatch.Groups[1].Value, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var parsedTime))
          timestamp = parsedTime;

        int newLevel = destroyLevelMatch.Success ? int.Parse(destroyLevelMatch.Groups[2].Value) : -1;
        string killer = causedByMatch.Success ? causedByMatch.Groups[1].Value : "N/A";
        string weapon = withMatch.Success ? withMatch.Groups[1].Value : "N/A";

        vehicleDestroyLevels[entityId] = newLevel;

        if (newLevel >= 2)
        {
          hasNewKill = true;
          lastKillDescription = $"Vehicle Kill! Time: {timestamp}, Entity: {entityName} [{entityId}], DestroyLevel: {newLevel}, Killer: {killer}, Weapon: {weapon}";
          LastKillEvent = new KillEvent
          {
            Timestamp = timestamp,
            EntityName = entityName,
            EntityId = entityId,
            Killer = killer,
            Weapon = weapon,
            DamageType = null,
            DestroyLevel = newLevel,
            EntityType = "Vehicle"
          };
        }
        else
        {
          hasNewKill = false;
        }
      }
      else
      {
        hasNewKill = false;
      }
      printKill();
    }

    public void printKill()
    {
      if (HasNewKill && lastKillDescription != null)
      {
        var prevColor = Console.ForegroundColor;
        Console.ForegroundColor = LastKillEvent.EntityType == "Vehicle" ? ConsoleColor.DarkMagenta : ConsoleColor.DarkRed;
        Console.WriteLine(GetLastKillDescription());
        Console.ForegroundColor = prevColor;
        Console.ForegroundColor = LastKillEvent?.EntityType == "Vehicle" ? ConsoleColor.DarkMagenta : ConsoleColor.DarkRed;
      }
    }
  }
}