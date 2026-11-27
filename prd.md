PRD – PrintGuard
Print Management, Employee Authentication & Monitoring System

Version: 1.0
Status: Final Specification – MVP
Deployment: Centralized LAN Server
Platform: Windows Desktop + Web Dashboard
Database: PostgreSQL

1. Project Overview

PrintGuard adalah aplikasi manajemen printer perusahaan yang digunakan untuk mengontrol, mengautentikasi, dan memonitor aktivitas printing karyawan.

Sistem terdiri dari tiga komponen utama:

PrintGuard Windows Client: Aplikasi EXE yang di-install pada PC karyawan.
PrintGuard Central Server: Server pusat yang menangani autentikasi, print queue, dan database.
PrintGuard Admin Dashboard: Web application untuk Admin IT.
1.1 Business Objectives
Memastikan setiap aktivitas printing memiliki identitas karyawan.
Mengharuskan autentikasi sebelum dokumen dicetak.
Memonitor printer yang digunakan.
Mencatat jumlah halaman dan mode warna.
Menyediakan laporan aktivitas printing.
Mengontrol akses printing dari satu server pusat.
2. Final System Configuration
Komponen	Teknologi / Konfigurasi
Server	Local LAN Server 
Architecture	Centralized
Database	PostgreSQL
Backend	ASP.NET Core Web API
Windows Service	C# .NET Worker Service
Windows GUI	C# WPF
Admin Dashboard	React / Next.js
Print Monitoring	Windows Print Spooler API
Communication	HTTPS REST API
Printer	Network Printer
Authentication	Every Print Job
Remember Me	Optional
Unauthenticated Job	HOLD
Installer	Windows EXE / MSI
3. System Architecture
3.1 Architecture Overview
text
                  ADMIN IT
                     |
                     v
             ADMIN DASHBOARD
                     |
                     v
                 API SERVER
                     |
              POSTGRESQL DB
                     |
                     |
              CENTRAL SERVER
                     |
             PRINT QUEUE MANAGER
                     |
             +-------+-------+
             |               |
         HOLD QUEUE      RELEASE QUEUE
             |               |
             v               v
        AUTHORIZATION    NETWORK PRINTER
             ^
             |
       WINDOWS CLIENT
             |
       PRINTGUARD AGENT
             |
       WINDOWS GUI POPUP
             |
       EMPLOYEE LOGIN

3.2 Main Components
A. PrintGuard Windows Client

Terdiri dari:

PrintGuard.Service.exe
PrintGuard.UI.exe

Responsibilities:

Background service.
Server communication.
Print job monitoring.
User authentication.
Remember Me session.
Desktop notifications.
Client registration.
Device identification.
B. Central Print Server

Responsibilities:

Manage Windows Print Spooler.
Receive print jobs.
Hold unauthenticated jobs.
Validate authorization.
Release authorized jobs.
Monitor print status.
Forward printing information to backend.
C. Backend API

Responsibilities:

Employee authentication.
Session management.
Device management.
Print job management.
Authorization.
Reporting.
Database operations.
D. Admin Dashboard

Responsibilities:

Employee management.
Printer management.
Computer management.
Print history.
Reports and analytics.
Audit logs.
System settings.
4. Print Workflow
4.1 Main Workflow
Employee opens a document.
Employee selects a registered company printer.
Employee clicks Print.
Print job enters HOLD_QUEUE.
PrintGuard detects the new job.
Windows GUI popup appears.
Employee enters username and password.
Backend validates credentials.
Server checks user status and device identity.
Authorization is granted.
Job moves to RELEASE_QUEUE.
Document is sent to the network printer.
Printing status is recorded.
Print activity is saved to PostgreSQL.
4.2 Job Status Lifecycle
Status	Description
RECEIVED	Job received
HELD	Waiting for authentication
AUTHENTICATING	Validating employee
AUTHORIZED	Authorization granted
RELEASING	Sending job to printer
PRINTING	Printing in progress
COMPLETED	Processing completed
DENIED	Authentication denied
CANCELLED	Job cancelled
FAILED	Printing failed
4.3 Remember Me

