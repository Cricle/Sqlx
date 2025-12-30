-- PostgreSQL 初始化脚本
-- 创建测试表

-- 删除已存在的表（旧测试）
DROP TABLE IF EXISTS dialect_users_postgresql;

-- 删除统一方言测试表（UnifiedDialect测试）
DROP TABLE IF EXISTS unified_dialect_users_pg;

-- 创建用户表
CREATE TABLE dialect_users_postgresql (
    id BIGSERIAL PRIMARY KEY,
    username VARCHAR(100) NOT NULL,
    email VARCHAR(255),
    age INT NOT NULL,
    balance DECIMAL(18, 2) NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_login_at TIMESTAMP NULL
);

-- 创建索引
CREATE INDEX idx_dialect_users_postgresql_username ON dialect_users_postgresql(username);
CREATE INDEX idx_dialect_users_postgresql_age ON dialect_users_postgresql(age);
CREATE INDEX idx_dialect_users_postgresql_balance ON dialect_users_postgresql(balance);

