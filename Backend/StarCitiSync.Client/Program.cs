using StarCitiSync.Client.Data;
using StarCitiSync.Client.Models;
using StarCitiSync.Client.Services;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;


namespace StarCitiSync.Client
{
  internal class Program
  {
    static async Task Main(string[] args)
    {
      DatabaseInitializer.Initialize();
      var sessionInfoReader = new SessionInfoReader();
      var sessionRepo = new SessionRepository(DatabaseInitializer.ConnectionString);
      var processService = new StarCitizenProcessService();
      var sessionManager = new SessionManager(sessionRepo, sessionInfoReader);
      LogProcessingService logProcessingService = null;
      SessionInfo? currentSession = null;

      Task.Run(() =>
      {
        string? line;
        while ((line = Console.ReadLine()) != null)
        {
          if (line.Trim() == "exit")
          {
            break;
          }
        }
      });

      AppDomain.CurrentDomain.ProcessExit += (s, e) =>
      {
        if (currentSession != null)
        {
          currentSession.AppClose = DateTime.Now;
          sessionRepo.UpdateAppClose(currentSession.SessionId, currentSession.AppClose);
          Console.WriteLine($"AppClose set to {currentSession.AppClose}");
        }
      };
      Console.CancelKeyPress += (s, e) =>
      {
        if (currentSession != null)
        {
          currentSession.AppClose = DateTime.Now;
          sessionRepo.UpdateAppClose(currentSession.SessionId, currentSession.AppClose);
          Console.WriteLine($"AppClose set to {currentSession.AppClose}");
        }
      };

      while (true)
      {
        var (process, logFilePath) = await processService.WaitForProcessAndLogFileAsync("StarCitizen");
        var sessionInfo = await sessionManager.EnsureSessionAsync(logFilePath);
        currentSession = sessionInfo;

        if (logProcessingService == null)
          logProcessingService = new LogProcessingService(logFilePath, DatabaseInitializer.ConnectionString);

        using var cts = new CancellationTokenSource();

        var logTask = logProcessingService.ProcessLogAsync(sessionInfo, cts.Token);

        var monitorTask = Task.Run(() =>
        {
          process.WaitForExit();
          cts.Cancel();
        });

        await logTask;

        await processService.HandleSessionEndAndWaitForNextAsync(
            process,
            logFilePath,
            endDate =>
            {
              sessionInfo.EndDate = endDate;
              sessionInfo.AppClose = null;
              sessionRepo.Update(sessionInfo);
              Console.WriteLine($"Log End Date: {sessionInfo.EndDate}");
            },
            () =>
            {
              sessionInfo.EndDate = DateTime.Now;
              sessionInfo.AppClose = null;
              sessionRepo.Update(sessionInfo);
              Console.WriteLine("Session ended due to process exit.");
            }
        );
      }
    }
  }
}