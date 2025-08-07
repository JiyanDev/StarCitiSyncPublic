using StarCitiSync.Client.Models;
using StarCitiSync.Client.Data;
using System.Threading.Tasks;

namespace StarCitiSync.Client.Services
{
    public class LogProcessingService
    { 

      private readonly FileLogReader _logReader;
      private readonly ShopTransactionTracker _shopTransactionTracker;
      private readonly CommodityTransactionTracker _commodityBoxTransactionTracker;
      private readonly KillTracker _killTracker;
      private readonly MissionTracker _missionTracker;
      private readonly ActorStallTracker _actorStallTracker;

      private readonly MissionRepository _missionRepo;
      private readonly KillEventRepository _killRepo;
      private readonly ShopTransactionRepository _shopRepo;
      private readonly CommodityBoxTransactionRepository _commodityBoxRepo;
      private readonly SessionRepository _sessionRepo;
      private readonly ActorStallRepository _actorStallRepo;

      public LogProcessingService(string logFilePath, string connectionString)
      {
        _logReader = new FileLogReader(logFilePath);
        _shopTransactionTracker = new ShopTransactionTracker();
        _killTracker = new KillTracker();
        _missionTracker = new MissionTracker(_logReader);
        _commodityBoxTransactionTracker = new CommodityTransactionTracker();
        _actorStallTracker = new ActorStallTracker();

        _missionRepo = new MissionRepository(connectionString);
        _killRepo = new KillEventRepository(connectionString);
        _shopRepo = new ShopTransactionRepository(connectionString);
        _commodityBoxRepo = new CommodityBoxTransactionRepository(connectionString);
        _sessionRepo = new SessionRepository(connectionString);
        _actorStallRepo = new ActorStallRepository(connectionString);
      }

    public async Task ProcessLogAsync(SessionInfo sessionInfo, CancellationToken cancellationToken)
    {
      try
      {
        await foreach (var line in _logReader.ReadLinesAsync(cancellationToken))
        {
          _missionTracker.ProcessLogLine(line);
          _killTracker.ProcessLogLine(line);
          _shopTransactionTracker.ProcessLogLine(line);
          _commodityBoxTransactionTracker.ProcessLogLine(line);
          _actorStallTracker.ProcessLogLine(line);
          var endDate = SessionInfoReader.TryExtractSessionEndDate(line);

          if (_missionTracker.hasNewEvent && _missionTracker.LastMissionEvent != null)
          {
            _missionTracker.LastMissionEvent.SessionId = sessionInfo.SessionId;
            if(_missionTracker.LastMissionEvent.EventType == "Started")
              _missionRepo.Insert(_missionTracker.LastMissionEvent);
            else
              _missionRepo.Update(_missionTracker.LastMissionEvent);
          }

          if (_killTracker.HasNewKill && _killTracker.LastKillEvent != null)
          {
            _killTracker.LastKillEvent.SessionId = sessionInfo.SessionId;
            _killRepo.Insert(_killTracker.LastKillEvent);
          }

          if (_actorStallTracker.HasNewStall && _actorStallTracker.LastStallEvent != null)
          {
            _actorStallTracker.LastStallEvent.SessionId = sessionInfo.SessionId;
            var existing = _actorStallRepo.GetBySessionAndPlayer(sessionInfo.SessionId, _actorStallTracker.LastStallEvent.Player);

            if (existing == null)
            {
              _actorStallRepo.Upsert(_actorStallTracker.LastStallEvent);
            }
            else if (_actorStallTracker.LastStallEvent.Timestamp > existing.Timestamp)
            {
              _actorStallTracker.LastStallEvent.Id = existing.Id;
              _actorStallRepo.Upsert(_actorStallTracker.LastStallEvent);
            }
          }

          if (_shopTransactionTracker.Transactions.Count > 0)
          {
            var lastTransaction = _shopTransactionTracker.Transactions[^1];
            if (lastTransaction != null && lastTransaction.SessionId == null)
            {
              lastTransaction.SessionId = sessionInfo.SessionId;
              _shopRepo.Insert(lastTransaction);
            }
            else if (lastTransaction != null && lastTransaction.SessionId == sessionInfo.SessionId)
            {
              if (lastTransaction.Result != "pending")
              {
                _shopRepo.UpdateResult(
                    lastTransaction.SessionId,
                    lastTransaction.Timestamp,
                    lastTransaction.ItemName,
                    lastTransaction.Result
                );
              }
            }
          }
          if (_commodityBoxTransactionTracker.Transactions.Count > 0)
          {
            var lastCommodityTransaction = _commodityBoxTransactionTracker.Transactions[^1];
            if (lastCommodityTransaction != null && lastCommodityTransaction.SessionId == null)
            {
              lastCommodityTransaction.SessionId = sessionInfo.SessionId;
              _commodityBoxRepo.Insert(lastCommodityTransaction);
            }
          }
        }
      }
      catch (TaskCanceledException)
      {
      }
    }
  }
}