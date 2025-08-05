using Microsoft.Data.Sqlite;
using StarCitiSync.Client.Data.Interfaces;
using StarCitiSync.Client.Models;
using System;
using System.Collections.Generic;

namespace StarCitiSync.Client.Data
{
    public class ShopTransactionRepository : ITableInitializer
    {
        private readonly string _connectionString;

        public ShopTransactionRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void EnsureTable()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS ShopTransactions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SessionId TEXT,
                    Timestamp DATETIME,
                    ShopName TEXT,
                    ShopId TEXT,
                    KioskId TEXT,
                    ClientPrice REAL,
                    ItemClassGuid TEXT,
                    ItemName TEXT,
                    Quantity INTEGER,
                    CurrencyType TEXT,
                    Result TEXT,
                    TransactionType TEXT,
                    FOREIGN KEY (SessionId) REFERENCES Sessions(SessionId),
                    UNIQUE (SessionId, Timestamp, ShopId, ItemName, Quantity)
                );";
            cmd.ExecuteNonQuery();
        }

        
        public void Insert(ShopTransaction transaction)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
               INSERT OR IGNORE INTO ShopTransactions
                (SessionId, Timestamp, ShopName, ShopId, KioskId, ClientPrice, ItemClassGuid, ItemName, Quantity, CurrencyType, Result)
                VALUES ($SessionId, $Timestamp, $ShopName, $ShopId, $KioskId, $ClientPrice, $ItemClassGuid, $ItemName, $Quantity, $CurrencyType, $Result);";
            cmd.Parameters.AddWithValue("$SessionId", transaction.SessionId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$Timestamp", transaction.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("$ShopName", transaction.ShopName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$ShopId", transaction.ShopId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$KioskId", transaction.KioskId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$ClientPrice", transaction.ClientPrice);
            cmd.Parameters.AddWithValue("$ItemClassGuid", transaction.ItemClassGuid ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$ItemName", transaction.ItemName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$Quantity", transaction.Quantity);
            cmd.Parameters.AddWithValue("$CurrencyType", transaction.CurrencyType ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$Result", transaction.Result ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public ShopTransaction? GetById(object id)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM ShopTransactions WHERE Id = $Id;";
            cmd.Parameters.AddWithValue("$Id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new ShopTransaction
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    SessionId = reader["SessionId"] as string,
                    Timestamp = reader["Timestamp"] != DBNull.Value ? DateTime.Parse(reader["Timestamp"].ToString()!) : default,
                    ShopName = reader["ShopName"] as string,
                    ShopId = reader["ShopId"] as string,
                    KioskId = reader["KioskId"] as string,
                    ClientPrice = reader["ClientPrice"] != DBNull.Value ? Convert.ToDecimal(reader["ClientPrice"]) : 0,
                    ItemClassGuid = reader["ItemClassGuid"] as string,
                    ItemName = reader["ItemName"] as string,
                    Quantity = reader["Quantity"] != DBNull.Value ? Convert.ToInt32(reader["Quantity"]) : 0,
                    CurrencyType = reader["CurrencyType"] as string,
                    Result = reader["Result"] as string
                };
            }
            return null;
        }

        public IEnumerable<ShopTransaction> GetAll()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM ShopTransactions;";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                yield return new ShopTransaction
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    SessionId = reader["SessionId"] as string,
                    Timestamp = reader["Timestamp"] != DBNull.Value ? DateTime.Parse(reader["Timestamp"].ToString()!) : default,
                    ShopName = reader["ShopName"] as string,
                    ShopId = reader["ShopId"] as string,
                    KioskId = reader["KioskId"] as string,
                    ClientPrice = reader["ClientPrice"] != DBNull.Value ? Convert.ToDecimal(reader["ClientPrice"]) : 0,
                    ItemClassGuid = reader["ItemClassGuid"] as string,
                    ItemName = reader["ItemName"] as string,
                    Quantity = reader["Quantity"] != DBNull.Value ? Convert.ToInt32(reader["Quantity"]) : 0,
                    CurrencyType = reader["CurrencyType"] as string,
                    Result = reader["Result"] as string
                };
            }
        }

        public void DeleteById(object id)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM ShopTransactions WHERE Id = $Id;";
            cmd.Parameters.AddWithValue("$Id", id);
            cmd.ExecuteNonQuery();
        }

        public void UpdateResult(string sessionId, DateTime timestamp, string itemName, string result)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE ShopTransactions
                SET Result = $Result
                WHERE SessionId = $SessionId AND Timestamp = $Timestamp AND ItemName = $ItemName;";
            cmd.Parameters.AddWithValue("$Result", result);
            cmd.Parameters.AddWithValue("$SessionId", sessionId);
            cmd.Parameters.AddWithValue("$Timestamp", timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("$ItemName", itemName ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public void Update(ShopTransaction transaction)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE ShopTransactions SET
                    SessionId = $SessionId,
                    Timestamp = $Timestamp,
                    ShopName = $ShopName,
                    ShopId = $ShopId,
                    KioskId = $KioskId,
                    ClientPrice = $ClientPrice,
                    ItemClassGuid = $ItemClassGuid,
                    ItemName = $ItemName,
                    Quantity = $Quantity,
                    CurrencyType = $CurrencyType,
                    Result = $Result
                WHERE Id = $Id;";
            cmd.Parameters.AddWithValue("$Id", transaction.Id);
            cmd.Parameters.AddWithValue("$SessionId", transaction.SessionId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$Timestamp", transaction.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("$ShopName", transaction.ShopName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$ShopId", transaction.ShopId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$KioskId", transaction.KioskId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$ClientPrice", transaction.ClientPrice);
            cmd.Parameters.AddWithValue("$ItemClassGuid", transaction.ItemClassGuid ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$ItemName", transaction.ItemName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$Quantity", transaction.Quantity);
            cmd.Parameters.AddWithValue("$CurrencyType", transaction.CurrencyType ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$Result", transaction.Result ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }
    }
}