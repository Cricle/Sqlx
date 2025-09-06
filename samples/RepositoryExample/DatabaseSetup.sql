-- ============================================================================
-- Sqlx Repository Pattern Database Setup Script
-- ============================================================================

-- Create database if it doesn't exist
USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'SqlxRepositoryDemo')
BEGIN
    CREATE DATABASE SqlxRepositoryDemo;
END
GO

USE SqlxRepositoryDemo;
GO

-- Create users table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[users]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[users] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [Name] nvarchar(255) NOT NULL,
        [Email] nvarchar(255) NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL,
        CONSTRAINT [PK_users] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

-- Insert sample data
IF NOT EXISTS (SELECT * FROM [dbo].[users])
BEGIN
    INSERT INTO [dbo].[users] ([Name], [Email], [CreatedAt]) VALUES 
    (N'John Doe', N'john@example.com', GETUTCDATE()),
    (N'Jane Smith', N'jane@example.com', GETUTCDATE()),
    (N'Bob Johnson', N'bob@example.com', GETUTCDATE()),
    (N'Alice Brown', N'alice@example.com', GETUTCDATE()),
    (N'Charlie Wilson', N'charlie@example.com', GETUTCDATE());
END
GO

-- Create indexes for better performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_users_Email' AND object_id = OBJECT_ID('users'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_users_Email ON [dbo].[users] ([Email]);
END
GO

-- Display setup completion message
PRINT 'Sqlx Repository Demo database setup completed successfully!';
PRINT 'Tables created: users';
PRINT 'Sample data inserted: 5 users';
PRINT 'Database is ready for repository pattern testing.';
GO
