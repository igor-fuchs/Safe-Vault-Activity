# Safe Vault

A secure user management REST API built with ASP.NET Core, featuring JWT authentication, role-based access control (RBAC), XSS/SQL Injection protection, and a SQLite database.

## Purpose

The project implements an authentication and authorization system focused on security, serving as a practical example of best practices:

- **JWT Authentication** — tokens signed with HMAC-SHA256 and 1-hour expiry
- **RBAC (Role-Based Access Control)** — two roles: `Admin` and `User`, with role-protected endpoints
- **Password hashing** — BCrypt for secure storage
- **XSS protection** — input validation via Data Annotations and output encoding
- **SQL Injection protection** — parameterized queries via Entity Framework Core
- **Interactive Webform** — HTML interface to test all API endpoints

## Endpoints

| Method | Route               | Access       | Description                          |
|--------|---------------------|--------------|--------------------------------------|
| POST   | `/api/user/register`| Public       | Register a new user (role: User)     |
| POST   | `/api/user/login`   | Public       | Login and obtain JWT token           |
| GET    | `/api/user/me`      | User, Admin  | Profile of the authenticated user   |
| GET    | `/api/user`         | Admin        | List all users                       |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## How to Run

### 1. Clone the repository

```bash
git clone https://github.com/igor-fuchs/Safe-Vault-Activity
cd Safe-Vault-Activity/SafeVault
```

### 2. Create the `.env` file

Create the file `SafeVault/.env` with the following environment variables:

```
JWT_KEY=SuperSecretKey-SafeVault-2026-MinLength32!
CONNECTION_STRING=Data Source=SafeVault.db
```

### 3. Restore dependencies and run

```bash
dotnet restore
dotnet run
```

The application will be available at `http://localhost:5000`.

### 4. Access the Webform

Open in your browser: `http://localhost:5000/webform.html`

### 5. Pre-seeded Admin account

In development mode, an admin user is created automatically at startup in development mode:

| Field    | Value          |
|----------|----------------|
| Username | `admin`        |
| Password | `Admin@1234!`  |
| Role     | `Admin`        |

## Tests

```bash
dotnet test
```

Tests cover:
- **XSS** — HTML/JS payload rejection at the DTO layer
- **SQL Injection** — parameterized queries via EF Core
- **RBAC** — role assignment and verification

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.4 | JWT Bearer authentication |
| `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.4 | ORM + SQLite database |
| `BCrypt.Net-Next` | 4.0.3 | Secure password hashing |
| `DotNetEnv` | 3.1.1 | Load environment variables from `.env` |
| `NUnit` | 4.5.1 | Test framework |
| `NUnit3TestAdapter` | 4.6.0 | NUnit adapter for `dotnet test` |
| `Microsoft.NET.Test.Sdk` | 18.3.0 | Test execution SDK |

## Project Structure

```
SafeVault/
├── Controllers/        # UserController (4 endpoints)
├── Data/               # AppDbContext (EF Core)
├── DTOs/               # RegisterRequest, LoginRequest, UserResponse, LoginResponse
├── Middlewares/        # ExceptionHandlingMiddleware
├── Models/             # User (Id, Username, Email, PasswordHash, Role)
├── Services/           # UserService (register, login, JWT, queries)
├── Tests/              # Security tests (XSS, SQLi, RBAC)
├── wwwroot/            # webform.html (test interface)
├── Program.cs          # Application configuration
├── .env                # Environment variables (not versioned)
└── SafeVault.csproj    # Project definition and dependencies
```