Behavior:

OFF: Employee must enter credentials for every print job.
ON: Existing authenticated session can be reused.
EXPIRED: Employee must log in again.
DISABLED USER: Printing authorization is denied.
REVOKED SESSION: Login is required again.
DIFFERENT WINDOWS USER: Existing session cannot be reused.

Remember Me must use encrypted device-bound credentials or secure session tokens. Passwords must never be stored in plaintext.

5. Windows GUI Requirements
5.1 Login Popup

Application name: PrintGuard

Recommended design:

Modern Windows desktop interface.
White background.
Primary blue color: #2563EB.
Font: Segoe UI / Inter.
Rounded cards.
Minimal layout.
Center-screen popup.

Components:

PrintGuard Logo.
Print Authorization title.
Username input.
Password input.
Show/Hide Password.
Remember Me checkbox.
Login & Print button.
Printer information.
Document information.
Connection status.
Error notification.
5.2 Popup Behavior
Condition	Behavior
First print	Show login
Wrong password	Show error
User inactive	Deny access
Server offline	Keep job held
Remember Me active	Reuse valid session
Authentication success	Authorize job
Job cancelled	Update status
Session expired	Request login
5.3 Printer Information

The popup should display:

Printer Name.
Document Name.
Computer Name.
Job ID.
Print Status.
5.4 System Tray

Menu:

Open PrintGuard.
Connection Status.
My Print History.
Account Information.
Settings.
Logout.
Exit (subject to IT policy).
6. Admin Dashboard Requirements
6.1 Dashboard Overview

Summary cards:

Total Employees.
Total Printers.
Online Computers.
Total Print Jobs.
Total Printed Pages.
Color Pages.
Black & White Pages.

Charts:

Daily Printing Activity.
Monthly Printing Activity.
Color vs Monochrome.
Top 10 Employees.
Most Used Printers.
Printing by Department.
6.2 Employee Management

Features:

Add employee.
Edit employee.
Delete or deactivate employee.
Reset password.
Search employee.
Filter by department.
View employee print history.

Employee fields:

Field	Description
Employee ID	Employee identifier
Full Name	Employee name
Username	Login username
Password	Hashed password
Email	Employee email
Department	Department
Position	Job position
Status	Active / Inactive
6.3 Printer Management

Features:

Add network printer.
Edit printer.
Delete or deactivate printer.
Test connection.
View printer status.
View printer usage history.

Printer fields:

Printer Name.
Printer Model.
IP Address.
Location.
Print Server.
Queue Name.
Status.
Total Jobs.
Total Pages.
6.4 Computer Management

Features:

View registered computers.
Device registration.
Agent version.
Online / Offline status.
Last heartbeat.
Remote revoke.
Client update information.
6.5 Print History

Table columns:

Job ID.
Date.
Employee.
Department.
Computer.
Document.
Printer.
Pages.
Copies.
Color Mode.
Status.

Filters:

Date Range.
Employee.
Department.
Printer.
Computer.
Status.
Color Mode.

Export formats:

Excel.
CSV.
6.6 Reports & Analytics
Daily print report.
Monthly print report.
Employee usage report.
Department usage report.
Printer usage report.
Color printing report.
Monochrome printing report.
7. Database Design

Database name: printguard_db

Database tables:

Table	Purpose
departments	Department information
users	Employee accounts
admin_users	Admin accounts
computers	Registered PCs
client_devices	Client device credentials
printers	Printer information
print_queues	Print queue configuration
user_sessions	Authentication sessions
print_jobs	Printing records
print_authorizations	Authorization history
audit_logs	Administrative audit logs
system_settings	System configuration
print_job_events	Job status history
printer_status_logs	Printer monitoring history
7.1 Entity Relationships
text
DEPARTMENTS
    |
    +---- USERS
             |
             +---- USER_SESSIONS
             |
             +---- PRINT_JOBS
                       |
                       +---- PRINT_AUTHORIZATIONS
                       |
                       +---- PRINTERS
                       |
                       +---- COMPUTERS

