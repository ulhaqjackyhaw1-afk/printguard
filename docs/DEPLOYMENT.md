# Petunjuk Deployment & Instalasi PrintGuard (Centralized LAN)

Sistem PrintGuard di-deploy pada lingkungan LAN Perusahaan dengan arsitektur terpusat.

## 1. Persyaratan Infrastruktur Server

- **Sistem Operasi**: Windows Server 2022 / 2025 Standard atau Datacenter
- **RAM**: Minimum 8 GB (Direkomendasikan 16 GB)
- **CPU**: 4 - 8 Cores
- **Penyimpanan**: 256 GB+ SSD
- **Jaringan**: 1 Gbps LAN dengan Static IP (misal: `192.168.10.10`)
- **Perangkat Lunak**:
  - PostgreSQL 15 / 16
  - .NET 8.0 Hosting Bundle (ASP.NET Core Runtime + .NET Desktop Runtime)
  - IIS (Internet Information Services) atau Kestrel Reverse Proxy (Nginx / Caddy)
  - Node.js (untuk build Admin Dashboard)

---

## 2. Aturan Firewall & Port Jaringan

| Port | Protokol | Sumber | Tujuan | Keterangan |
|------|----------|--------|--------|------------|
| 443  | HTTPS    | Karyawan & Admin | Print Server | REST API & Admin Web |
| 5432 | TCP      | Localhost Server | PostgreSQL | Database (Blokir dari luar) |
| 9100 / SNMP | RAW / UDP | Print Server | Printer Fisik | Komunikasi Cetak Fisik |

> **PERINGATAN KEAMANAN**:
> Akses langsung dari VLAN karyawan ke IP fisik printer harus **DIBLOKIR** pada sakelar jaringan (switch) atau firewall router perusahaan. Semua PC karyawan hanya boleh mengakses printer lewat antrean share Central Print Server.

---

## 3. Langkah Instalasi Server

1. **Setup Database**:
   Jalankan file [database/schema.sql](file:///d:/1. MAGANG/2. ULHAQ/print-agent/database/schema.sql) dan [database/seeds/seed_data.sql](file:///d:/1. MAGANG/2. ULHAQ/print-agent/database/seeds/seed_data.sql) di PostgreSQL.
2. **Publish Backend API**:
   ```powershell
   dotnet publish src/PrintGuard.API/PrintGuard.API.csproj -c Release -o C:\inetpub\PrintGuardAPI
   ```
3. **Build & Host Admin Dashboard**:
   ```bash
   cd src/PrintGuard.Admin
   npm run build
   ```
   Salin folder `dist` ke web root server.

---

## 4. Instalasi Client PC Karyawan

Aplikasi client didistribusikan dalam bentuk installer:
- **PrintGuard-Setup-1.0.0.exe**
- Komponen yang terpasang di client:
  1. `PrintGuard.Service.exe`: Berjalan otomatis sebagai Windows Service (SYSTEM account).
  2. `PrintGuard.UI.exe`: Berjalan otomatis saat user login Windows (Startup Registry).
  3. Konfigurasi `appsettings.json` diarahkan ke IP Central Server (`https://192.168.10.10/api/v1`).
