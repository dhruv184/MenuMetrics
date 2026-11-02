-- Reference SQL Server schema. EF Core EnsureCreated() at runtime will create
-- equivalent tables for the chosen provider (SQLite by default, SQL Server when configured).
CREATE TABLE Businesses (Id INT IDENTITY PRIMARY KEY, Name NVARCHAR(200), Timezone NVARCHAR(64), Plan NVARCHAR(32), CreatedAt DATETIME2);
CREATE TABLE Locations  (Id INT IDENTITY PRIMARY KEY, BusinessId INT, Name NVARCHAR(200), Address NVARCHAR(400));
CREATE TABLE MenuItems  (Id INT IDENTITY PRIMARY KEY, BusinessId INT, Name NVARCHAR(200), Category NVARCHAR(80), Price DECIMAL(10,2), Cost DECIMAL(10,2), Active BIT);
CREATE TABLE Transactions(Id BIGINT IDENTITY PRIMARY KEY, BusinessId INT, LocationId INT, MenuItemId INT, OccurredAt DATETIME2, Quantity INT, LineTotal DECIMAL(12,2), Source NVARCHAR(32));
CREATE INDEX IX_Transactions_Business_Time ON Transactions(BusinessId, OccurredAt);
CREATE TABLE Promotions(Id INT IDENTITY PRIMARY KEY, BusinessId INT, Name NVARCHAR(200), StartDate DATETIME2, EndDate DATETIME2, DiscountPct DECIMAL(5,2), PreRevenue DECIMAL(12,2), DuringRevenue DECIMAL(12,2), PostRevenue DECIMAL(12,2), Roi DECIMAL(8,2), Rating NVARCHAR(32));
CREATE TABLE Forecasts(Id INT IDENTITY PRIMARY KEY, BusinessId INT, MenuItemId INT NULL, ForDate DATETIME2, PredictedOrders DECIMAL(12,2), BaseDemand DECIMAL(12,2), SeasonalityFactor DECIMAL(8,4), TrendFactor DECIMAL(8,4), Lower95 DECIMAL(12,2), Upper95 DECIMAL(12,2), Model NVARCHAR(32), CreatedAt DATETIME2);
CREATE TABLE Recommendations(Id INT IDENTITY PRIMARY KEY, BusinessId INT, Title NVARCHAR(300), Category NVARCHAR(32), Detail NVARCHAR(1000), ImpactScore DECIMAL(5,2), EstimatedRevenueLift DECIMAL(12,2), CreatedAt DATETIME2, Status NVARCHAR(32));
CREATE TABLE InventoryItems(Id INT IDENTITY PRIMARY KEY, BusinessId INT, Sku NVARCHAR(64), Name NVARCHAR(200), OnHand DECIMAL(12,3), ReorderPoint DECIMAL(12,3), Unit NVARCHAR(16));
