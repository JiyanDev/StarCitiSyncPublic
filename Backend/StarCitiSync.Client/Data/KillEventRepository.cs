using Microsoft.Data.Sqlite;
using StarCitiSync.Client.Data.Interfaces;
using StarCitiSync.Client.Models;
using System;
using System.Collections.Generic;

namespace StarCitiSync.Client.Data
{
  public class KillEventRepository : ITableInitializer
  {
    private readonly string _connectionString;

    public KillEventRepository(string connectionString)
    {
      _connectionString = connectionString;
    }

    public void EnsureTable()
    {
      using var conn = new SqliteConnection(_connectionString);
      conn.Open();
      var cmd = conn.CreateCommand();
      cmd.CommandText = @"
    CREATE TABLE IF NOT EXISTS KillEvents (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        SessionId TEXT,
        Timestamp DATETIME,
        EntityName TEXT,
        EntityId TEXT,
        Killer TEXT,
        Weapon TEXT,
        DamageType TEXT,
        DestroyLevel INTEGER,
        EntityType TEXT,
        UNIQUE(SessionId, Timestamp, EntityName, EntityId, Killer, EntityType),
        FOREIGN KEY (SessionId) REFERENCES Sessions(SessionId)
    );";
      cmd.ExecuteNonQuery();
    }

    public void Insert(KillEvent killEvent)
    {
      using var conn = new SqliteConnection(_connectionString);
      conn.Open();
      var cmd = conn.CreateCommand();
      cmd.CommandText = @"
    INSERT OR IGNORE INTO KillEvents
    (SessionId, Timestamp, EntityName, EntityId, Killer, Weapon, DamageType, DestroyLevel, EntityType)
    VALUES ($SessionId, $Timestamp, $EntityName, $EntityId, $Killer, $Weapon, $DamageType, $DestroyLevel, $EntityType);";
      cmd.Parameters.AddWithValue("$SessionId", killEvent.SessionId ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$Timestamp", killEvent.Timestamp?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$EntityName", killEvent.EntityName ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$EntityId", killEvent.EntityId ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$Killer", killEvent.Killer ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$Weapon", killEvent.Weapon ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$DamageType", killEvent.DamageType ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$DestroyLevel", killEvent.DestroyLevel ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$EntityType", killEvent.EntityType ?? (object)DBNull.Value);
      cmd.ExecuteNonQuery();
    }

    public KillEvent? GetById(object id)
    {
      using var conn = new SqliteConnection(_connectionString);
      conn.Open();
      var cmd = conn.CreateCommand();
      cmd.CommandText = "SELECT * FROM KillEvents WHERE Id = $Id;";
      cmd.Parameters.AddWithValue("$Id", id);

      using var reader = cmd.ExecuteReader();
      if (reader.Read())
      {
        return new KillEvent
        {
          Id = reader.GetInt32(reader.GetOrdinal("Id")),
          SessionId = reader["SessionId"] as string,
          Timestamp = reader["Timestamp"] != DBNull.Value ? DateTime.Parse(reader["Timestamp"].ToString()!) : null,
          EntityName = reader["EntityName"] as string,
          EntityId = reader["EntityId"] as string,
          Killer = reader["Killer"] as string,
          Weapon = reader["Weapon"] as string,
          DamageType = reader["DamageType"] as string,
          DestroyLevel = reader["DestroyLevel"] != DBNull.Value ? Convert.ToInt32(reader["DestroyLevel"]) : (int?)null,
          EntityType = reader["EntityType"] as string
        };
      }
      return null;
    }

    public IEnumerable<KillEvent> GetAll()
    {
      using var conn = new SqliteConnection(_connectionString);
      conn.Open();
      var cmd = conn.CreateCommand();
      cmd.CommandText = "SELECT * FROM KillEvents;";

      using var reader = cmd.ExecuteReader();
      while (reader.Read())
      {
        yield return new KillEvent
        {
          Id = reader.GetInt32(reader.GetOrdinal("Id")),
          SessionId = reader["SessionId"] as string,
          Timestamp = reader["Timestamp"] != DBNull.Value ? DateTime.Parse(reader["Timestamp"].ToString()!) : null,
          EntityName = reader["EntityName"] as string,
          EntityId = reader["EntityId"] as string,
          Killer = reader["Killer"] as string,
          Weapon = reader["Weapon"] as string,
          DamageType = reader["DamageType"] as string,
          DestroyLevel = reader["DestroyLevel"] != DBNull.Value ? Convert.ToInt32(reader["DestroyLevel"]) : (int?)null,
          EntityType = reader["EntityType"] as string
        };
      }
    }

    public void DeleteById(object id)
    {
      using var conn = new SqliteConnection(_connectionString);
      conn.Open();
      var cmd = conn.CreateCommand();
      cmd.CommandText = "DELETE FROM KillEvents WHERE Id = $Id;";
      cmd.Parameters.AddWithValue("$Id", id);
      cmd.ExecuteNonQuery();
    }

    public void Update(KillEvent killEvent)
    {
      using var conn = new SqliteConnection(_connectionString);
      conn.Open();
      var cmd = conn.CreateCommand();
      cmd.CommandText = @"
                UPDATE KillEvents SET
                    SessionId = $SessionId,
                    Timestamp = $Timestamp,
                    EntityName = $EntityName,
                    EntityId = $EntityId,
                    Killer = $Killer,
                    Weapon = $Weapon,
                    DamageType = $DamageType,
                    DestroyLevel = $DestroyLevel,
                    EntityType = $EntityType
                WHERE Id = $Id;";
      cmd.Parameters.AddWithValue("$Id", killEvent.Id);
      cmd.Parameters.AddWithValue("$SessionId", killEvent.SessionId ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$Timestamp", killEvent.Timestamp?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$EntityName", killEvent.EntityName ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$EntityId", killEvent.EntityId ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$Killer", killEvent.Killer ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$Weapon", killEvent.Weapon ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$DamageType", killEvent.DamageType ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$DestroyLevel", killEvent.DestroyLevel ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$EntityType", killEvent.EntityType ?? (object)DBNull.Value);
      cmd.ExecuteNonQuery();
    }
  }
}