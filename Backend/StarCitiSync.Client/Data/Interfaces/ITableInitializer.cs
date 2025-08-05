using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarCitiSync.Client.Data.Interfaces
{
  public interface ITableInitializer
  {
    void EnsureTable();
  }
}
