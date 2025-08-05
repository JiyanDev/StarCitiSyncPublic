using Microsoft.Data.Sqlite;
using StarCitiSync.Client.Data.Interfaces;
using StarCitiSync.Client.Models;
using StarCitiSync.Client.Services.OCR;
using System;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Drawing; // For Bitmap
using System.IO;
using System.Windows.Forms; // For screen capture
using Tesseract; // Add this using

namespace StarCitiSync.Client.Data
{
  public class MissionRepository : ITableInitializer
  {
    private readonly string _connectionString;


    public int ReadRewardFromScreen()
    {
      var reader = new RewardReader();

      var gameWindowRect = reader.GetGameWindowRect("Star Citizen");
      if (gameWindowRect == Rectangle.Empty)
      {
        Console.WriteLine("Star Citizen-fönster inte hittat");
        return 0;
      }

      //var rewardAreaRelativeToGame = new Rectangle(2000 - 1900, 240 - 100, 350, 45);
      var rewardAreaRelativeToGame = reader.GetScaledRewardArea(gameWindowRect);

      int reward = reader.ReadRewardFromGame("Star Citizen", rewardAreaRelativeToGame);
      Console.WriteLine($"🎁 Reward: {reward}");

      if (reward > 0)
        return reward;
      return 0;
    }

    public MissionRepository(string connectionString)
    {
      _connectionString = connectionString;
    }

    public void EnsureTable()
    {
      using var conn = new SqliteConnection(_connectionString);
      conn.Open();
      var cmd = conn.CreateCommand();
      cmd.CommandText = @"
                  CREATE TABLE IF NOT EXISTS MissionEvents (
                      Id INTEGER PRIMARY KEY AUTOINCREMENT,
                      SessionId TEXT,
                      MissionId TEXT,
                      EventType TEXT,
                      GeneratorName TEXT,
                      Contract TEXT,
                      CompletionType TEXT,
                      StartTime DATETIME,
                      EndTime DATETIME,
                      Reward  INTEGER,
                      FOREIGN KEY (SessionId) REFERENCES Sessions(SessionId)
                  );";
      cmd.ExecuteNonQuery();
    }

    public void EnsureRewardTable()
    {
      using var conn = new SqliteConnection(_connectionString);
      conn.Open();
      var cmd = conn.CreateCommand();
      cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS MissionRewards (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Contract TEXT UNIQUE,
            Reward INTEGER
        );";
      cmd.ExecuteNonQuery();
    }

    public int GetOrInsertReward(string contract, int scannedReward)
    {
      using var conn = new SqliteConnection(_connectionString);
      conn.Open();

      // Try to get reward from table
      var selectCmd = conn.CreateCommand();
      selectCmd.CommandText = "SELECT Reward FROM MissionRewards WHERE Contract = $Contract;";
      selectCmd.Parameters.AddWithValue("$Contract", contract);

      var result = selectCmd.ExecuteScalar();
      if (result != null && int.TryParse(result.ToString(), out int dbReward))
      {
        // If scannedReward is 0 or differs by less than 10, use dbReward
        if (scannedReward == 0 || Math.Abs(dbReward - scannedReward) < 10)
          return dbReward;
      }

      // Otherwise, insert new reward if not present
      if (result == null && scannedReward > 0)
      {
        var insertCmd = conn.CreateCommand();
        insertCmd.CommandText = "INSERT INTO MissionRewards (Contract, Reward) VALUES ($Contract, $Reward);";
        insertCmd.Parameters.AddWithValue("$Contract", contract);
        insertCmd.Parameters.AddWithValue("$Reward", scannedReward);
        insertCmd.ExecuteNonQuery();
        return scannedReward;
      }

      // Fallback: use scannedReward if valid, otherwise 0
      return scannedReward > 0 ? scannedReward : 0;
    }

    public bool MissionExists(DateTime? StartTime, string missionId)
    {
      using var conn = new SqliteConnection(_connectionString);
      conn.Open();
      var cmd = conn.CreateCommand();
      cmd.CommandText = "SELECT COUNT(*) FROM MissionEvents WHERE StartTime = $StartTime AND MissionId = $MissionId;";
      cmd.Parameters.AddWithValue("$StartTime", StartTime?.ToString("yyyy-MM-dd HH:mm:ss"));
      cmd.Parameters.AddWithValue("$MissionId", missionId);
      var count = Convert.ToInt32(cmd.ExecuteScalar());
      return count > 0;
    }

    public void Insert(MissionEvent missionEvent)
    {
      EnsureRewardTable();

      if (MissionExists(missionEvent.StartTime, missionEvent.MissionId))
        return;

      using var conn = new SqliteConnection(_connectionString);
      conn.Open();

      var contract = missionEvent.Contract ?? string.Empty;
      var scannedReward = ReadRewardFromScreen();
      var reward = GetOrInsertReward(contract, scannedReward);
      Console.WriteLine($"Reward for contract '{contract}': {reward}");

      var cmd = conn.CreateCommand();
      cmd.CommandText = @"
                  INSERT INTO MissionEvents
                  (SessionId, MissionId, EventType, GeneratorName, Contract, CompletionType, StartTime, EndTime, Reward)
                  VALUES ($SessionId, $MissionId, $EventType, $GeneratorName, $Contract, $CompletionType, $StartTime, $EndTime, $Reward);";
      cmd.Parameters.AddWithValue("$SessionId", missionEvent.SessionId ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$MissionId", missionEvent.MissionId ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$EventType", missionEvent.EventType ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$GeneratorName", missionEvent.GeneratorName ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$Contract", missionEvent.Contract ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$CompletionType", missionEvent.CompletionType ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$StartTime", missionEvent.StartTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$EndTime", missionEvent.EndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$Reward", reward);
      cmd.ExecuteNonQuery();
    }

