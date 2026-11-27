# Kamus Data PostgreSQL (Database Schema)

Database: `printguard_db`

## Daftar Tabel & Relasi

1. **`departments`**: Unit kerja / departemen perusahaan.
2. **`users`**: Akun karyawan yang memiliki hak akses mencetak.
3. **`admin_users`**: Akun administrator IT untuk dashboard web.
4. **`computers`**: Endpoint PC karyawan yang menjalankan agen client.
5. **`client_devices`**: Kredensial dan token perangkat PC client.
6. **`printers`**: Printer jaringan yang dikelola secara terpusat.
7. **`print_queues`**: Antrean printer spooler (HOLD dan RELEASE).
8. **`user_sessions`**: Sesi login dan Remember Me (masa berlaku 8 jam).
9. **`print_jobs`**: Catatan riwayat pekerjaan cetak (dokumen, halaman, warna, status).
10. **`print_authorizations`**: Riwayat otorisasi tiap pekerjaan cetak.
11. **`audit_logs`**: Log aktivitas administratif dan perubahan keamanan.
12. **`system_settings`**: Konfigurasi global sistem format JSONB.
13. **`print_job_events`**: Jejak perubahan status pekerjaan cetak (*lifecycle history*).
14. **`printer_status_logs`**: Log status ketersediaan printer.
