using Microsoft.Data.Sqlite;
using StarCitiSync.Client.Data.Interfaces;
using StarCitiSync.Client.Models;
using System;
using System.Collections.Generic;

namespace StarCitiSync.Client.Data
{
  public class SessionRepository : ITableInitializer
  {
    private readonly string _connectionString;

    public SessionRepository(string connectionString)
    {
      _connectionString = connectionString;
    }

    public void EnsureTable()
    {
      using var conn = new SqliteConnection(_connectionString);
      conn.Open();
      var cmd = conn.CreateCommand();
      cmd.CommandText = @"
                  CREATE TABLE IF NOT EXISTS Sessions (
                      Id INTEGER PRIMARY KEY AUTOINCREMENT,
                      SessionId TEXT UNIQUE,
                      StartDate DATETIME,
                      EndDate DATETIME,
                      Geid TEXT,
                      Username TEXT,
                      CreatedAt TEXT,
                      UpdatedAt TEXT,
                      AccountId INTEGER,
                      AppClose DATETIME
                  );";
      cmd.ExecuteNonQuery();
    }

    public void Insert(SessionInfo session)
    {
      using var conn = new SqliteConnection(_connectionString);
      conn.Open();
      var cmd = conn.CreateCommand();
      cmd.CommandText = @"
                  INSERT OR REPLACE INTO Sessions
                  (SessionId, StartDate, Geid, Username, CreatedAt, UpdatedAt, AccountId)
                  VALUES ($SessionId, $StartDate, $Geid, $Username, $CreatedAt, $UpdatedAt, $AccountId);";
      cmd.Parameters.AddWithValue("$SessionId", session.SessionId ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$StartDate", session.StartDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$Geid", session.Geid ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$Username", session.Username ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$CreatedAt", session.CreatedAt ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$UpdatedAt", session.UpdatedAt ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$AccountId", int.TryParse(session.AccountId, out var id) ? id : (object)DBNull.Value);
      cmd.ExecuteNonQuery();
    }

    public SessionInfo? GetById(object id)
    {
      if (id is not string sessionId)
        return null;

      using var conn = new SqliteConnection(_connectionString);
      conn.Open();
      var cmd = conn.CreateCommand();
      cmd.CommandText = "SELECT * FROM Sessions WHERE SessionId = $SessionId;";
      cmd.Parameters.AddWithValue("$SessionId", sessionId);

      using var reader = cmd.ExecuteReader();
      if (reader.Read())
      {
        return new SessionInfo
        {
          SessionId = reader["SessionId"] as string,
          StartDate = reader["StartDate"] != DBNull.Value ? DateTime.Parse(reader["StartDate"].ToString()!) : null,
          Geid = reader["Geid"] as string,
          Username = reader["Username"] as string,
          CreatedAt = reader["CreatedAt"] as string,
          UpdatedAt = reader["UpdatedAt"] as string,
          AccountId = reader["AccountId"]?.ToString(),
          AppClose = reader["AppClose"] != DBNull.Value ? DateTime.Parse(reader["AppClose"].ToString()!) : null,
        };
      }
      return null;
    }

    public SessionInfo? GetLatestSession()
    {
      using var conn = new SqliteConnection(_connectionString);
      conn.Open();
      var cmd = conn.CreateCommand();
      cmd.CommandText = @"
        SELECT * FROM Sessions
        ORDER BY Id DESC
        LIMIT 1;";

      using var reader = cmd.ExecuteReader();
      if (reader.Read())
      {
        return new SessionInfo
        {
          Id = reader["Id"].ToString(),
          SessionId = reader["SessionId"] as string,
          StartDate = reader["StartDate"] != DBNull.Value ? DateTime.Parse(reader["StartDate"].ToString()!) : null,
          EndDate = reader["EndDate"] != DBNull.Value ? DateTime.Parse(reader["EndDate"].ToString()!) : null,
          Geid = reader["Geid"] as string,
          Username = reader["Username"] as string,
          CreatedAt = reader["CreatedAt"] as string,
          UpdatedAt = reader["UpdatedAt"] as string,
          AccountId = reader["AccountId"]?.ToString(),
          AppClose = reader["AppClose"] != DBNull.Value ? DateTime.Parse(reader["AppClose"].ToString()!) : null
        };
      }
      return null;
    }

