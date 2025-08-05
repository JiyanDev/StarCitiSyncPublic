using StarCitiSync.Client.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

public class FileLogReader : ILogReader
{
  private string logFilePath;

  public FileLogReader(string logFilePath)
  {
    this.logFilePath = logFilePath;
  }

  public async IAsyncEnumerable<string> ReadLinesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    using var stream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    using var reader = new StreamReader(stream);

    while (!cancellationToken.IsCancellationRequested)
    {
      string? line;
      while ((line = await reader.ReadLineAsync()) != null)
      {
        yield return line;
      }

      // Wait for new data to be written to the file
      await Task.Delay(200, cancellationToken);
    }
  }
}
