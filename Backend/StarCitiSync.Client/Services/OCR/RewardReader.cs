using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Tesseract;

namespace StarCitiSync.Client.Services.OCR
{
  public class RewardReader
  {
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    struct RECT
    {
      public int Left; public int Top;
      public int Right; public int Bottom;
    }

    public int ReadRewardFromGame(string partialWindowTitle, Rectangle rewardAreaRelativeToGame)
    {
      var hWnd = FindWindow(null, null);
      foreach (var p in Process.GetProcessesByName("StarCitizen"))
      {
        if (p.MainWindowTitle.Contains(partialWindowTitle))
        {
          hWnd = p.MainWindowHandle;
          break;
        }
      }

      if (hWnd == IntPtr.Zero)
      {
        Console.WriteLine("❌ Kunde inte hitta Star Citizen-fönstret.");
        return 0;
      }

      if (!GetWindowRect(hWnd, out var rect))
      {
        Console.WriteLine("❌ Kunde inte läsa fönstrets koordinater.");
        return 0;
      }

      var gameWindowRect = new Rectangle(
          rect.Left,
          rect.Top,
          rect.Right - rect.Left,
          rect.Bottom - rect.Top
      );

      var rewardArea = new Rectangle(
          gameWindowRect.X + rewardAreaRelativeToGame.X,
          gameWindowRect.Y + rewardAreaRelativeToGame.Y,
          rewardAreaRelativeToGame.Width,
          rewardAreaRelativeToGame.Height
      );

      using var bmp = new Bitmap(rewardArea.Width, rewardArea.Height);
      using (var g = Graphics.FromImage(bmp))
      {
        g.CopyFromScreen(rewardArea.Location, Point.Empty, rewardArea.Size);
      }

      // saving picture for debugging purposes
      //var filePath = $@"..\temp\reward_dump_{DateTime.Now:yyyyMMdd_HHmmss}.png";
      //bmp.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);

      using var ms = new MemoryStream();
      bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
      ms.Position = 0;
      using var img = Pix.LoadFromMemory(ms.ToArray());
      string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
      Console.WriteLine($"🔍 Using Tesseract data from: {exeDir}");
      string tessdataPath = Path.Combine(exeDir, "tessdata");
      using var engine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default);
      using var page = engine.Process(img);

      var text = page.GetText();
      Console.WriteLine($"📄 TEXT: '{text}'");
      var digits = System.Text.RegularExpressions.Regex.Replace(text, @"[^\d]", "");
      return int.TryParse(digits, out int reward) ? reward : 0;
    }

    public Rectangle GetScaledRewardArea(Rectangle gameWindowRect)
    {
      int marginX = 20;
      int marginY = 10;
      int x = (int)(gameWindowRect.Width * 0.781) - marginX;
      int y = (int)(gameWindowRect.Height * 0.167) - marginY;
      int width = (int)(gameWindowRect.Width * (0.918 - 0.781)) + 2 * marginX;
      int height = (int)(gameWindowRect.Height * (0.198 - 0.167)) + 2 * marginY;

      return new Rectangle(gameWindowRect.X + x, gameWindowRect.Y + y, width, height);
    }
    public Rectangle GetGameWindowRect(string partialWindowTitle)
    {
      IntPtr hWnd = IntPtr.Zero;
      foreach (var proc in Process.GetProcessesByName("StarCitizen"))
      {
        if (proc.MainWindowTitle.Contains(partialWindowTitle))
        {
          hWnd = proc.MainWindowHandle;
          break;
        }
      }

      if (hWnd == IntPtr.Zero)
        return Rectangle.Empty;

      if (!GetWindowRect(hWnd, out RECT rect))
        return Rectangle.Empty;

      return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }
  }
}