    public IEnumerable<SessionInfo> GetAll()
    {
      var sessions = new List<SessionInfo>();

      using var conn = new SqliteConnection(_connectionString);
      conn.Open();
      var cmd = conn.CreateCommand();
      cmd.CommandText = "SELECT * FROM Sessions;";

      using var reader = cmd.ExecuteReader();
      while (reader.Read())
      {
        sessions.Add(new SessionInfo
        {
          SessionId = reader["SessionId"] as string,
          StartDate = reader["StartDate"] != DBNull.Value ? DateTime.Parse(reader["StartDate"].ToString()!) : null,
          Geid = reader["Geid"] as string,
          Username = reader["Username"] as string,
          CreatedAt = reader["CreatedAt"] as string,
          UpdatedAt = reader["UpdatedAt"] as string,
          AccountId = reader["AccountId"]?.ToString(),
          AppClose = reader["AppClose"] != DBNull.Value ? DateTime.Parse(reader["AppClose"].ToString()!) : null,
        });
      }

      return sessions;
    }

    public void DeleteById(object id)
    {
      using var conn = new SqliteConnection(_connectionString);
      conn.Open();
      var cmd = conn.CreateCommand();
      cmd.CommandText = "DELETE FROM Sessions WHERE Id = $SessionId;";
      cmd.Parameters.AddWithValue("$SessionId", id);
      cmd.ExecuteNonQuery();
    }

    public void UpdateAppClose(string sessionId, DateTime? appClose)
    {
      using var conn = new SqliteConnection(_connectionString);
      conn.Open();
      var cmd = conn.CreateCommand();
      cmd.CommandText = @"
                  UPDATE Sessions SET
                    AppClose = $AppClose
                  WHERE SessionId = $SessionId;";
      cmd.Parameters.AddWithValue("$SessionId", sessionId ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$AppClose", appClose?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
      cmd.ExecuteNonQuery();
    }
    public void Update(SessionInfo session)
    {
      using var conn = new SqliteConnection(_connectionString);
      conn.Open();
      var cmd = conn.CreateCommand();
      cmd.CommandText = @"
                  UPDATE Sessions SET
                    EndDate = $EndDate
                  WHERE SessionId = $SessionId;";
      cmd.Parameters.AddWithValue("$SessionId", session.SessionId ?? (object)DBNull.Value);
      cmd.Parameters.AddWithValue("$EndDate", session.EndDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
       cmd.ExecuteNonQuery();
    }
    public SessionInfo? GetByStartDate(DateTime startDate)
    {
      using var conn = new SqliteConnection(_connectionString);
      conn.Open();
      var cmd = conn.CreateCommand();
      cmd.CommandText = "SELECT * FROM Sessions WHERE StartDate = $StartDate;";
      cmd.Parameters.AddWithValue("$StartDate", startDate.ToString("yyyy-MM-dd HH:mm:ss"));

      using var reader = cmd.ExecuteReader();
      if (reader.Read())
      {
        return new SessionInfo
        {
          SessionId = reader["SessionId"] as string,
          StartDate = reader["StartDate"] != DBNull.Value ? DateTime.Parse(reader["StartDate"].ToString()!) : null,
          EndDate = reader["EndDate"] != DBNull.Value ? DateTime.Parse(reader["EndDate"].ToString()!) : null,
          Geid = reader["Geid"] as string,
          Username = reader["Username"] as string,
          CreatedAt = reader["CreatedAt"] as string,
          UpdatedAt = reader["UpdatedAt"] as string,
          AccountId = reader["AccountId"]?.ToString()
        };
      }
      return null;
    }
  }
}