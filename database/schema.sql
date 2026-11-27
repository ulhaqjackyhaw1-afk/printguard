-- =============================================================================
-- PrintGuard Database Schema (PostgreSQL)
-- Version: 1.0 (MVP)
-- Reference: PRD Section 7
-- =============================================================================

CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- 1. Departments Table
CREATE TABLE IF NOT EXISTS departments (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name VARCHAR(120) NOT NULL UNIQUE,
    description TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 2. Users (Employees) Table
CREATE TABLE IF NOT EXISTS users (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    department_id BIGINT REFERENCES departments(id) ON DELETE SET NULL,
    employee_id VARCHAR(50) UNIQUE,
    full_name VARCHAR(150) NOT NULL,
    username VARCHAR(80) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    email VARCHAR(150),
    position VARCHAR(100),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 3. Admin Users Table
CREATE TABLE IF NOT EXISTS admin_users (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    full_name VARCHAR(150) NOT NULL,
    username VARCHAR(80) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    role VARCHAR(30) NOT NULL DEFAULT 'ADMIN',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 4. Computers Table
CREATE TABLE IF NOT EXISTS computers (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    hostname VARCHAR(120) NOT NULL UNIQUE,
    ip_address INET,
    windows_version VARCHAR(100),
    agent_version VARCHAR(30),
    last_seen_at TIMESTAMPTZ,
    status VARCHAR(20) NOT NULL DEFAULT 'OFFLINE',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 5. Client Devices Table
CREATE TABLE IF NOT EXISTS client_devices (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    computer_id BIGINT NOT NULL REFERENCES computers(id) ON DELETE CASCADE,
    device_token_hash TEXT NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    registered_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    revoked_at TIMESTAMPTZ
);

-- 6. Printers Table
CREATE TABLE IF NOT EXISTS printers (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name VARCHAR(150) NOT NULL,
    model VARCHAR(150),
    ip_address INET,
    location VARCHAR(150),
    printer_server VARCHAR(150),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 7. Print Queues Table
CREATE TABLE IF NOT EXISTS print_queues (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    printer_id BIGINT NOT NULL REFERENCES printers(id) ON DELETE CASCADE,
    queue_name VARCHAR(150) NOT NULL UNIQUE,
    queue_type VARCHAR(30) NOT NULL, -- e.g., 'HOLD', 'RELEASE', 'DIRECT'
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 8. User Sessions Table (Remember Me & Active Sessions)
CREATE TABLE IF NOT EXISTS user_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id BIGINT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    computer_id BIGINT NOT NULL REFERENCES computers(id) ON DELETE CASCADE,
    token_hash TEXT NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    revoked_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 9. Print Jobs Table
CREATE TABLE IF NOT EXISTS print_jobs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    server_job_id VARCHAR(100),
    user_id BIGINT REFERENCES users(id) ON DELETE SET NULL,
    computer_id BIGINT REFERENCES computers(id) ON DELETE SET NULL,
    printer_id BIGINT NOT NULL REFERENCES printers(id) ON DELETE RESTRICT,
    document_name VARCHAR(255),
    total_pages INTEGER DEFAULT 1,
    copies INTEGER DEFAULT 1,
    color_mode VARCHAR(20) DEFAULT 'MONO', -- 'COLOR' or 'MONO'
    status VARCHAR(30) NOT NULL DEFAULT 'RECEIVED', -- RECEIVED, HELD, AUTHENTICATING, AUTHORIZED, RELEASING, PRINTING, COMPLETED, DENIED, CANCELLED, FAILED
    submitted_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    authorized_at TIMESTAMPTZ,
    completed_at TIMESTAMPTZ,
    error_message TEXT
);

-- 10. Print Authorizations Table
CREATE TABLE IF NOT EXISTS print_authorizations (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    print_job_id UUID NOT NULL REFERENCES print_jobs(id) ON DELETE CASCADE,
    user_id BIGINT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    computer_id BIGINT NOT NULL REFERENCES computers(id) ON DELETE CASCADE,
    result VARCHAR(30) NOT NULL, -- 'SUCCESS', 'DENIED', 'EXPIRED'
    auth_method VARCHAR(30) NOT NULL, -- 'PASSWORD', 'REMEMBER_ME'
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 11. Audit Logs Table
CREATE TABLE IF NOT EXISTS audit_logs (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    admin_user_id BIGINT REFERENCES admin_users(id) ON DELETE SET NULL,
    user_id BIGINT REFERENCES users(id) ON DELETE SET NULL,
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(80),
    entity_id VARCHAR(100),
    description TEXT,
    ip_address INET,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 12. System Settings Table
CREATE TABLE IF NOT EXISTS system_settings (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    setting_key VARCHAR(120) NOT NULL UNIQUE,
    setting_value JSONB NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 13. Print Job Events Table
CREATE TABLE IF NOT EXISTS print_job_events (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    print_job_id UUID NOT NULL REFERENCES print_jobs(id) ON DELETE CASCADE,
    previous_status VARCHAR(30),
    new_status VARCHAR(30) NOT NULL,
    details TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 14. Printer Status Logs Table
CREATE TABLE IF NOT EXISTS printer_status_logs (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    printer_id BIGINT NOT NULL REFERENCES printers(id) ON DELETE CASCADE,
    status VARCHAR(30) NOT NULL,
    details TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_print_jobs_user ON print_jobs(user_id);
CREATE INDEX IF NOT EXISTS idx_print_jobs_printer ON print_jobs(printer_id);
CREATE INDEX IF NOT EXISTS idx_print_jobs_submitted ON print_jobs(submitted_at DESC);
CREATE INDEX IF NOT EXISTS idx_print_jobs_status ON print_jobs(status);
CREATE INDEX IF NOT EXISTS idx_sessions_user ON user_sessions(user_id);
CREATE INDEX IF NOT EXISTS idx_authorizations_job ON print_authorizations(print_job_id);
CREATE INDEX IF NOT EXISTS idx_computers_hostname ON computers(hostname);
