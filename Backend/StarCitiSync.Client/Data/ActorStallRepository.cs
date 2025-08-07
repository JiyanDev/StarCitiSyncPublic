using Microsoft.Data.Sqlite;
using StarCitiSync.Client.Data.Interfaces;
using StarCitiSync.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarCitiSync.Client.Data
{
  public class ActorStallRepository : ITableInitializer
  {
    private readonly string _connectionString;
    public ActorStallRepository(string connectionString)
    {
      _connectionString = connectionString;
    }
    public void EnsureTable()
    {
      using var conn = new SqliteConnection(_connectionString);
      conn.Open();
      var cmd = conn.CreateCommand();
      cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS ActorStallEvents (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Player TEXT,
            Type TEXT,
            Length REAL,
            Timestamp DATETIME,
            SessionId TEXT,
            UNIQUE(SessionId, Player),
            FOREIGN KEY (SessionId) REFERENCES Sessions(SessionId)
        );";
      cmd.ExecuteNonQuery();
    }
    public void Upsert(ActorStallEvent actorStallEvent)
    {
      using var conn = new SqliteConnection(_connectionString);
      conn.Open();
      var cmd = conn.CreateCommand();
      cmd.CommandText = @"
        INSERT OR REPLACE INTO ActorStallEvents (Id, Player, Type, Length, Timestamp, SessionId)
        VALUES (
            (SELECT Id FROM ActorStallEvents WHERE SessionId = $SessionId AND Player = $Player),
            $Player, $Type, $Length, $Timestamp, $SessionId
        );";
      cmd.Parameters.AddWithValue("$Player", actorStallEvent.Player ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$Type", actorStallEvent.Type ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$Length", actorStallEvent.Length);
      cmd.Parameters.AddWithValue("$Timestamp", actorStallEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
      cmd.Parameters.AddWithValue("$SessionId", actorStallEvent.SessionId ?? (object)DBNull.Value);
      cmd.ExecuteNonQuery();
    }
    public ActorStallEvent? GetBySessionAndPlayer(string sessionId, string player)
    {
      using var conn = new SqliteConnection(_connectionString);
      conn.Open();
      var cmd = conn.CreateCommand();
      cmd.CommandText = @"
        SELECT Id, Player, Type, Length, Timestamp, SessionId
        FROM ActorStallEvents
        WHERE SessionId = $SessionId AND Player = $Player;";
      cmd.Parameters.AddWithValue("$SessionId", sessionId);
      cmd.Parameters.AddWithValue("$Player", player);

      using var reader = cmd.ExecuteReader();
      if (reader.Read())
      {
        return new ActorStallEvent
        {
          Id = reader.GetInt32(0),
          Player = reader.GetString(1),
          Type = reader.GetString(2),
          Length = reader.GetDouble(3),
          Timestamp = reader.GetDateTime(4),
          SessionId = reader.GetString(5)
        };
      }
      return null;
    }
  }
}
