# LoanTracker — Personal Finance Loan Management System

A production-grade system to track personal loans from persons and banks.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core 8 Web API |
| ORM | Entity Framework Core 8 |
| Database | PostgreSQL 16 |
| Frontend | React 18 + TypeScript + Vite |
| UI | shadcn/ui + Tailwind CSS |
| Charts | Recharts |
| Forms | React Hook Form + Zod |
| State | TanStack Query v5 |

## Architecture

```
LoanTracker/
├── backend/
│   └── src/
│       ├── LoanTracker.Domain/          # Entities, Enums (no dependencies)
│       ├── LoanTracker.Application/     # Interfaces, DTOs, Validators
│       ├── LoanTracker.Infrastructure/  # EF Core, Service implementations
│       └── LoanTracker.Api/             # Controllers, Middleware, Program.cs
├── frontend/
│   └── src/
│       ├── api/          # Axios API clients
│       ├── hooks/        # React Query hooks
│       ├── pages/        # Page components
│       ├── components/   # Reusable UI components
│       └── types/        # TypeScript type definitions
├── database/
│   ├── schema.sql        # Reference-only: hand-written schema (native enums)
│   └── seed.sql          # Reference-only: sample data
└── docker-compose.yml

# NOTE: At runtime, EF Core migrations create the schema and DbInitializer
# seeds sample data on first startup. The database/*.sql files are kept as
# design deliverables and are NOT applied automatically.
```

## Quick Start — Docker (Recommended)

```bash
# 1. Copy environment file
cp .env.example .env

# 2. Start everything
docker compose up -d

# 3. Access
#    Frontend:  http://localhost:5173
#    API:       http://localhost:8090
#    Swagger:   http://localhost:8090/swagger
```

## Development Setup

### Database (PostgreSQL)

```bash
# Start only the database
docker compose up postgres -d

# Or use an existing PostgreSQL instance and update .env
```

### Backend

```bash
cd backend

# Restore packages
dotnet restore

# Apply database migrations
dotnet ef database update --project src/LoanTracker.Infrastructure --startup-project src/LoanTracker.Api

# Run the API
dotnet run --project src/LoanTracker.Api
# API available at http://localhost:8090
# Swagger at http://localhost:8090/swagger
```

### Frontend

```bash
cd frontend

# Install dependencies
npm install

# Start development server
npm run dev
# Available at http://localhost:5173
```

## API Endpoints

### Lenders
| Method | Path | Description |
|--------|------|-------------|
| GET | /api/lenders | List all lenders |
| GET | /api/lenders/{id} | Get lender with financial summary |
| POST | /api/lenders | Create lender |
| PUT | /api/lenders/{id} | Update lender |
| POST | /api/lenders/{id}/deactivate | Deactivate lender |
| DELETE | /api/lenders/{id} | Soft delete lender |

### Loans
| Method | Path | Description |
|--------|------|-------------|
| GET | /api/loans | List loans (filter by lenderId, status) |
| GET | /api/loans/{id} | Get loan with payment summary |
| POST | /api/loans | Create loan |
| PUT | /api/loans/{id} | Update loan |
| POST | /api/loans/{id}/close | Close loan |
| POST | /api/loans/{id}/interest-rate | Update interest rate (with history) |
| DELETE | /api/loans/{id} | Soft delete loan |

### Payments
| Method | Path | Description |
|--------|------|-------------|
| GET | /api/payments | List payments (filter by loan/lender/date) |
| GET | /api/payments/loan/{loanId} | Payments for a specific loan |
| POST | /api/payments | Record payment (principal/interest/mixed) |
| PUT | /api/payments/{id} | Edit payment |
| DELETE | /api/payments/{id} | Soft delete payment |

### Dashboard
| Method | Path | Description |
|--------|------|-------------|
| GET | /api/dashboard | Full summary with all metrics |
| GET | /api/dashboard/lenders | Lender-wise aggregates |
| GET | /api/dashboard/monthly | Monthly payment trend |
| GET | /api/dashboard/yearly | Yearly summary |

## Business Rules

1. **Outstanding Principal** = Original Principal − Σ(principal payments)
2. **Auto-close**: Loan automatically closes when outstanding = 0
3. **Payment validation**: `principal + interest + penalty = total` (enforced at DB + API)
4. **No overpayment**: Principal payment cannot exceed outstanding principal
5. **Closed loan**: Rejects new payments; rate changes not allowed
6. **Soft delete**: Payments and loans use soft delete to preserve audit trail
7. **Interest rate change**: Logged in `loan_interest_rate_history` with effective date

## Payment Types Supported

| Scenario | principal_amount | interest_amount | Total |
|---------|----------------|----------------|-------|
| Interest only | 0 | > 0 | = interest |
| Principal only | > 0 | 0 | = principal |
| Mixed (EMI) | > 0 | > 0 | = sum |
| Penalty only | 0 | 0 (penalty > 0) | = penalty |

## Dashboard Metrics

| Metric | Calculation |
|--------|------------|
| Total Borrowed | Σ(loan.principal_amount) |
| Total Outstanding | Total Borrowed − Total Principal Paid |
| Total Principal Paid | Σ(payment.principal_amount) |
| Total Interest Paid | Σ(payment.interest_amount) |
| This Month Payment | Σ(payment.total_amount) where month = current |
| Active Loans | COUNT(loans where status = Active) |

## EF Core Migrations

```bash
cd backend

# Add a migration
dotnet ef migrations add <MigrationName> \
  --project src/LoanTracker.Infrastructure \
  --startup-project src/LoanTracker.Api

# Apply migrations
dotnet ef database update \
  --project src/LoanTracker.Infrastructure \
  --startup-project src/LoanTracker.Api
```
