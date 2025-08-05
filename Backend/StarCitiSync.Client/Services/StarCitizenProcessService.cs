using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace StarCitiSync.Client.Services
{
  public class StarCitizenProcessService
  {
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool QueryFullProcessImageName(IntPtr hProcess, int flags, StringBuilder text, ref int size);

    public string? GetExecutablePath(Process process)
    {
      var buffer = new StringBuilder(1024);
      int size = buffer.Capacity;
      if (QueryFullProcessImageName(process.Handle, 0, buffer, ref size))
      {
        return buffer.ToString();
      }
      return null;
    }

    public async Task<(Process process, string logFilePath)> WaitForProcessAndLogFileAsync(string processName = "StarCitizen")
    {
      Process? process = null;
      string? exePath = null;
      string? logFilePath = null;

      while (process == null)
      {
        process = Process.GetProcessesByName(processName).FirstOrDefault();
        if (process == null)
        {
          Console.WriteLine($"waiting for {processName} to start...");
          await Task.Delay(10000);
        }
        else
        {
          var processes = Process.GetProcessesByName(processName);
          foreach (var process2 in processes)
          {
            try
            {
              await Task.Delay(10000);
              exePath = GetExecutablePath(process2);
              Console.WriteLine($"PID {process2.Id}: {exePath}");
            }
            catch (Exception ex)
            {
              Console.WriteLine($"Fel: {ex.Message}");
            }
          }
          if (exePath == null)
            throw new InvalidOperationException("Kunde inte hitta sökväg till StarCitizen-processen.");

          string? liveDir = Directory.GetParent(Directory.GetParent(exePath)!.FullName)!.FullName;
          logFilePath = Path.Combine(liveDir, "Game.log");

          if (!File.Exists(logFilePath))
          {
            Console.WriteLine($"Game.log hittades inte i {logFilePath}. Väntar på att filen ska skapas...");
            while (!File.Exists(logFilePath))
            {
              await Task.Delay(1000);
            }
          }
          Console.WriteLine("StarCitizen is running, skipping log clear.");
        }
      }

      return (process, logFilePath!);
    }

    public async Task HandleSessionEndAndWaitForNextAsync(
            Process process,
            string logFilePath,
            Action<DateTime> onSessionEnd,
            Action onCrashSessionEnd)
    {
      if (process != null && !process.HasExited)
      {
        process.WaitForExit();
      }

      bool cleared = false;
      int retryCount = 0;
      while (!cleared && retryCount < 10)
      {
        try
        {
          File.WriteAllText(logFilePath, string.Empty);
          cleared = true;
        }
        catch (IOException)
        {
          retryCount++;
          await Task.Delay(5000);
        }
      }
      if (!cleared)
        Console.WriteLine("Could not clear the log file after multiple attempts. It may still be locked by another process.");

      onCrashSessionEnd?.Invoke();

      Console.WriteLine("Waiting for next Star Citizen session...");
      while (process != null && !process.HasExited)
      {
        await Task.Delay(5000);
        process = Process.GetProcessesByName(process.ProcessName).FirstOrDefault();
      }
    }
  }
}