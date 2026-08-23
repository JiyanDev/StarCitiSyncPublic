using StarCitiSync.Client.Models;
using StarCitiSync.Client.Data;
using StarCitiSync.Client.Services.OCR;
using System.Threading.Tasks;

namespace StarCitiSync.Client.Services
{
    public class LogProcessingService
    { 

      private readonly FileLogReader _logReader;
      private readonly ShopTransactionTracker _shopTransactionTracker;
      private readonly CommodityTransactionTracker _commodityBoxTransactionTracker;
      private readonly KillTracker _killTracker;
      private readonly BountyKillTracker _bountyKillTracker;
      private readonly MissionTracker _missionTracker;
      private readonly ActorStallTracker _actorStallTracker;
      private readonly RewardReader _rewardReader;

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
        _bountyKillTracker = new BountyKillTracker();
        _missionTracker = new MissionTracker(_logReader);
        _commodityBoxTransactionTracker = new CommodityTransactionTracker();
        _actorStallTracker = new ActorStallTracker();
        _rewardReader = new RewardReader();

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
          _bountyKillTracker.ProcessLogLine(line);
          _shopTransactionTracker.ProcessLogLine(line);
          _commodityBoxTransactionTracker.ProcessLogLine(line);
          _actorStallTracker.ProcessLogLine(line);
          var endDate = SessionInfoReader.TryExtractSessionEndDate(line);

          if (_missionTracker.hasNewEvent && _missionTracker.LastMissionEvent != null)
          {
            var missionEvent = _missionTracker.LastMissionEvent;
            missionEvent.SessionId = sessionInfo.SessionId;
            if (missionEvent.EventType == "Started")
            {
              // Insert immediately with any cached reward; OCR runs in the
              // background so it never stalls the log pipeline.
              var cachedReward = _missionRepo.GetOrInsertReward(missionEvent.Contract ?? string.Empty, 0);
              _missionRepo.Insert(missionEvent, cachedReward);
              if (cachedReward == 0)
                _ = Task.Run(() => UpdateRewardFromScreen(missionEvent));
            }
            else
            {
              _missionRepo.Update(missionEvent);
            }
          }

          if (_killTracker.HasNewKill && _killTracker.LastKillEvent != null)
          {
            _killTracker.LastKillEvent.SessionId = sessionInfo.SessionId;
            _killRepo.Insert(_killTracker.LastKillEvent);
          }

          if (_bountyKillTracker.HasNewKill && _bountyKillTracker.LastKillEvent is { } inferredKill)
          {
            inferredKill.SessionId = sessionInfo.SessionId;
            if (inferredKill.DamageType == "Bounty")
              inferredKill.Killer = sessionInfo.Username ?? "You";
            else
              inferredKill.EntityName = sessionInfo.Username ?? "You";
            _killRepo.Insert(inferredKill);
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

          if (_shopTransactionTracker.NewTransaction is { } newShopTransaction)
          {
            newShopTransaction.SessionId = sessionInfo.SessionId;
            _shopRepo.Insert(newShopTransaction);
          }

          if (_shopTransactionTracker.CompletedTransaction is { SessionId: not null } completedShopTransaction)
          {
            _shopRepo.UpdateResult(
                completedShopTransaction.SessionId,
                completedShopTransaction.Timestamp,
                completedShopTransaction.ItemName,
                completedShopTransaction.Result
            );
          }

          foreach (var commodityTransaction in _commodityBoxTransactionTracker.NewTransactions)
          {
            commodityTransaction.SessionId = sessionInfo.SessionId;
            _commodityBoxRepo.Insert(commodityTransaction);
          }
        }
      }
      catch (TaskCanceledException)
      {
      }
    }

    private void UpdateRewardFromScreen(MissionEvent missionEvent)
    {
      try
      {
        var scannedReward = _rewardReader.ReadCurrentMissionReward();
        if (scannedReward <= 0)
          return;

        var reward = _missionRepo.GetOrInsertReward(missionEvent.Contract ?? string.Empty, scannedReward);
        Console.WriteLine($"🎁 Reward for contract '{missionEvent.Contract}': {reward}");
        _missionRepo.UpdateReward(missionEvent.SessionId!, missionEvent.MissionId!, reward);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Could not read mission reward from screen: {ex.Message}");
      }
    }
  }
}