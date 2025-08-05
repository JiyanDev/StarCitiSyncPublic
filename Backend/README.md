# StarCitiSync Backend

This is the backend for StarCitiSync, a .NET 9 Windows application that parses and logs events from Star Citizen log files. The backend handles session data, missions, shop transactions, commodity box transactions, and kill events, storing all data in a local SQLite database.

## Features

- Automatic detection of the Star Citizen process and log file
- Real-time log file analysis for:
  - Session start and end
  - Mission start, end, and reward (including OCR for mission rewards)
  - Shop transactions (buy/sell)
  - Commodity box transactions
  - Kill events
- All data is stored in a local SQLite database (`Data/starcitisync.db`)
- Robust session end handling, even on crash or unexpected shutdown

## Technical Overview

- **Language & Framework:** C# 13, .NET 9, Windows Forms (for OCR/screen capture)
- **Database:** SQLite (via `Microsoft.Data.Sqlite`)
- **OCR:** Tesseract (to read mission rewards from the screen)
- **Structure:**
  - `Data/` – Database handlers and repositories
  - `Models/` – Data models for sessions, missions, events, transactions
  - `Services/` – Logic for log file reading, process handling, OCR, trackers
  - `tessdata/` – Tesseract OCR data (e.g. `eng.traineddata`)

## Getting Started

1. **Requirements:**  
   - Windows 10/11  
   - .NET 9 SDK  
   - Star Citizen installed

2. **Build and run:**

cd backend/StarCitiSync.Client
```bash
cd backend/StarCitiSync.Client
dotnet build
dotnet run
```

3. **Configuration:**  
   - Tesseract data (`tessdata/`) must be present in the project folder (at least `eng.traineddata`).
   - Icon files (`logo2.ico`, `logo2.png`) are included in the project.

4. **Database:**  
   - The database is created automatically in `Data/` on first run.

## Key Dependencies

- [Microsoft.Data.Sqlite](https://www.nuget.org/packages/Microsoft.Data.Sqlite)
- [Tesseract](https://www.nuget.org/packages/Tesseract)
- [System.Drawing.Common](https://www.nuget.org/packages/System.Drawing.Common)

## Project Structure

```
backend/
└── StarCitiSync.Client/
    ├── Data/
    ├── Models/
    ├── Services/
    ├── tessdata/
    ├── Program.cs
    └── StarCitiSync.Client.csproj
```

## Notes

- Make sure your `.gitignore` excludes `bin/`, `obj/`, local databases, and temporary files.
- For frontend details, see `frontend/README.md`.

---

**StarCitiSync** – Automatically analyze and save your Star Citizen data!
