# SS-AuthService

## Overview

SS-AuthService is the centralized authentication and authorization microservice for the SamStore e-commerce platform. Built with .NET 10 and C#, it handles key aspects of identity security, credentials validation, and role-based permissions management.

The service issues short-lived JWT access tokens and long-lived refresh tokens (using HttpOnly secure cookies). It coordinates with other downstream services (like SS-ProfileService) using RabbitMQ domain events for actions such as user registration.

## Features

- **Secure Authentication**: Traditional credentials-based login backed by the GPU-resistant Argon2id hashing algorithm.
- **Token Management**: RS256 JWT access tokens and HttpOnly refresh tokens.
- **Multi-Factor Authentication (MFA)**: Built-in support for Time-Based One-Time Passwords (TOTP).
- **Role-Based Access Control (RBAC)**: Fine-grained permission system mapped to roles and menus.
- **Email Flows**: Secure verification links and token-based password reset workflows.
- **Observability**: Structured logging with Serilog, and standard OpenTelemetry traces and metrics.
- **Brute-Force Protection**: Account lockout policies and rate limiting on sensitive endpoints.

## Tech Stack

| Category       | Technology                       |
| -------------- | -------------------------------- |
| Backend        | .NET 10.0 (C#)                   |
| Database       | PostgreSQL                       |
| Message Broker | RabbitMQ (Outbox pattern)        |
| Security       | Argon2id, JWT (RS256), TOTP      |
| Telemetry      | Serilog, OpenTelemetry           |
| Testing        | xUnit, FluentValidation, MediatR |

## Project Structure

```text
SS-AuthService/
├── src/
│   ├── SS.AuthService.API/            # Presentation layer (HTTP controllers, middleware)
│   ├── SS.AuthService.Application/    # Application layer (commands, queries, validators)
│   ├── SS.AuthService.Domain/         # Domain layer (core entities, interfaces)
│   └── SS.AuthService.Infrastructure/ # Infrastructure layer (EF Core context, email, encryption)
└── tests/
    ├── SS.AuthService.API.Tests/
    ├── SS.AuthService.Application.Tests/
    ├── SS.AuthService.Domain.Tests/
    └── SS.AuthService.Infrastructure.Tests/
```

## Requirements

- .NET 10.0 SDK
- PostgreSQL
- RabbitMQ
- SMTP server credentials (for email workflows)

## Installation

```bash
git clone <repository>
cd SamStore/SS-AuthService
```

Build the dependencies:

```bash
dotnet restore
```

## Configuration

Configuration is managed via `appsettings.json` and environment variables. Key parameters:

```env
SSAuthDB=                 # PostgreSQL database connection string
Jwt__PrivateKeyPath=      # Path to JWT RSA private key (.pem)
Jwt__PublicKeyPath=       # Path to JWT RSA public key (.pem)
SmtpServer=               # SMTP server address for outgoing emails
SmtpPort=                 # SMTP port (e.g. 587)
SmtpUsername=             # SMTP username
SmtpPassword=             # SMTP password
```

## Running Locally

```bash
dotnet run --project src/SS.AuthService.API/SS.AuthService.API.csproj
```

## Build

```bash
dotnet build
```

## Testing

```bash
dotnet test
```

## API Documentation

The project exposes standard controllers. Endpoint configuration details include:

| Method | Endpoint             | Description                   |
| ------ | -------------------- | ----------------------------- |
| POST   | /api/auth/register   | Register a new user           |
| POST   | /api/auth/login      | Authenticate user credentials |
| POST   | /api/auth/mfa/verify | Verify TOTP code              |
| POST   | /api/auth/refresh    | Refresh expired JWT token     |

## Database

- **Database Type**: PostgreSQL.
- **ORM**: Entity Framework Core.
- **Database Schema**: Tracks key tables like `users`, `auth_sessions`, `roles`, `menus`, `login_attempts`, `outbox_events`, and `inbox_events` to implement transactional publishing and idempotency.

## Deployment

- **Docker**: Built using a multi-stage `Dockerfile`.
- **Docker Compose**: Can be orchestrated alongside the gateway and PostgreSQL using the global docker-compose configuration.

## Architecture Notes

- **Clean Architecture**: Strict separation of concerns (Domain, Application, Infrastructure, API).
- **CQRS Pattern**: Uses MediatR to decouple commands (write actions) and queries (read actions).
- **Reliability Patterns**: Utilizes Transactional Outbox and Inbox patterns for reliable event delivery via RabbitMQ.

## Known Issues

Not identified from source code.

## Future Improvements

- Add OAuth2 external login providers (e.g., Google, GitHub).

## License

```text
License information not specified.
```
