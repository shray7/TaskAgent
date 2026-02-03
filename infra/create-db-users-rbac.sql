-- =============================================================================
-- RBAC: Create database users for managed identities (OBJECT_ID-based)
-- =============================================================================
-- Run this as the SQL Entra Admin (SQL-Entra-Admin, Object ID: ed40cbe5-d7d9-4bdc-8eb3-89396ac1d74f)
-- Options: Azure Portal > SQL Database > Query editor (use Entra auth), or sqlcmd -G
-- Server: taskagent-sql-eus2.database.windows.net | Database: taskagent
-- =============================================================================

USE [taskagent];
GO

-- -----------------------------------------------------------------------------
-- 1. taskagent-api (production Container App)
--    Principal ID: 0b23586d-c319-4353-872d-e8f06b314bb6
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'taskagent-api')
BEGIN
  CREATE USER [taskagent-api] FROM EXTERNAL PROVIDER WITH OBJECT_ID = '0b23586d-c319-4353-872d-e8f06b314bb6';
  PRINT 'Created user: taskagent-api';
END
ELSE
  PRINT 'User taskagent-api already exists';

IF IS_ROLEMEMBER('db_datareader', 'taskagent-api') = 0
  ALTER ROLE db_datareader ADD MEMBER [taskagent-api];
IF IS_ROLEMEMBER('db_datawriter', 'taskagent-api') = 0
  ALTER ROLE db_datawriter ADD MEMBER [taskagent-api];
IF IS_ROLEMEMBER('db_ddladmin', 'taskagent-api') = 0
  ALTER ROLE db_ddladmin ADD MEMBER [taskagent-api];

-- -----------------------------------------------------------------------------
-- 2. taskagent-api-staging (staging Container App)
--    Principal ID: 91e66909-fd7f-4016-8259-b1dda4983412
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'taskagent-api-staging')
BEGIN
  CREATE USER [taskagent-api-staging] FROM EXTERNAL PROVIDER WITH OBJECT_ID = '91e66909-fd7f-4016-8259-b1dda4983412';
  PRINT 'Created user: taskagent-api-staging';
END
ELSE
  PRINT 'User taskagent-api-staging already exists';

IF IS_ROLEMEMBER('db_datareader', 'taskagent-api-staging') = 0
  ALTER ROLE db_datareader ADD MEMBER [taskagent-api-staging];
IF IS_ROLEMEMBER('db_datawriter', 'taskagent-api-staging') = 0
  ALTER ROLE db_datawriter ADD MEMBER [taskagent-api-staging];
IF IS_ROLEMEMBER('db_ddladmin', 'taskagent-api-staging') = 0
  ALTER ROLE db_ddladmin ADD MEMBER [taskagent-api-staging];

-- -----------------------------------------------------------------------------
-- 3. taskagent-github-actions (GitHub Actions service principal for EF migrations)
--    Principal ID: 6968e74f-4ea9-4d5c-9955-9cc1e4423dad
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'taskagent-github-actions')
BEGIN
  CREATE USER [taskagent-github-actions] FROM EXTERNAL PROVIDER WITH OBJECT_ID = '6968e74f-4ea9-4d5c-9955-9cc1e4423dad';
  PRINT 'Created user: taskagent-github-actions';
END
ELSE
  PRINT 'User taskagent-github-actions already exists';

IF IS_ROLEMEMBER('db_datareader', 'taskagent-github-actions') = 0
  ALTER ROLE db_datareader ADD MEMBER [taskagent-github-actions];
IF IS_ROLEMEMBER('db_datawriter', 'taskagent-github-actions') = 0
  ALTER ROLE db_datawriter ADD MEMBER [taskagent-github-actions];
IF IS_ROLEMEMBER('db_ddladmin', 'taskagent-github-actions') = 0
  ALTER ROLE db_ddladmin ADD MEMBER [taskagent-github-actions];

-- -----------------------------------------------------------------------------
PRINT 'RBAC database users configured successfully.';
GO
