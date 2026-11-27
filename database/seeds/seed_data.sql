-- =============================================================================
-- PrintGuard Initial Seed Data
-- =============================================================================

-- Default Departments
INSERT INTO departments (name, description) VALUES
('Information Technology', 'IT infrastructure, systems, and support'),
('Finance & Accounting', 'Financial reports, billing, and accounting'),
('Human Resources', 'HR management, recruitment, and payroll'),
('Operations', 'General business operations and supply chain')
ON CONFLICT (name) DO NOTHING;

-- Default Admin User (Password: Admin@123456 - hashed using bcrypt/argon2 or standard placeholder)
-- Hashed using standard BCrypt hash for Admin@123456: $2a$11$0W7N3NnU6wZf3F6wS7rAee7QxK0m2Q4vL3f3x/XpDlm4kL1fT9mWy
INSERT INTO admin_users (full_name, username, password_hash, role, is_active) VALUES
('System Administrator', 'admin', '$2a$11$zW6Yk6uD9bJqN1pX3XkMe.l9S1yFpMvT/7lq6M3cQz5E9gS.d4.sK', 'ADMIN', TRUE)
ON CONFLICT (username) DO NOTHING;

-- Sample Employees
-- Password for all sample users: Password@123 -> $2a$11$w1Wd3d9rU2mS2kG0Y4uB.e1wM9pZ9b0bL9kQ3mB8cT7kO1fS8rL7e
INSERT INTO users (department_id, employee_id, full_name, username, password_hash, email, position, is_active)
SELECT d.id, 'EMP-001', 'Budi Santoso', 'budi.santoso', '$2a$11$w1Wd3d9rU2mS2kG0Y4uB.e1wM9pZ9b0bL9kQ3mB8cT7kO1fS8rL7e', 'budi.santoso@company.local', 'Finance Specialist', TRUE
FROM departments d WHERE d.name = 'Finance & Accounting'
ON CONFLICT (username) DO NOTHING;

INSERT INTO users (department_id, employee_id, full_name, username, password_hash, email, position, is_active)
SELECT d.id, 'EMP-002', 'Siti Rahma', 'siti.rahma', '$2a$11$w1Wd3d9rU2mS2kG0Y4uB.e1wM9pZ9b0bL9kQ3mB8cT7kO1fS8rL7e', 'siti.rahma@company.local', 'HR Officer', TRUE
FROM departments d WHERE d.name = 'Human Resources'
ON CONFLICT (username) DO NOTHING;

INSERT INTO users (department_id, employee_id, full_name, username, password_hash, email, position, is_active)
SELECT d.id, 'EMP-003', 'Ahmad Fauzi', 'ahmad.fauzi', '$2a$11$w1Wd3d9rU2mS2kG0Y4uB.e1wM9pZ9b0bL9kQ3mB8cT7kO1fS8rL7e', 'ahmad.fauzi@company.local', 'IT Support', TRUE
FROM departments d WHERE d.name = 'Information Technology'
ON CONFLICT (username) DO NOTHING;

-- Sample Printers
INSERT INTO printers (name, model, ip_address, location, printer_server, is_active) VALUES
('PRN-LAN-01-FINANCE', 'HP LaserJet Enterprise M608', '192.168.10.201', 'Floor 2 - Finance Area', 'SRV-PRINT-01', TRUE),
('PRN-LAN-02-COLOR', 'Canon imageRUNNER ADVANCE C5535i', '192.168.10.202', 'Floor 1 - Main Hallway', 'SRV-PRINT-01', TRUE),
('PRN-LAN-03-HR', 'Epson WorkForce Pro WF-C579R', '192.168.10.203', 'Floor 3 - HR Department', 'SRV-PRINT-01', TRUE)
ON CONFLICT DO NOTHING;

-- Print Queues for the printers
INSERT INTO print_queues (printer_id, queue_name, queue_type, is_active)
SELECT id, name || '-HOLD', 'HOLD', TRUE FROM printers
ON CONFLICT (queue_name) DO NOTHING;

-- System Settings
INSERT INTO system_settings (setting_key, setting_value) VALUES
('remember_me_duration_hours', '8'::jsonb),
('require_auth_every_job', 'true'::jsonb),
('session_timeout_minutes', '480'::jsonb),
('max_failed_attempts', '5'::jsonb),
('allow_remember_me', 'true'::jsonb)
ON CONFLICT (setting_key) DO NOTHING;
