# GameStop Inventory Management System

A cloud-native, real-time inventory management system built with ASP.NET Core MVC and Microsoft Azure. Developed as part of the CLDV7112w Cloud Development module at The Independent Institute of Education.

---

## Project Overview

The system allows GameStop to manage their online game inventory with full CRUD functionality, user authentication, cloud storage for game images, audit logging, and a serverless data pipeline using Azure Functions.

---

## Tech Stack

- ASP.NET Core MVC (.NET 8)
- Entity Framework Core 8
- Microsoft Azure (App Service, SQL Database, Blob Storage, Table Storage, Storage Queues, Functions)
- Azure Functions (.NET 8 Isolated Worker)

---

## Azure Services Used

| Service | Purpose |
|---|---|
| Azure App Service | Hosts the MVC web application |
| Azure SQL Database | Stores users and game inventory |
| Azure Blob Storage | Stores game cover images |
| Azure Table Storage | Stores audit logs |
| Azure Storage Queue | Queues order messages for processing |
| Azure Function App | Runs the 4 serverless functions |

---

## Prerequisites

- Visual Studio 2022
- .NET 8 SDK
- Azure account with active subscription
- Azure Functions Core Tools v4
- Node.js (for Azure Functions Core Tools)
- SQL Server Management Studio or Azure Portal Query Editor (optional)

---

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/T3RRY-J4Y/GameStopInventory.git
cd GameStopInventory
```

### 2. Set up Azure resources

Create the following in the Azure Portal:

- Resource Group
- Azure SQL Server and Database (GameStopDB)
- Azure Storage Account with:
  - Blob container named `game-images` (set to Blob anonymous access)
  - Table named `AuditLogs`
  - Queue named `orders-queue`
- Azure App Service (.NET 8, Windows)

### 3. Configure connection strings

Create an `appsettings.json` file in the `GameStopInventory` project root with the following structure:

```json
{
  "ConnectionStrings": {
    "SqlConnection": "YOUR_AZURE_SQL_CONNECTION_STRING",
    "StorageConnection": "YOUR_AZURE_STORAGE_CONNECTION_STRING"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

Replace the placeholder values with your actual Azure connection strings from the Azure Portal.

### 4. Apply database migrations

Open the Package Manager Console in Visual Studio, set the default project to `GameStopInventory`, and run:

```powershell
Update-Database
```

This creates the Users and Games tables in your Azure SQL Database.

### 5. Run the MVC application

Press `F5` in Visual Studio or run:

```bash
dotnet run --project GameStopInventory
```

The application will open in your browser. An admin account is seeded automatically on first run:

- Username: `admin`
- Password: `admin123`

---

## Running the Azure Functions

### 1. Configure local settings

Create a `local.settings.json` file in the `GameStopFunctions` folder:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "YOUR_STORAGE_CONNECTION_STRING",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "SqlConnection": "YOUR_AZURE_SQL_CONNECTION_STRING",
    "StorageConnection": "YOUR_STORAGE_CONNECTION_STRING"
  }
}
```

### 2. Start the functions

```bash
cd GameStopFunctions
func start
```

The following functions will be available:

| Function | Trigger | Endpoint |
|---|---|---|
| BlobTriggerFunction | Blob upload to game-images | Automatic |
| GetGameStock | HTTP GET | http://localhost:7071/api/games/{id}/stock |
| PlaceOrder | HTTP POST | http://localhost:7071/api/orders |
| ProcessOrder | Queue message in orders-queue | Automatic |

---

## Function Details

### BlobTriggerFunction
Fires automatically when a new image is uploaded to the `game-images` Blob container. Writes an audit log entry to Azure Table Storage with the file name, size, and timestamp.

### GetGameStock (HTTP GET)
Fetches real-time stock levels for a specific game directly from Azure SQL Database.

Example request:
```
GET http://localhost:7071/api/games/3/stock
```

Example response:
```json
{"Id": 3, "Title": "GTA6", "StockLevel": 63}
```

### PlaceOrder (HTTP POST)
Accepts an order request and places a message into the `orders-queue` Azure Storage Queue for asynchronous processing.

Example request:
```
POST http://localhost:7071/api/orders
Content-Type: application/json

{"GameId": 3, "Quantity": 1, "Username": "admin"}
```

Example response:
```json
{"message": "Order placed successfully"}
```

### ProcessOrder (Queue Trigger)
Listens to the `orders-queue` and processes each order by reducing the game's stock level in Azure SQL Database.

---

## Deploying to Azure

### Deploy MVC Application
1. Right-click `GameStopInventory` in Visual Studio
2. Select Publish
3. Choose Azure App Service and select your existing App Service
4. Click Publish

After deploying, add the connection strings as environment variables in the App Service configuration under Settings > Environment variables:

```
ConnectionStrings__SqlConnection = YOUR_SQL_CONNECTION_STRING
ConnectionStrings__StorageConnection = YOUR_STORAGE_CONNECTION_STRING
```

### Deploy Azure Functions
1. Right-click `GameStopFunctions` in Visual Studio
2. Select Publish
3. Choose Azure Function App and select your existing Function App
4. Click Publish

After deploying, add the environment variables in the Function App configuration:

```
SqlConnection = YOUR_SQL_CONNECTION_STRING
StorageConnection = YOUR_STORAGE_CONNECTION_STRING
```

---

## Project Structure

```
GameStopInventory/
    Controllers/
        AccountController.cs       -- Login, register, logout
        CartController.cs          -- Shopping cart and checkout
        GamesController.cs         -- Game CRUD with Blob and Table Storage
        HomeController.cs          -- Landing page
    Models/
        AppDbContext.cs             -- Entity Framework DbContext
        AuditLog.cs                -- Azure Table Storage entity
        CartItem.cs                -- Shopping cart item model
        Game.cs                    -- Game inventory model
        User.cs                    -- User account model
    Views/
        Account/                   -- Login and Register views
        Cart/                      -- Cart and Success views
        Games/                     -- Index, Create, Edit, Delete views
        Home/                      -- Landing page
        Shared/                    -- Layout and shared partials
    appsettings.json               -- Connection strings (not committed)
    Program.cs                     -- App configuration and service registration

GameStopFunctions/
    BlobTriggerFunction.cs         -- Fires on image upload
    HttpGetFunction.cs             -- GET stock levels
    HttpPostFunction.cs            -- POST order to queue
    QueueTriggerFunction.cs        -- Process order from queue
    local.settings.json            -- Local config (not committed)
```

---

## Security Notes

- The `appsettings.json` and `local.settings.json` files are excluded from version control as they contain sensitive connection strings.
- All connection strings should be stored as environment variables in production.
- The Azure Storage Account key should be regenerated after any accidental exposure.

---

## Module Information

- Module: CLDV7112w — Cloud Development
- Assessment: Summative Practicum (a) — 2025 Mid-year
- Institution: The Independent Institute of Education
- Student Number: ST10202185
