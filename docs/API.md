# Dokumentasi API PrintGuard

Base URL:
```
https://printguard-server.local/api/v1
```

Format Respon Standar:
```json
{
  "success": true,
  "message": "Deskripsi respon",
  "data": {},
  "errors": null,
  "timestamp": "2026-11-27T14:30:00Z"
}
```

---

## 1. Authentication Endpoints

### 1.1 POST `/auth/login`
Autentikasi akun karyawan dari PC client.
- **Request Body**:
```json
{
  "username": "budi.santoso",
  "password": "Password@123",
  "computer_id": 1,
  "remember_me": true
}
```
- **Response**:
```json
{
  "success": true,
  "data": {
    "userId": 1,
    "fullName": "Budi Santoso",
    "username": "budi.santoso",
    "department": "Finance & Accounting",
    "token": "eyJhbGciOi...",
    "sessionToken": "S3ss10nTok3n...",
    "expiresAt": "2026-11-27T22:30:00Z"
  }
}
```

### 1.2 POST `/auth/logout`
Header: `X-Session-Token: <token>`

---

## 2. Device Endpoints

### 2.1 POST `/devices/register`
Mendaftarkan PC baru ke sistem.
```json
{
  "hostname": "PC-FIN-01",
  "ipAddress": "192.168.10.51",
  "windowsVersion": "Windows 11 Pro",
  "agentVersion": "1.0.0"
}
```

### 2.2 POST `/devices/heartbeat`
```json
{
  "computerId": 1,
  "deviceToken": "device-token-string",
  "agentVersion": "1.0.0",
  "ipAddress": "192.168.10.51"
}
```

---

## 3. Print Job Endpoints

### 3.1 POST `/print-jobs`
Mendaftarkan pekerjaan cetak yang masuk dan langsung ditahan (HELD).
```json
{
  "serverJobId": "SPOOL-101",
  "computerId": 1,
  "printerId": 1,
  "documentName": "Laporan_Keuangan.pdf",
  "totalPages": 5,
  "copies": 1,
  "colorMode": "MONO"
}
```

### 3.2 POST `/print-jobs/{id}/authorize`
Mengotorisasi pekerjaan cetak.
```json
{
  "username": "budi.santoso",
  "password": "Password@123",
  "sessionToken": null,
  "computerId": 1,
  "rememberMe": true
}
```

### 3.3 POST `/print-jobs/{id}/release`
Memindahkan job dari antrean tahan ke antrean cetak printer fisik.

---

## 4. Admin Endpoints

- `POST /admin/login`: Login Admin IT.
- `GET /admin/dashboard`: Statistik ringkasan, grafik mingguan, karyawan dan printer teratas.
- `GET /admin/users`: Daftar karyawan beserta filter departemen dan status aktif.
- `POST /admin/users`: Tambah akun karyawan baru.
- `PUT /admin/users/{id}`: Edit profil karyawan.
- `PATCH /admin/users/{id}/status`: Aktifkan atau nonaktifkan karyawan.
- `POST /admin/users/{id}/reset-password`: Reset kata sandi karyawan.
- `GET /admin/printers`: Daftar printer jaringan.
- `POST /admin/printers`: Tambah konfigurasi printer baru.
- `GET /admin/computers`: Monitor komputer yang terdaftar dan heartbeat terakhir.
- `GET /admin/print-jobs`: Riwayat lengkap pencetakan dokumen.
- `GET /admin/audit-logs`: Log audit keamanan.
