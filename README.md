# StellarisCharts

Parse Stellaris save files (`.sav`) and visualize empire and galaxy stats in a web UI.

## What it does
- Upload/scan `.sav` files and extract `gamestate`
- Store snapshots, budgets, and species demographics in Postgres
- Show empire and galaxy dashboards (charts, monthlies, species pie, if you like numbers like me :p)

<img width="1318" height="1019" alt="image" src="https://github.com/user-attachments/assets/3e7d619a-30ba-4721-84a6-baa6ab48974b" />

## Prereqs
- .NET SDK 10
- Node.js 18+
- PostgreSQL 14+
- Docker Desktop (optional, for Postgres via docker-compose)

## Backend setup
1. Copy the template config and set your connection string:
   ```
   copy backend\StellarisCharts.Api\appsettings.Template.json backend\StellarisCharts.Api\appsettings.json
   ```
2. Update `backend\StellarisCharts.Api\appsettings.json` with your DB credentials.
   - You can also use an env var instead:
     `ConnectionStrings__DefaultConnection=Host=localhost;Database=stellaris;Username=stellaris;Password=YOUR_PASSWORD`
3. Run migrations:
   ```
   dotnet tool install --global dotnet-ef
   dotnet ef database update -p backend\StellarisCharts.Api\StellarisCharts.Api.csproj -s backend\StellarisCharts.Api\StellarisCharts.Api.csproj
   ```
4. Start the API:
   ```
   dotnet run --project backend\StellarisCharts.Api\StellarisCharts.Api.csproj
   ```

## Frontend setup
```
cd frontend
npm install
npm run dev
```

The frontend expects the API at `http://localhost:5000` (Vite proxy to `/api`).

## Uploading saves
- Place `.sav` files in `saves/` or use the UI scan/upload.
- Save filenames like `autosave_2327.07.01.sav` are parsed for the in-game date.
- Re-upload/rescan after schema changes to refresh derived fields (ethos, civics, traditions, federation, etc).

## Migrations
- New fields require migrations
- Run:
  ```
  dotnet ef database update -p backend\StellarisCharts.Api\StellarisCharts.Api.csproj -s backend\StellarisCharts.Api\StellarisCharts.Api.csproj
  ```

## Notes
- `saves/` and `appsettings.json` are ignored by git.
- If you use docker-compose, put DB creds in a local `.env` (see `.env.example`).
- If you change models, create and apply a new migration.

## Useful endpoints
- `POST /api/saves/upload`
- `DELETE /api/saves/clear`
- `GET /api/countries`
- `GET /api/countries/{id}/snapshots`
- `GET /api/snapshots/{id}/budget`
- `GET /api/snapshots/{id}/species`
- `GET /api/galaxy/summary`
- `GET /api/galaxy/species`
- `GET /api/galaxy/species/previous`
- `GET /api/galaxy/species/history`
