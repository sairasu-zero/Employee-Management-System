# Employee Records Management System (ERMS)

> Enterprise Full-Stack Application for Secure Employee Records, Role-Based Access Control, and Regulatory Compliance.

---

## 1. Project Overview

The **Employee Records Management System (ERMS)** is a modern, enterprise-grade full-stack solution engineered to securely manage workforce records, departmental structures, authentication with Multi-Factor Authentication (MFA), and audit logging.

**Phase 1 – Foundation** establishes the development baseline:
- High-performance **React 18** client scaffolded with **TypeScript** (Strict Mode) and **Vite**.
- Scalable **ASP.NET Core Web API** backend (.NET 8 LTS) with dependency injection, separation of concerns, and Swagger documentation.
- Robust **PostgreSQL** relational database integration via **Entity Framework Core** and the **Npgsql** provider.
- Health diagnostic endpoints and real-time connectivity verification between Frontend, Backend, and Database.

---

## 2. Technology Stack

### Frontend
- **Framework**: [React 18](https://react.dev/)
- **Language**: [TypeScript](https://www.typescriptlang.org/) (Strict Mode enabled)
- **Build Tool / Bundler**: [Vite](https://vitejs.dev/)
- **Routing**: [React Router DOM v6](https://reactrouter.com/)
- **Iconography**: [Lucide React](https://lucide.dev/)
- **Design System**: Light Mode, responsive CSS3 variables, enterprise dashboard aesthetic

### Backend
- **Framework**: [ASP.NET Core Web API](https://dotnet.microsoft.com/apps/aspnet/apis) (.NET 8.0 LTS)
- **Language**: C# 12
- **ORM**: [Entity Framework Core 8](https://learn.microsoft.com/ef/core/)
- **API Documentation**: [Swagger / OpenAPI (Swashbuckle)](https://swagger.io/)
- **Architecture**: Clean separation of concerns (Controllers, DTOs, Data, Services, Repositories, Middleware, Configuration)

### Database
- **Engine**: [PostgreSQL 18](https://www.postgresql.org/)
- **Database Name**: `erms_db`
- **EF Core Provider**: [Npgsql.EntityFrameworkCore.PostgreSQL](https://www.npgsql.org/efcore/)
- **Database Administration**: pgAdmin 4

---

## 3. Folder Structure

```
Employee Management System/
├── backend/
│   └── ERMS.API/
│       ├── Configuration/             # Secure configuration & DB connection builder
│       │   └── DatabaseConfiguration.cs
│       ├── Controllers/               # REST API Controllers
│       │   └── HealthController.cs
│       ├── Data/                      # Entity Framework Core contexts & mappings
│       │   └── ApplicationDbContext.cs
│       ├── DTOs/                      # Data Transfer Objects
│       │   └── HealthDtos.cs
│       ├── Interfaces/                # Service & repository contracts
│       ├── Middleware/                # Custom HTTP middleware
│       ├── Models/                    # Future ERMS entity models (Phase 2+)
│       ├── Properties/                # Launch settings & environment profiles
│       │   └── launchSettings.json
│       ├── Repositories/              # Data access repositories
│       ├── Services/                  # Business logic services
│       ├── Validators/                # Request validation rules
│       ├── appsettings.json           # Default backend configuration
│       ├── appsettings.Development.json # Development configuration & CORS origins
│       ├── .env.example               # Backend environment template
│       └── ERMS.API.csproj            # .NET 8 project file & dependencies
├── frontend/
│   ├── public/                        # Static assets (favicons, icons)
│   │   └── vite.svg
│   ├── src/
│   │   ├── assets/                    # Bundled graphics and media
│   │   ├── components/                # Modular React components
│   │   │   ├── common/                # Header, StatusCard, Footer
│   │   │   ├── layout/                # MainLayout wrapper
│   │   │   └── ui/                    # Reusable badges, buttons
│   │   ├── contexts/                  # React state contexts
│   │   ├── hooks/                     # Custom React hooks
│   │   ├── layouts/                   # Layout templates
│   │   ├── pages/                     # Routed view components
│   │   │   ├── admin/                 # Administrator module placeholders
│   │   │   ├── auth/                  # Authentication & login module
│   │   │   ├── department/            # Department management placeholders
│   │   │   ├── employee/              # Employee records placeholders
│   │   │   ├── hr/                    # Human Resources placeholders
│   │   │   └── FoundationPage.tsx     # Phase 1 Foundation verification view
│   │   ├── routes/                    # React Router configuration
│   │   │   └── AppRoutes.tsx
│   │   ├── services/                  # API client & fetch wrappers
│   │   │   └── api.ts
│   │   ├── styles/                    # Global CSS & ERMS theme variables
│   │   │   └── index.css
│   │   ├── types/                     # TypeScript interfaces & types
│   │   │   └── index.ts
│   │   ├── utils/                     # Helper utility functions
│   │   ├── App.tsx                    # Root application component
│   │   ├── main.tsx                   # Entry point with StrictMode
│   │   └── vite-env.d.ts              # Vite environment typings
│   ├── .env                           # Local environment variables
│   ├── .env.example                   # Environment template
│   ├── .gitignore                     # Frontend-specific Git exclusions
│   ├── index.html                     # HTML5 shell
│   ├── package.json                   # NPM dependencies & scripts
│   ├── tsconfig.json                  # TypeScript compiler settings (strict)
│   └── vite.config.ts                 # Vite server & bundler configuration
├── .env.example                       # Root environment variables template
├── .gitignore                         # Repository-wide Git exclusions
└── README.md                          # Project documentation
```

---

## 4. Prerequisites

Before running the application, ensure the following software is installed on your development machine:

1. **.NET 8.0 SDK** (or later) &mdash; Verify with `dotnet --version`
2. **Node.js (v18.x or later)** and **npm** &mdash; Verify with `node --version` and `npm --version`
3. **PostgreSQL (v14 or later)** &mdash; Verify service is running on port `5432`
4. **Git** &mdash; Verify with `git --version`

---

## 5. Database Configuration (PostgreSQL)

1. **Start PostgreSQL**: Ensure the PostgreSQL service (e.g. `postgresql-x64-18`) is active.
2. **Create Database**:
   Open `psql` or `pgAdmin 4` and execute:
   ```sql
   CREATE DATABASE erms_db;
   ```
3. **Configure Credentials**:
   Credentials can be set via environment variables, User Secrets, or in `backend/ERMS.API/appsettings.Development.json`.

   *Option A: Environment Variables (Recommended)*
   ```bash
   # In terminal or .env
   DB_HOST=localhost
   DB_PORT=5432
   DB_NAME=erms_db
   DB_USER=postgres
   DB_PASSWORD=your_postgres_password
   ```

   *Option B: .NET User Secrets*
   ```bash
   cd backend/ERMS.API
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=erms_db;Username=postgres;Password=your_postgres_password"
   ```

---

## 6. Installation Instructions

### Backend Setup

1. Open a terminal and navigate to the backend directory:
   ```bash
   cd backend/ERMS.API
   ```
2. Restore NuGet dependencies:
   ```bash
   dotnet restore
   ```
3. Build the project:
   ```bash
   dotnet build
   ```
4. Start the ASP.NET Core API server:
   ```bash
   dotnet run
   ```
   The backend will start at:
   - **API Base URL**: `http://localhost:5000`
   - **Swagger UI**: `http://localhost:5000/swagger`

### Frontend Setup

1. Open a second terminal and navigate to the frontend directory:
   ```bash
   cd frontend
   ```
2. Install npm dependencies:
   ```bash
   npm install
   ```
3. Copy environment configuration:
   ```bash
   cp .env.example .env
   ```
   Ensure `VITE_API_BASE_URL=http://localhost:5000` matches the backend URL.

4. Start the Vite development server:
   ```bash
   npm run dev
   ```
   The frontend will be available at:
   - **Frontend URL**: `http://localhost:5173`

---

## 7. Health Check & Connectivity Verification

Once both servers are running:
1. Open your browser and navigate to `http://localhost:5173`.
2. The **System Foundation** dashboard will display real-time status cards:
   - **Frontend Status**: `Running` (React 18 + Vite)
   - **Backend Connection**: `Connected` (verified via `GET /api/health`)
   - **Database Connection**: `Connected` (verified via `GET /api/health/database` connecting to `erms_db`)
3. Use the **Refresh Status** button to re-trigger live connectivity checks at any time.

---

## 8. Upcoming Architectural Phases

The architecture is pre-configured to support incremental module development:
- **Phase 2**: Authentication, MFA (TOTP), Session Security & Token Lifecycle
- **Phase 3**: Role-Based Access Control (RBAC), Roles & Granular Permissions
- **Phase 4**: Employee Management, Department Hierarchies, Employment History
- **Phase 5**: Employee Documents, Storage, Full Audit Trail & Security Logs
