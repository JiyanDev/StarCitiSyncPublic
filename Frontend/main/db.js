const sqlite3 = require('sqlite3').verbose();
const path = require('path');
const fs = require('fs');

let dbPath;
if (process.env.NODE_ENV === 'development') {
  dbPath = path.join(__dirname, '..', 'resources', 'backend', 'Data', 'starcitisync.db');
  console.log("🔧 Development mode: Using local database at", dbPath);
} else {
  dbPath = path.join(process.resourcesPath, 'backend', 'Data', 'starcitisync.db');
  console.log("🔧 Production mode: Using packaged database at", dbPath);
}

let db = null;

function initDb(pathOverride) {
  dbPath = pathOverride || dbPath;
  db = new sqlite3.Database(dbPath, (err) => {
    if (err) {
      console.error("❌ Could not open database:", err.message);
    } else {
      console.log("✅ Connected to the database!");
    }
  });
}

function getLatestSession() {
  return new Promise((resolve, reject) => {
    const query = `
      SELECT * FROM Sessions
      ORDER BY Id DESC
      LIMIT 1;
    `;
    db.get(query, (err, row) => {
      if (err) return reject(err);
      resolve(row);
    });
  });
}

function getSessionById(sessionId) {
  return new Promise((resolve, reject) => {
    db.get(`SELECT * FROM Sessions WHERE SessionId = ?`, [sessionId], (err, row) => {
      if (err) return reject(err);
      resolve(row);
    });
  });
}

function getMissionCountForSession(sessionId) {
  return new Promise((resolve, reject) => {
    const db = new sqlite3.Database(dbPath, sqlite3.OPEN_READONLY, (err) => {
      if (err) return reject(err);
    });

    const sql = `
      SELECT COUNT(DISTINCT MissionId) AS count
      FROM MissionEvents
      WHERE SessionId = ?
        AND (CompletionType = 'Complete' AND EventType = 'Ended')
    `;

    db.get(sql, [sessionId], (err, row) => {
      db.close();
      if (err) reject(err);
      else resolve(row.count);
    });
  });
}

function getTotalRewardForSession(sessionId) {
  return new Promise((resolve, reject) => {
    const db = new sqlite3.Database(dbPath, sqlite3.OPEN_READONLY, (err) => {
      if (err) return reject(err);
    });

    const sql = `
      SELECT SUM(Reward) AS total
      FROM MissionEvents
      WHERE SessionId = ?
        AND CompletionType = 'Complete'
        AND EventType = 'Ended'
    `;

    db.get(sql, [sessionId], (err, row) => {
      db.close();
      if (err) reject(err);
      else resolve(row.total ?? 0); 
    });
  });
}


function getLatestUnfinishedMission(sessionId) {
  return new Promise((resolve, reject) => {
    const db = new sqlite3.Database(dbPath, sqlite3.OPEN_READONLY, (err) => {
      if (err) return reject(err);
    });

    db.get(
      `
      SELECT * FROM MissionEvents
      WHERE SessionId = ? AND (CompletionType IS NULL OR CompletionType != 'Success')
      ORDER BY Id DESC
      LIMIT 1
      `,
      [sessionId],
      (err, row) => {
        db.close();
        if (err) reject(err);
        else resolve(row);
      }
    );
  });
}

function getRewardLastHourForSession(sessionId) {
  return new Promise((resolve, reject) => {
    const db = new sqlite3.Database(dbPath, sqlite3.OPEN_READONLY, (err) => {
      if (err) return reject(err);
    });

    const sql = `
      SELECT SUM(Reward) AS total
      FROM MissionEvents
      WHERE SessionId = ? AND
        CompletionType = 'Complete'
        AND EventType = 'Ended'
        AND EndTime >= datetime('now', '-1 hour', 'localtime');
    `;

    db.get(sql, [sessionId], (err, row) => {
      db.close();
      if (err) reject(err);
      else resolve(row.total ?? 0);
    });
  });
}

function getLatestOverlayKillEvents(limit = 5) {
  return new Promise((resolve, reject) => {
    const db = new sqlite3.Database(dbPath, sqlite3.OPEN_READONLY, (err) => {
      if (err) return reject(err);
    });

    const query = `
      SELECT *
      FROM (
        SELECT *
        FROM KillEvents
        ORDER BY timestamp DESC
        LIMIT ?
      ) sub
      ORDER BY timestamp ASC
    `;

    db.all(query, [limit], (err, rows) => {
      if (err) return reject(err);
      resolve(rows);
    });

    db.close();
  });
}

