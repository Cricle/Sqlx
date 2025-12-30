-- SQL Server 初始化脚本
-- 创建测试表

-- 删除已存在的表（旧测试）
IF OBJECT_ID('dialect_users_sqlserver', 'U') IS NOT NULL
    DROP TABLE dialect_users_sqlserver;
GO

-- 删除统一方言测试表（UnifiedDialect测试）
IF OBJECT_ID('unified_dialect_users_ss', 'U') IS NOT NULL
    DROP TABLE unified_dialect_users_ss;
GO

-- 创建用户表
CREATE TABLE dialect_users_sqlserver (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    username NVARCHAR(100) NOT NULL,
    email NVARCHAR(255),
    age INT NOT NULL,
    balance DECIMAL(18, 2) NOT NULL DEFAULT 0,
    is_active BIT NOT NULL DEFAULT 1,
    created_at DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

-- 创建索引
CREATE INDEX idx_dialect_users_sqlserver_username ON dialect_users_sqlserver(username);
CREATE INDEX idx_dialect_users_sqlserver_age ON dialect_users_sqlserver(age);
CREATE INDEX idx_dialect_users_sqlserver_balance ON dialect_users_sqlserver(balance);
GO