    public IEnumerable<MissionEvent> GetBySessionId(string sessionId)
    {
      using var conn = new SqliteConnection(_connectionString);
      conn.Open();
      var cmd = conn.CreateCommand();
      cmd.CommandText = "SELECT * FROM MissionEvents WHERE SessionId = $SessionId;";
      cmd.Parameters.AddWithValue("$SessionId", sessionId);

      using var reader = cmd.ExecuteReader();
      while (reader.Read())
      {
        yield return new MissionEvent
        {
          Id = reader.GetInt32(reader.GetOrdinal("Id")),
          SessionId = reader["SessionId"] as string,
          MissionId = reader["MissionId"] as string,
          EventType = reader["EventType"] as string,
          GeneratorName = reader["GeneratorName"] as string,
          Contract = reader["Contract"] as string,
          CompletionType = reader["CompletionType"] as string,
          StartTime = reader["StartTime"] != DBNull.Value ? DateTime.Parse(reader["StartTime"].ToString()!) : null,
          EndTime = reader["EndTime"] != DBNull.Value ? DateTime.Parse(reader["EndTime"].ToString()!) : null,
        };
      }
    }

    public MissionEvent? GetById(object id)
    {
      using var conn = new SqliteConnection(_connectionString);
      conn.Open();
      var cmd = conn.CreateCommand();
      cmd.CommandText = "SELECT * FROM MissionEvents WHERE Id = $Id;";
      cmd.Parameters.AddWithValue("$Id", id);

      using var reader = cmd.ExecuteReader();
      if (reader.Read())
      {
        return new MissionEvent
        {
          Id = reader.GetInt32(reader.GetOrdinal("Id")),
          SessionId = reader["SessionId"] as string,
          MissionId = reader["MissionId"] as string,
          EventType = reader["EventType"] as string,
          GeneratorName = reader["GeneratorName"] as string,
          Contract = reader["Contract"] as string,
          CompletionType = reader["CompletionType"] as string,
          StartTime = reader["StartTime"] != DBNull.Value ? DateTime.Parse(reader["StartTime"].ToString()!) : null,
          EndTime = reader["EndTime"] != DBNull.Value ? DateTime.Parse(reader["EndTime"].ToString()!) : null,
        };
      }

      return null;
    }

    public IEnumerable<MissionEvent> GetAll()
    {
      using var conn = new SqliteConnection(_connectionString);
      conn.Open();
      var cmd = conn.CreateCommand();
      cmd.CommandText = "SELECT * FROM MissionEvents;";

      using var reader = cmd.ExecuteReader();
      while (reader.Read())
      {
        yield return new MissionEvent
        {
          Id = reader.GetInt32(reader.GetOrdinal("Id")),
          SessionId = reader["SessionId"] as string,
          MissionId = reader["MissionId"] as string,
          EventType = reader["EventType"] as string,
          GeneratorName = reader["GeneratorName"] as string,
          Contract = reader["Contract"] as string,
          CompletionType = reader["CompletionType"] as string,
          StartTime = reader["StartTime"] != DBNull.Value ? DateTime.Parse(reader["StartTime"].ToString()!) : null,
          EndTime = reader["EndTime"] != DBNull.Value ? DateTime.Parse(reader["EndTime"].ToString()!) : null,
        };
      }
    }

    public void DeleteById(object id)
    {
      using var conn = new SqliteConnection(_connectionString);
      conn.Open();
      var cmd = conn.CreateCommand();
      cmd.CommandText = "DELETE FROM MissionEvents WHERE Id = $Id;";
      cmd.Parameters.AddWithValue("$Id", id);
      cmd.ExecuteNonQuery();
    }

    public void Update(MissionEvent missionEvent)
    {
      using var conn = new SqliteConnection(_connectionString);
      conn.Open();
      var cmd = conn.CreateCommand();
      cmd.CommandText = @"
                UPDATE MissionEvents
                SET EventType = $EventType,
                    CompletionType = $CompletionType,
                    EndTime = $EndTime
                WHERE MissionId = $MissionId AND SessionId = $SessionId;";
      cmd.Parameters.AddWithValue("$SessionId", missionEvent.SessionId);
      cmd.Parameters.AddWithValue("$MissionId", missionEvent.MissionId);
      cmd.Parameters.AddWithValue("$CompletionType", missionEvent.CompletionType ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$EventType", missionEvent.EventType ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$EndTime", missionEvent.EndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
      cmd.ExecuteNonQuery();
    }
  }
}