function toLocalDateTimeString(date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  const hour = String(date.getHours()).padStart(2, '0');
  const minute = String(date.getMinutes()).padStart(2, '0');

  return `${year}-${month}-${day} ${hour}:${minute}`;
}

function getShopSummary() {
  return new Promise((resolve, reject) => {
    const db = new sqlite3.Database(dbPath, sqlite3.OPEN_READONLY, (err) => {
      if (err) return reject(err);
    });

    const sql = `
      WITH TotalSpent AS (
        SELECT SUM(ClientPrice) AS TotalSpent
        FROM ShopTransactions
        WHERE Result = 'Success'
      ),
      TotalTransactions AS (
        SELECT COUNT(*) AS TotalTransactions
        FROM ShopTransactions
        WHERE Result = 'Success'
      ),
      TopItem AS (
        SELECT ItemName, SUM(ClientPrice) AS Total
        FROM ShopTransactions
        WHERE Result = 'Success'
        GROUP BY ItemName
        ORDER BY Total DESC
        LIMIT 1
      ),
      TopShop AS (
        SELECT ShopName, SUM(ClientPrice) AS Total
        FROM ShopTransactions
        WHERE Result = 'Success'
        GROUP BY ShopName
        ORDER BY Total DESC
        LIMIT 1
      )
      SELECT 
        ts.TotalSpent,
        tt.TotalTransactions,
        ti.ItemName AS TopItem,
        sh.ShopName AS TopShop
      FROM TotalSpent ts, TotalTransactions tt, TopItem ti, TopShop sh
    `;

    db.get(sql, [], (err, row) => {
      db.close();
      if (err) reject(err);
      else resolve(row);
    });
  });
}

async function getRewardPer10MinLastHour(sessionId) {
  return new Promise((resolve, reject) => {
    const sql = `
      SELECT
        strftime('%Y-%m-%d %H:', EndTime) || 
          printf('%02d', (CAST(strftime('%M', EndTime) AS INTEGER) / 10) * 10) AS time_bucket,
        SUM(Reward) as total_reward
      FROM MissionEvents
      WHERE 
        SessionId = ?
        AND EventType = 'Ended' 
        AND CompletionType = 'Complete'
        AND EndTime >= datetime('now', '-1 hour')
      GROUP BY time_bucket
      ORDER BY time_bucket ASC;
    `;

    db.all(sql, [sessionId], (err, rows) => {
      if (err) return reject(err);

      const now = new Date();
      const buckets = [];
      for (let i = 6; i >= 0; i--) {
        const date = new Date(now.getTime() - i * 10 * 60 * 1000);
        date.setMinutes(Math.floor(date.getMinutes() / 10) * 10);
        date.setSeconds(0);
        date.setMilliseconds(0);

        const bucket = toLocalDateTimeString(date);
        buckets.push({ time_bucket: bucket, total_reward: 0 });
      }

      rows.forEach(row => {
        const bucket = buckets.find(b => b.time_bucket === row.time_bucket);
        if (bucket) bucket.total_reward = row.total_reward;
      });

      const result = buckets.map(b => ({
        label: b.time_bucket.slice(11), // HH:MM
        reward: b.total_reward
      }));

      resolve(result);
    });
  });
}

function getdbPath() {
  let dbPath;
  if (process.env.NODE_ENV === 'development') {
    dbPath = path.join(__dirname, '..', 'resources', 'backend', 'Data', 'starcitisync.db');
    console.log("🔧 Development mode: Using local database at", dbPath);
  } else {
    dbPath = path.join(process.resourcesPath, 'backend', 'Data', 'starcitisync.db');
    console.log("🔧 Production mode: Using packaged database at", dbPath);
  }
  return dbPath;
}

function updateAppClose(sessionId, appCloseDate) {
  return new Promise((resolve, reject) => {
    const query = `UPDATE Sessions SET AppClose = ? WHERE SessionId = ?`;
    db.run(query, [appCloseDate, sessionId], function(err) {
      if (err) return reject(err);
      resolve();
    });
  });
}


module.exports = {
  getLatestSession,
  getSessionById,
  getMissionCountForSession,
  getLatestUnfinishedMission,
  getTotalRewardForSession,
  getRewardLastHourForSession,
  getRewardPer10MinLastHour,
  getShopSummary,
  getLatestOverlayKillEvents,
  initDb,
  getdbPath,
  updateAppClose
};