ADMIN_USERS
    |
    +---- AUDIT_LOGS

COMPUTERS
    |
    +---- CLIENT_DEVICES

PRINTERS
    |
    +---- PRINT_QUEUES

7.2 PostgreSQL Schema
sql
CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE departments (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name VARCHAR(120) NOT NULL UNIQUE,
    description TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE users (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    department_id BIGINT REFERENCES departments(id),
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

CREATE TABLE admin_users (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    full_name VARCHAR(150) NOT NULL,
    username VARCHAR(80) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    role VARCHAR(30) NOT NULL DEFAULT 'ADMIN',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE computers (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    hostname VARCHAR(120) NOT NULL UNIQUE,
    ip_address INET,
    windows_version VARCHAR(100),
    agent_version VARCHAR(30),
    last_seen_at TIMESTAMPTZ,
    status VARCHAR(20) NOT NULL DEFAULT 'OFFLINE',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE client_devices (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    computer_id BIGINT NOT NULL REFERENCES computers(id),
    device_token_hash TEXT NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    registered_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    revoked_at TIMESTAMPTZ
);

CREATE TABLE printers (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name VARCHAR(150) NOT NULL,
    model VARCHAR(150),
    ip_address INET,
    location VARCHAR(150),
    printer_server VARCHAR(150),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE print_queues (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    printer_id BIGINT NOT NULL REFERENCES printers(id),
    queue_name VARCHAR(150) NOT NULL UNIQUE,
    queue_type VARCHAR(30) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE user_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id BIGINT NOT NULL REFERENCES users(id),
    computer_id BIGINT NOT NULL REFERENCES computers(id),
    token_hash TEXT NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    revoked_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE print_jobs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    server_job_id VARCHAR(100),
    user_id BIGINT REFERENCES users(id),
    computer_id BIGINT REFERENCES computers(id),
    printer_id BIGINT NOT NULL REFERENCES printers(id),
    document_name VARCHAR(255),
    total_pages INTEGER,
    copies INTEGER DEFAULT 1,
    color_mode VARCHAR(20),
    status VARCHAR(30) NOT NULL DEFAULT 'RECEIVED',
    submitted_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    authorized_at TIMESTAMPTZ,
    completed_at TIMESTAMPTZ,
    error_message TEXT
);

CREATE TABLE print_authorizations (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    print_job_id UUID NOT NULL REFERENCES print_jobs(id),
    user_id BIGINT NOT NULL REFERENCES users(id),
    computer_id BIGINT NOT NULL REFERENCES computers(id),
    result VARCHAR(30) NOT NULL,
    auth_method VARCHAR(30) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE audit_logs (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    admin_user_id BIGINT REFERENCES admin_users(id),
    user_id BIGINT REFERENCES users(id),
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(80),
    entity_id VARCHAR(100),
    description TEXT,
    ip_address INET,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE system_settings (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    setting_key VARCHAR(120) NOT NULL UNIQUE,
    setting_value JSONB NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE print_job_events (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    print_job_id UUID NOT NULL REFERENCES print_jobs(id),
    previous_status VARCHAR(30),
    new_status VARCHAR(30) NOT NULL,
    details TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE printer_status_logs (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    printer_id BIGINT NOT NULL REFERENCES printers(id),
    status VARCHAR(30) NOT NULL,
    details TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_print_jobs_user
ON print_jobs(user_id);

CREATE INDEX idx_print_jobs_printer
ON print_jobs(printer_id);

CREATE INDEX idx_print_jobs_submitted
ON print_jobs(submitted_at DESC);

CREATE INDEX idx_print_jobs_status
ON print_jobs(status);

CREATE INDEX idx_sessions_user
ON user_sessions(user_id);

CREATE INDEX idx_authorizations_job
ON print_authorizations(print_job_id);

8. API Specification

Base URL:

text
https://printguard-server.local/api/v1


All communication must use HTTPS.

8.1 Authentication API
Method	Endpoint	Purpose
POST	/auth/login	Employee login
POST	/auth/refresh	Refresh session
POST	/auth/logout	Logout
GET	/auth/me	Current user

Example login:

json
{
  "username": "budi.santoso",
  "password": "user-password",
  "computer_id": 12,
  "remember_me": true
}

8.2 Device API
Method	Endpoint	Purpose
POST	/devices/register	Register computer
POST	/devices/heartbeat	Update heartbeat
GET	/devices/config	Get configuration
POST	/devices/revoke	Revoke device
8.3 Print Job API
Method	Endpoint	Purpose
POST	/print-jobs	Register print job
GET	/print-jobs/{id}	Get job details
POST	/print-jobs/{id}/authorize	Authorize job
POST	/print-jobs/{id}/release	Release job
POST	/print-jobs/{id}/cancel	Cancel job
POST	/print-jobs/{id}/complete	Complete job
POST	/print-jobs/{id}/fail	Report failure

Example print job:

json
{
  "server_job_id": "JOB-2026-00001",
  "computer_id": 12,
  "printer_id": 4,
  "document_name": "Laporan Keuangan.pdf",
  "copies": 1
}

8.4 Admin API
Method	Endpoint	Purpose
POST	/admin/login	Admin login
GET	/admin/dashboard	Dashboard
GET	/admin/users	Employee list
POST	/admin/users	Create employee
PUT	/admin/users/{id}	Update employee
PATCH	/admin/users/{id}/status	Employee status
POST	/admin/users/{id}/reset-password	Reset password
GET	/admin/printers	Printer list
POST	/admin/printers	Create printer
PUT	/admin/printers/{id}	Update printer
GET	/admin/computers	Computer list
GET	/admin/print-jobs	Print history
GET	/admin/reports	Reports
GET	/admin/audit-logs	Audit history
8.5 Standard Response
json
{
  "success": true,
  "message": "Request successful",
  "data": {},
  "errors": null,
  "timestamp": "2026-11-27T14:30:00+07:00"
}

8.6 HTTP Status Codes
Code	Meaning
200	Success
201	Created
400	Bad Request
401	Unauthorized
403	Forbidden
404	Not Found
409	Conflict
422	Validation Error
500	Internal Server Error
503	Service Unavailable
9. Security Requirements
Password hashing using Argon2id or bcrypt.
HTTPS communication.
Device registration.
Device-bound session.
Secure token storage.
Role-based access control.
Server-side print authorization.
Session expiration.
Session revocation.
Audit logging.
Database backup.
Fail-closed authorization.
Idempotent print job release.
9.1 Important Security Rules
Closing the GUI must not release a held job.
Stopping the client service must not bypass server authorization.
Employees must not print directly to printer IP addresses.
Only the central print server can submit jobs to printers.
Expired sessions must not authorize new jobs.
Disabled employees cannot print.
Duplicate release requests must not create duplicate prints.
Admin actions must be logged.
10. Server Infrastructure

Assumption: 50–150 employee PCs.

Component	Minimum	Recommended
CPU	4 Core	8 Core
RAM	8 GB	16 GB
Storage	256 GB SSD	512 GB SSD
Network	1 Gbps	1 Gbps
OS	Windows Server	Windows Server 2022/2025
Database	PostgreSQL	PostgreSQL
10.1 Network Ports
Service	Port
PrintGuard API	443
PostgreSQL	5432
Windows Print Spooler	Windows-managed
Admin Dashboard	443

PostgreSQL must only be accessible from the backend server.

10.2 Network Rules
Employee PCs use shared printers from the Central Print Server.
Direct access from employee VLAN to printer IP must be blocked.
Only the Print Server can communicate with printer devices.
Admin Dashboard access must be restricted.
Central Server should use static IP.
11. Windows Client Installation

Installer name:

PrintGuard-Setup-1.0.0.exe

Installation workflow:

Check Windows compatibility.
Check administrator privileges.
Install Windows Service.
Install desktop GUI.
Register Windows Service.
Configure service startup.
Register GUI startup for logged-in users.
Configure server endpoint.
Register client device.
Test server connection.
Complete installation.
11.1 Folder Structure
text
C:\Program Files\PrintGuard\
│
├── Service\
│   └── PrintGuard.Service.exe
│
├── Desktop\
│   └── PrintGuard.UI.exe
│
├── Libraries\
│
├── Config\
│   └── appsettings.json
│
└── Logs\
    └── application.log


Sensitive credentials must not be stored as plaintext in configuration files.

12. UI/UX Requirements
12.1 Design System
Element	Specification
Style	Modern Enterprise
Primary	#2563EB
Background	#F8FAFC
Font	Inter / Segoe UI
Border Radius	10–14px
Icons	Lucide
Layout	Minimal
Theme	Light
12.2 Desktop Popup Screens

Required screens:

Login Popup.
Authentication Error.
Authorization Successful.
Server Connection Error.
Print Job Status.
Settings.
Account Information.
12.3 Admin Dashboard Pages
Dashboard Overview.
Employee Management.
Printer Management.
Computer Management.
Print History.
Reports & Analytics.
Authorization Logs.
System Settings.

13. Acceptance Criteria
Installer works on Windows 10/11.
Windows Service starts automatically.
GUI starts on Windows user login.
Jobs enter HOLD_QUEUE before authentication.
Login popup appears for new jobs.
Username/password validation works.
Remember Me works securely.
Inactive users cannot print.
Server offline keeps jobs held.
Authorized jobs are released exactly once.
Printer information is recorded.
Page count is recorded when available.
Color mode is recorded when available.
Print History appears in Admin Dashboard.
Employee management works.
Printer management works.
Computer management works.
Audit logs are recorded.
Installer supports safe upgrade and uninstall.
14. Repository Structure

Recommended project structure:

text
PrintGuard/
│
├── src/
│   ├── PrintGuard.API/
│   ├── PrintGuard.Application/
│   ├── PrintGuard.Domain/
│   ├── PrintGuard.Infrastructure/
│   │
│   ├── PrintGuard.Service/
│   ├── PrintGuard.UI/
│   │
│   ├── PrintGuard.PrintServer/
│   ├── PrintGuard.QueueManager/
│   │
│   └── PrintGuard.Admin/
│
├── tests/
│   ├── UnitTests/
│   ├── IntegrationTests/
│   └── PrintQueueTests/
│
├── database/
│   ├── migrations/
│   └── seeds/
│
├── installer/
│
└── docs/
    ├── PRD.md
    ├── API.md
    ├── DATABASE.md
    ├── DEPLOYMENT.md
    └── USER_MANUAL.md

15. Final Product Deliverables
Deliverable	Output
Windows Installer	PrintGuard-Setup.exe
Background Service	PrintGuard.Service.exe
Desktop GUI	PrintGuard.UI.exe
Backend	ASP.NET Core API
Admin Dashboard	React / Next.js
Database	PostgreSQL
Queue Manager	HOLD / RELEASE
API Documentation	Swagger / OpenAPI
Database Migration	SQL / EF Core
Deployment Guide	Documentation
User Manual	Employee Guide
Admin Manual	IT Guide
Test Report	QA Documentation
16. Final Architecture Decisions

Three decisions must be maintained throughout development:

1. Server-side authorization is mandatory.

The popup is only the user interface. The actual enforcement must be implemented on the central print queue.

2. All printers must be centralized.

Employee PCs must use shared printers managed by the Central Print Server. Direct printing to physical printer IP addresses must be blocked at the network level.

3. Authorization and printing completion are separate events.

Successful login does not mean the document has been physically printed.

Use separate states:

AUTHORIZED
RELEASING
PRINTING
COMPLETED

Print completion from Windows Spooler is not necessarily proof that all physical pages were printed successfully. Printer counters or SNMP integration can be considered in future versions.

END OF DOCUMENT

Project: PrintGuard
Document: Product Requirements Document
Version: 1.0
Status: Final MVP Specification