using StarCitiSync.Client.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace StarCitiSync.Client.Data
{
  public static class DatabaseInitializer
  {
    public static string DbPath { get; set; }
    public static string ConnectionString => $"Data Source={DbPath}";

    public static void Initialize()
    {
      var exePath = Process.GetCurrentProcess().MainModule?.FileName!;
      var exeDir = Path.GetDirectoryName(exePath)!;
      var dataDir = Path.Combine(exeDir, "Data");
      Directory.CreateDirectory(dataDir);
      DbPath = Path.Combine(dataDir, "starcitisync.db");

      if (string.IsNullOrEmpty(DbPath))
        throw new ArgumentException("Database path cannot be null or empty.", nameof(DbPath));

      Console.WriteLine($"Initializing database at {DbPath}...");

      var connectionString = ConnectionString;
      var repositories = new List<ITableInitializer>
      {
          new SessionRepository(connectionString),
          new MissionRepository(connectionString),
          new ShopTransactionRepository(connectionString),
          new CommodityBoxTransactionRepository(connectionString),
          new KillEventRepository(connectionString)
      };
      foreach (var repo in repositories)
        repo.EnsureTable();

      Console.WriteLine("Database initialization complete.");
    }
  }
}