using StarCitiSync.Client.Models;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace StarCitiSync.Client.Services.Interfaces
{
  public interface ISessionInfoReader
  {
    SessionInfo ReadSessionInfo(string logFilePath);
  }
}
