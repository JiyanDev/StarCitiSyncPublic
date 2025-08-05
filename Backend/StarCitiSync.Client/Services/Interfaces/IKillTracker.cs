using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarCitiSync.Client.Services.Interfaces
{
  public interface IKillTracker
  {
    bool HasNewKill { get; }
    string GetLastKillDescription();
  }
}
