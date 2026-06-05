# SS-AuthService

## Overview

`SS-AuthService` adalah microservice autentikasi dan otorisasi terpusat untuk platform SamStore. Dibangun dengan **.NET 10.0 (C#)** menggunakan pola **Clean Architecture** dan **CQRS (MediatR)**, service ini bertanggung jawab atas seluruh siklus identitas pengguna: pendaftaran, login, manajemen token JWT, Multi-Factor Authentication (TOTP), RBAC (Role-Based Access Control), serta alur email seperti verifikasi dan reset password.

Token JWT yang diterbitkan menggunakan algoritma **RS256** (RSA private key). API Gateway menggunakan RSA public key yang sesuai untuk memvalidasi token tanpa perlu berkomunikasi ke AuthService untuk setiap request.

---

## Tech Stack

| Kategori       | Teknologi                              |
| -------------- | -------------------------------------- |
| Runtime        | .NET 10.0 (C#)                         |
| Database       | PostgreSQL                             |
| ORM            | Entity Framework Core (Npgsql)         |
| Message Broker | RabbitMQ (Outbox Pattern)              |
| Hashing        | Argon2id (via Konscious.Security.Cryptography) |
| JWT            | RS256 (RSA key pair)                   |
| MFA            | TOTP (Time-based One-Time Password)    |
| Telemetry      | Serilog, OpenTelemetry                 |
| Testing        | xUnit, FluentValidation, MediatR       |

---

## Arsitektur: Clean Architecture

```text
SS-AuthService/
├── src/
│   ├── SS.AuthService.API/              # Presentation Layer
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs        # Register, Login, Verify Email, Forgot/Reset Password, Refresh, Logout
│   │   │   ├── UserController.cs        # CRUD User, Sessions, Lock/Unlock, Activate/Deactivate, MFA admin
│   │   │   ├── MfaController.cs         # Setup MFA, Enable MFA, Verify TOTP, Recovery codes
│   │   │   ├── RolesController.cs       # CRUD Roles & permission assignment
│   │   │   ├── MenusController.cs       # CRUD Menus (untuk navigasi RBAC)
│   │   │   └── SecurityController.cs    # Login attempts, audit security
│   │   ├── Middlewares/                 # Custom middleware (e.g., exception handling)
│   │   ├── Filters/                     # Action filters
│   │   ├── Extensions/                  # Extension methods untuk DI
│   │   └── Program.cs                   # Entry point
│   │
│   ├── SS.AuthService.Application/      # Application Layer (CQRS)
│   │   ├── Auth/
│   │   │   ├── Commands/                # RegisterUser, Login, Logout, Refresh, ForgotPassword, ResetPassword, VerifyEmail, SetupMfa, EnableMfa, VerifyMfa
│   │   │   ├── Handlers/                # MediatR handlers untuk tiap command
│   │   │   ├── DTOs/                    # Request/Response DTOs
│   │   │   └── Validators/              # FluentValidation validators
│   │   ├── Users/                       # Commands & Queries untuk manajemen user
│   │   ├── Roles/                       # Commands & Queries untuk RBAC roles
│   │   ├── Menus/                       # Commands & Queries untuk menus
│   │   ├── RoleMenus/                   # Assign/revoke menu ke role
│   │   ├── Security/                    # Queries untuk login attempts & audit
│   │   └── Interfaces/                  # Port definitions (IEmailService, IJwtService, dll)
│   │
│   ├── SS.AuthService.Domain/           # Domain Layer (Core Business)
│   │   ├── Entities/
│   │   │   ├── User.cs                  # User identity entity
│   │   │   ├── AuthSession.cs           # Refresh token session
│   │   │   ├── Role.cs                  # RBAC role
│   │   │   ├── Menu.cs                  # Navigation menu item
│   │   │   ├── RoleMenu.cs              # Role ↔ Menu mapping
│   │   │   ├── LoginAttempt.cs          # Brute force tracking
│   │   │   ├── PasswordHistory.cs       # Reuse prevention history
│   │   │   ├── PasswordReset.cs         # Password reset token
│   │   │   ├── EmailVerification.cs     # Email verification token
│   │   │   ├── MfaRecoveryCode.cs       # MFA backup codes
│   │   │   ├── SocialAccount.cs         # Future OAuth2 link
│   │   │   ├── OutboxEvent.cs           # Transactional outbox record
│   │   │   └── InboxEvent.cs            # Idempotency inbox record
│   │   └── Constants/                   # Domain constants
│   │
│   └── SS.AuthService.Infrastructure/   # Infrastructure Layer
│       ├── Authentication/              # JwtService, JwtOptions
│       ├── BackgroundServices/          # Outbox publisher worker
│       ├── Messaging/                   # RabbitMQ publisher & consumer
│       ├── Migrations/                  # EF Core database migrations
│       ├── Persistence/                 # ApplicationDbContext (EF Core)
│       ├── Repositories/                # EF Core repository implementations
│       ├── Security/                    # Argon2id hashing, TOTP
│       ├── Services/                    # Email service (SMTP)
│       └── Templates/                   # Email HTML templates
│
└── tests/
    ├── SS.AuthService.API.Tests/
    ├── SS.AuthService.Application.Tests/
    ├── SS.AuthService.Domain.Tests/
    └── SS.AuthService.Infrastructure.Tests/
```

---

## API Endpoints

### Auth (`/api/auth`)

| Method | Endpoint                  | Auth      | Rate Limit   | Deskripsi                                               |
| ------ | ------------------------- | --------- | ------------ | ------------------------------------------------------- |
| POST   | `/api/auth/register`      | anonim    | anti-abuse   | Daftar user baru, kirim email verifikasi                |
| POST   | `/api/auth/login`         | anonim    | brute-force  | Login dengan email/password, return JWT + refresh token |
| GET    | `/api/auth/verify-email`  | anonim    | global       | Verifikasi email via token dari link email              |
| POST   | `/api/auth/forgot-password` | anonim  | anti-abuse   | Kirim link reset password ke email                      |
| POST   | `/api/auth/reset-password` | anonim   | anti-abuse   | Reset password dengan token dari email                  |
| POST   | `/api/auth/refresh`       | anonim    | anti-abuse   | Perbarui JWT menggunakan refresh token dari cookie      |
| POST   | `/api/auth/logout`        | anonim    | global       | Hapus refresh token & clear cookies                     |

### MFA (`/api/mfa`)

| Method | Endpoint                   | Auth      | Deskripsi                                      |
| ------ | -------------------------- | --------- | ---------------------------------------------- |
| POST   | `/api/mfa/verify`          | anonim    | Verifikasi TOTP code (setelah login MFA step)  |
| POST   | `/api/mfa/setup`           | JWT       | Generate TOTP secret & QR code                 |
| POST   | `/api/mfa/enable`          | JWT       | Aktifkan MFA setelah setup                     |
| GET    | `/api/mfa/recovery-codes`  | JWT       | Lihat recovery codes                           |

### User Management (`/api/user`)

| Method | Endpoint                                          | Permission         | Deskripsi                            |
| ------ | ------------------------------------------------- | ------------------ | ------------------------------------ |
| GET    | `/api/user/me`                                    | JWT (self)         | Profil user yang sedang login        |
| PATCH  | `/api/user/me`                                    | JWT (self)         | Update profil sendiri                |
| POST   | `/api/user/me/change-password`                    | JWT (self)         | Ganti password sendiri               |
| GET    | `/api/user`                                       | Users:Read         | List semua user (filter/sort/paging) |
| GET    | `/api/user/{publicId}`                            | Users:Read         | Detail user                          |
| PUT    | `/api/user/{publicId}`                            | Users:Update       | Update data user                     |
| DELETE | `/api/user/{publicId}`                            | Users:Delete       | Soft-delete user                     |
| PUT    | `/api/user/{publicId}/unlock`                     | Users:Update       | Buka blokir akun                     |
| PUT    | `/api/user/{publicId}/lock`                       | Users:Update       | Kunci akun sementara                 |
| PUT    | `/api/user/{publicId}/activate`                   | Users:Update       | Aktifkan akun                        |
| PUT    | `/api/user/{publicId}/deactivate`                 | Users:Update       | Nonaktifkan akun                     |
| PUT    | `/api/user/{publicId}/force-reset-password`       | Users:Update       | Paksa reset password (kirim email)   |
| PUT    | `/api/user/{publicId}/role`                       | Users:Update       | Assign role ke user                  |
| GET    | `/api/user/{publicId}/sessions`                   | Users:Read         | Daftar sesi aktif user               |
| DELETE | `/api/user/{publicId}/sessions`                   | Users:Update       | Cabut semua sesi user                |
| DELETE | `/api/user/{publicId}/sessions/{sessionPublicId}` | Users:Update       | Cabut satu sesi spesifik             |
| POST   | `/api/user/{publicId}/resend-verification`        | Users:Update       | Kirim ulang email verifikasi         |
| PUT    | `/api/user/{publicId}/mfa/disable`                | Users:Update       | Nonaktifkan MFA (admin recovery)     |
| POST   | `/api/user/{publicId}/mfa/recovery-codes/regenerate` | Users:Update    | Regenerasi MFA recovery codes        |
| GET    | `/api/user/{publicId}/mfa`                        | Users:Read         | Info status MFA user                 |

### Roles (`/api/roles`), Menus (`/api/menus`), Security (`/api/security`)

Endpoint CRUD untuk manajemen RBAC dan audit security. Semua membutuhkan JWT + permission yang sesuai.

---

## Database Schema

Database: `ss_auth_db` (PostgreSQL)

| Tabel               | Deskripsi                                                |
| ------------------- | -------------------------------------------------------- |
| `users`             | Data identity utama user                                 |
| `auth_sessions`     | Refresh token sessions (device binding, IP tracking)     |
| `roles`             | Daftar role (admin, seller, customer, dll)               |
| `menus`             | Item navigasi yang dapat diassign ke role                |
| `role_menus`        | Mapping role ↔ menu dengan permission flags              |
| `login_attempts`    | Riwayat percobaan login untuk brute-force protection     |
| `password_history`  | Riwayat hash password untuk mencegah reuse               |
| `password_resets`   | Token reset password dengan expiry                       |
| `email_verifications` | Token verifikasi email dengan expiry                   |
| `mfa_recovery_codes` | Backup codes MFA (hashed)                              |
| `social_accounts`   | Link OAuth2 (reserved for future use)                   |
| `outbox_events`     | Transactional Outbox untuk RabbitMQ publishing           |
| `inbox_events`      | Idempotency guard untuk konsumsi event                   |

---

## Token & Cookie Strategy

| Token         | Tipe        | Storage           | TTL (default)    |
| ------------- | ----------- | ----------------- | ---------------- |
| Access Token  | JWT RS256   | HttpOnly Cookie (`accessToken`) + Response body | Dikonfigurasi via `JwtOptions.AccessTokenExpirationMinutes` |
| Refresh Token | Opaque UUID | HttpOnly Cookie (`refreshToken`) | Dikonfigurasi via `SecuritySettings.RefreshTokenExpiryDays` |

Cookie flags:
- `HttpOnly: true` selalu
- `Secure: true` hanya di Production
- `SameSite: Strict` di Production, `Lax` di Development

---

## RabbitMQ Events

Service ini **mempublish** event berikut (via Outbox Pattern):

| Routing Key           | Dipicu Saat                              | Payload (ringkasan)         |
| --------------------- | ---------------------------------------- | --------------------------- |
| `auth.user.registered` | User berhasil mendaftar                 | `{ userId, email, username }` |
| `auth.user.verified`  | User berhasil verifikasi email           | `{ userId, email }`         |

Exchange: `samstore.events` (Topic, Durable)

---

## Environment Variables

| Variable              | Deskripsi                                              | Wajib |
| --------------------- | ------------------------------------------------------ | ----- |
| `SSAuthDB`            | PostgreSQL connection string                           | ✅    |
| `Jwt__PrivateKeyPath` | Path ke file PEM RSA private key untuk sign JWT       | ✅    |
| `Jwt__PublicKeyPath`  | Path ke file PEM RSA public key                        | ✅    |
| `SmtpServer`          | Alamat SMTP server                                     | ✅    |
| `SmtpPort`            | Port SMTP (contoh: `587`)                              | ✅    |
| `SmtpUsername`        | Username SMTP                                          | ✅    |
| `SmtpPassword`        | Password SMTP                                          | ✅    |
| `GATEWAY_HMAC_SECRET` | Shared secret untuk validasi signature dari Gateway    | ✅    |
| `OpenTelemetry__Endpoint` | OTel Collector endpoint                           | ✅    |
| `ASPNETCORE_ENVIRONMENT` | Environment runtime                                | ✅    |

---

## Instalasi & Menjalankan

### Prasyarat

- .NET 10.0 SDK
- PostgreSQL (database: `ss_auth_db`)
- RabbitMQ
- SMTP server
- RSA key pair (PEM format)

### Setup

```bash
git clone <repository>
cd SamStore/SS-AuthService
dotnet restore
```

### Menjalankan Lokal

```bash
dotnet run --project src/SS.AuthService.API/SS.AuthService.API.csproj
```

### Build

```bash
dotnet build
```

### Testing

```bash
dotnet test
```

### Docker

```bash
docker build -t ss-auth-service .
```

---

## Observability

- **Logging**: Serilog dengan `CompactJsonFormatter` ke stdout → Fluent Bit → Loki.
- **Tracing**: OpenTelemetry SDK mengirim traces ke OTel Collector via OTLP.
- **Metrics**: Diekspor via OpenTelemetry.

---

## Security Features

| Fitur                    | Implementasi                                                  |
| ------------------------ | ------------------------------------------------------------- |
| Password hashing         | Argon2id (GPU-resistant)                                      |
| Token signing            | RS256 JWT (asymmetric, private key hanya di AuthService)      |
| MFA                      | TOTP (RFC 6238 compliant) + Recovery codes                    |
| Brute-force protection   | `LoginAttempt` tracking + account lockout                     |
| Password reuse prevention | `PasswordHistory` table                                      |
| Account enumeration prevention | ForgotPassword selalu return 200 OK                  |
| Session revocation       | Setiap refresh token di-track per session/device              |

---

## Known Issues

Tidak ada issue yang teridentifikasi dari source code.

## Future Improvements

- Tambah OAuth2 external login providers (Google, GitHub).
- Implementasi device fingerprinting untuk session security lebih ketat.
