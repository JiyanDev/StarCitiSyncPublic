using StarCitiSync.Client.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StarCitiSync.Client.Services
{
    public class LogServiceManager
    {
        private readonly ILogReader _logReader;
        private readonly List<ILogLineProcessor> _processors;

        public LogServiceManager(ILogReader logReader, IEnumerable<ILogLineProcessor> processors)
        {
            _logReader = logReader;
            _processors = new List<ILogLineProcessor>(processors);
        }

        public async Task RunAsync()
        {
            await foreach (var line in _logReader.ReadLinesAsync())
            {
                foreach (var processor in _processors)
                {
                    processor.ProcessLogLine(line);
                }
            }
        }
    }
}