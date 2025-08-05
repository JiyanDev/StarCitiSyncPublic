using Microsoft.Data.Sqlite;
using StarCitiSync.Client.Models;
using StarCitiSync.Client.Data.Interfaces;
using System;
using System.Collections.Generic;

namespace StarCitiSync.Client.Data
{
    public class CommodityBoxTransactionRepository : ITableInitializer
    {
        private readonly string _connectionString;

        public CommodityBoxTransactionRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void EnsureTable()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS CommodityBoxTransactions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SessionId TEXT,
                    Timestamp DATETIME,
                    EventType TEXT,
                    PlayerId TEXT,
                    ShopId TEXT,
                    ShopName TEXT,
                    KioskId TEXT,
                    Price REAL,
                    ShopPricePerCentiSCU REAL,
                    ResourceGuid TEXT,
                    AutoLoading INTEGER,
                    Quantity REAL,
                    QuantityUnit TEXT,
                    CommodityName TEXT,
                    BoxSize INTEGER,
                    UnitAmount INTEGER,
                    UNIQUE(SessionId, Timestamp, PlayerId, Price, Quantity)
                );";
            cmd.ExecuteNonQuery();
        }

        public void Insert(CommodityBoxTransaction transaction)
        {
           using var conn = new SqliteConnection(_connectionString);
           conn.Open();
           var cmd = conn.CreateCommand();
           cmd.CommandText = @"
                INSERT OR IGNORE INTO CommodityBoxTransactions
                (SessionId, Timestamp, EventType, PlayerId, ShopId, ShopName, KioskId, Price, ShopPricePerCentiSCU, ResourceGuid, AutoLoading, Quantity, QuantityUnit, CommodityName, BoxSize, UnitAmount)
                VALUES ($SessionId, $Timestamp, $EventType, $PlayerId, $ShopId, $ShopName, $KioskId, $Price, $ShopPricePerCentiSCU, $ResourceGuid, $AutoLoading, $Quantity, $QuantityUnit, $CommodityName, $BoxSize, $UnitAmount);";
            cmd.Parameters.AddWithValue("$SessionId", transaction.SessionId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$Timestamp", transaction.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("$EventType", transaction.EventType ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$PlayerId", transaction.PlayerId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$ShopId", transaction.ShopId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$ShopName", transaction.ShopName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$KioskId", transaction.KioskId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$Price", transaction.Price ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$ShopPricePerCentiSCU", transaction.ShopPricePerCentiSCU ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$ResourceGuid", transaction.ResourceGuid ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$AutoLoading", transaction.AutoLoading ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$Quantity", transaction.Quantity ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$QuantityUnit", transaction.QuantityUnit ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$CommodityName", transaction.CommodityName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$BoxSize", transaction.BoxSize ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$UnitAmount", transaction.UnitAmount ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public CommodityBoxTransaction? GetById(object id)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM CommodityBoxTransactions WHERE Id = $Id;";
            cmd.Parameters.AddWithValue("$Id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new CommodityBoxTransaction
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    SessionId = reader["SessionId"] as string,
                    Timestamp = reader["Timestamp"] != DBNull.Value ? DateTime.Parse(reader["Timestamp"].ToString()!) : default,
                    EventType = reader["EventType"] as string,
                    PlayerId = reader["PlayerId"] as string,
                    ShopId = reader["ShopId"] as string,
                    ShopName = reader["ShopName"] as string,
                    KioskId = reader["KioskId"] as string,
                    Price = reader["Price"] != DBNull.Value ? Convert.ToDecimal(reader["Price"]) : null,
                    ShopPricePerCentiSCU = reader["ShopPricePerCentiSCU"] != DBNull.Value ? Convert.ToDecimal(reader["ShopPricePerCentiSCU"]) : null,
                    ResourceGuid = reader["ResourceGuid"] as string,
                    AutoLoading = reader["AutoLoading"] != DBNull.Value ? Convert.ToInt32(reader["AutoLoading"]) : null,
                    Quantity = reader["Quantity"] != DBNull.Value ? Convert.ToDecimal(reader["Quantity"]) : null,
                    QuantityUnit = reader["QuantityUnit"] as string,
                    CommodityName = reader["CommodityName"] as string,
                    BoxSize = reader["BoxSize"] != DBNull.Value ? Convert.ToInt32(reader["BoxSize"]) : null,
                    UnitAmount = reader["UnitAmount"] != DBNull.Value ? Convert.ToInt32(reader["UnitAmount"]) : null
                };
            }
            return null;
        }

        public IEnumerable<CommodityBoxTransaction> GetAll()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM CommodityBoxTransactions;";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                yield return new CommodityBoxTransaction
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    SessionId = reader["SessionId"] as string,
                    Timestamp = reader["Timestamp"] != DBNull.Value ? DateTime.Parse(reader["Timestamp"].ToString()!) : default,
                    EventType = reader["EventType"] as string,
                    PlayerId = reader["PlayerId"] as string,
                    ShopId = reader["ShopId"] as string,
                    ShopName = reader["ShopName"] as string,
                    KioskId = reader["KioskId"] as string,
                    Price = reader["Price"] != DBNull.Value ? Convert.ToDecimal(reader["Price"]) : null,
                    ShopPricePerCentiSCU = reader["ShopPricePerCentiSCU"] != DBNull.Value ? Convert.ToDecimal(reader["ShopPricePerCentiSCU"]) : null,
                    ResourceGuid = reader["ResourceGuid"] as string,
                    AutoLoading = reader["AutoLoading"] != DBNull.Value ? Convert.ToInt32(reader["AutoLoading"]) : null,
                    Quantity = reader["Quantity"] != DBNull.Value ? Convert.ToDecimal(reader["Quantity"]) : null,
                    QuantityUnit = reader["QuantityUnit"] as string,
                    CommodityName = reader["CommodityName"] as string,
                    BoxSize = reader["BoxSize"] != DBNull.Value ? Convert.ToInt32(reader["BoxSize"]) : null,
                    UnitAmount = reader["UnitAmount"] != DBNull.Value ? Convert.ToInt32(reader["UnitAmount"]) : null
                };
            }
        }

        public void DeleteById(object id)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM CommodityBoxTransactions WHERE Id = $Id;";
            cmd.Parameters.AddWithValue("$Id", id);
            cmd.ExecuteNonQuery();
        }

        public void Update(CommodityBoxTransaction transaction)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE CommodityBoxTransactions SET
                    SessionId = $SessionId,
                    Timestamp = $Timestamp,
                    EventType = $EventType,
                    PlayerId = $PlayerId,
                    ShopId = $ShopId,
                    ShopName = $ShopName,
                    KioskId = $KioskId,
                    Price = $Price,
                    ShopPricePerCentiSCU = $ShopPricePerCentiSCU,
                    ResourceGuid = $ResourceGuid,
                    AutoLoading = $AutoLoading,
                    Quantity = $Quantity,
                    QuantityUnit = $QuantityUnit,
                    CommodityName = $CommodityName,
                    BoxSize = $BoxSize,
                    UnitAmount = $UnitAmount
                WHERE Id = $Id;";
            cmd.Parameters.AddWithValue("$Id", transaction.Id);
            cmd.Parameters.AddWithValue("$SessionId", transaction.SessionId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$Timestamp", transaction.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("$EventType", transaction.EventType ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$PlayerId", transaction.PlayerId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$ShopId", transaction.ShopId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$ShopName", transaction.ShopName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$KioskId", transaction.KioskId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$Price", transaction.Price ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$ShopPricePerCentiSCU", transaction.ShopPricePerCentiSCU ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$ResourceGuid", transaction.ResourceGuid ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$AutoLoading", transaction.AutoLoading ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$Quantity", transaction.Quantity ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$QuantityUnit", transaction.QuantityUnit ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$CommodityName", transaction.CommodityName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$BoxSize", transaction.BoxSize ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$UnitAmount", transaction.UnitAmount ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }
    }
}