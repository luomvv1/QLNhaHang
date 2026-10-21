# QL Nha Hang

Restaurant management system with a .NET 8 API, Vue web client, Flutter mobile client, SQL scripts, and tests.

## Local setup

1. Copy `backend/Restaurant.Api/appsettings.example.json` to `appsettings.json` and set the database connection string and a private JWT key.
2. If payment configuration is needed, copy `backend/Restaurant.Api/bank_config.example.json` to `bank_config.json` and fill in your provider credentials.
3. Open `backend/Restaurant.Api/Restaurant.Api.sln` in Visual Studio or run the API project with the .NET 8 SDK.
4. Install the web dependencies from `fontend/web` and start it with `npm run dev`.

Local configuration files and build output are excluded from Git.
