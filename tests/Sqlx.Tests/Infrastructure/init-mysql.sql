-- MySQL 初始化脚本
-- 创建测试表

-- 删除已存在的表
DROP TABLE IF EXISTS dialect_users_mysql;

-- 创建用户表
CREATE TABLE dialect_users_mysql (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(100) NOT NULL,
    email VARCHAR(255),
    age INT NOT NULL,
    balance DECIMAL(18, 2) NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_login_at TIMESTAMP NULL,
    INDEX idx_dialect_users_mysql_username (username),
    INDEX idx_dialect_users_mysql_age (age),
    INDEX idx_dialect_users_mysql_balance (balance)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

