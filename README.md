# MELA Fair and Event Management System

[![CI](https://github.com/Turjo101365/MELA/actions/workflows/ci.yml/badge.svg)](https://github.com/Turjo101365/MELA/actions/workflows/ci.yml)

An enterprise cultural fair, exhibition, and festival management platform built with ASP.NET Core MVC (C#) and Microsoft SQL Server. The system coordinates multi-day cultural events, handles high-concurrency stall leasing and visitor ticketing through database stored procedures with row-level locking, and provides a seasonal recruitment board for event staff.

## Features

- Fair and Festival Scheduling: Administrators can create and configure multi-day fairs with daily capacity limits, dates, venue locations, base pricing, and stall configurations.
- High-Concurrency Stall Leasing: Vendors can view interactive fair ground layouts and book commercial booths. The reservation transaction utilizes SQL Server stored procedures with explicit row-level update locks (`UPDLOCK, ROWLOCK`) to prevent race conditions and duplicate assignments.
- Digital Ticketing and Admission: Visitors can select specific fair dates, purchase single or group admission passes within daily capacity thresholds, and obtain unique e-ticket verification codes.
- Event Workforce Recruitment: Vendors can post temporary staffing vacancies (security, sales, logistical support), while job seekers can apply directly with contact information and experience summaries.
- Role-Based Access Control: Granular authorization partitioning across Administrator, Vendor, Visitor, and Employee personas via ASP.NET Core Identity.
- Analytics and Reporting: Keyless database views calculate fair revenue, occupancy rates, and daily visitor counts.
- Account Security: Single-use, time-limited password reset tokens hashed with SHA-256, supported by fixed-window rate limiting on reset endpoints and automated SMTP notifications.

## Tech Stack

- Backend: ASP.NET Core 8 MVC (C# 12)
- Data Access: Entity Framework Core 8 (ORM, Identity, and Migrations) and Dapper 2.1 (High-throughput Stored Procedure execution)
- Database: Microsoft SQL Server 2022
- Security: ASP.NET Core Identity with role-based authorization and rate limiting
- Frontend: Razor Views (.cshtml), Tailwind CSS, Vanilla JavaScript
- Containerization: Docker and Docker Compose

## Project Structure

```
MELA/
├── .github/
│   └── workflows/
│       ├── ci.yml
│       └── deploy.yml
├── .gitattributes
├── .gitignore
├── docker-compose.yml
├── Dockerfile
├── MelaFair.slnx
├── MelaFair.Core/
│   ├── Constants/
│   │   ├── RoleConstants.cs
│   │   └── SqlErrorCodes.cs
│   └── Enums/
│       ├── JobApplicationStatus.cs
│       ├── PaymentStatus.cs
│       └── TicketStatus.cs
└── MelaFair.Web/
    ├── Controllers/
    │   ├── AccountController.cs
    │   ├── AdminController.cs
    │   ├── EmployeeController.cs
    │   ├── FairController.cs
    │   ├── HomeController.cs
    │   ├── VendorController.cs
    │   └── VisitorController.cs
    ├── Data/
    │   ├── ApplicationDbContext.cs
    │   └── DbInitializer.cs
    ├── Database/
    │   ├── StoredProcedures/
    │   ├── Triggers/
    │   └── Views/
    ├── Migrations/
    ├── Models/
    │   ├── Entities/
    │   └── ViewModels/
    ├── Repositories/
    │   ├── Implementations/
    │   └── Interfaces/
    ├── Services/
    ├── Views/
    └── wwwroot/
```

## Prerequisites

- .NET 8.0 SDK or later
- Microsoft SQL Server 2022 (Local instance or Docker container)
- Docker Desktop (Optional, for containerized execution)

## Setup and Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/Turjo101365/MELA.git
   cd MELA
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Configure connection string:
   Update `appsettings.json` or configure an environment variable for `DefaultConnection`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost,1433;Database=MelaFairDb;User Id=sa;Password=YourStrongPassword!;TrustServerCertificate=True;MultipleActiveResultSets=true;"
   }
   ```

4. Configure email credentials (Optional, for password reset):
   Populate environment variables or user secrets matching `.env.example`:
   ```bash
   Email__Host=smtp.example.com
   Email__Port=587
   Email__UseSsl=true
   Email__UserName=your-username
   Email__Password=your-password
   Email__FromAddress=no-reply@example.com
   Email__PublicBaseUrl=http://localhost:5243
   ```

5. Apply database initialization and migrations:
   Database tables, SQL stored procedures, views, triggers, and default roles/seed data are automatically deployed on application start via `DbInitializer`. Alternatively, run:
   ```bash
   dotnet ef database update --project MelaFair.Web
   ```

6. Run the application:
   ```bash
   dotnet run --project MelaFair.Web
   ```

   Or run with Docker Compose:
   ```bash
   docker compose up -d --build
   ```

## Usage and Routes

- Home: `/` (Overview of active cultural fairs and upcoming festivals)
- Explore Fairs: `/Fair` (List of scheduled events and festival details)
- Vendor Stalls: `/Vendor/Marketplace` (Booth lease marketplace and interactive floorplans)
- Vendor Dashboard: `/Vendor/Dashboard` (Booked stalls and staffing management)
- Visitor Passes: `/Visitor/BrowseFairs` (Ticket purchase and daily quota availability)
- Visitor My Passes: `/Visitor/MyTickets` (Purchased passes and verification codes)
- Staff Jobs: `/Employee/JobListings` (Active event employment opportunities)
- Employee Applications: `/Employee/MyApplications` (Application history tracking)
- Admin Panel: `/Admin/Dashboard` (Event operations, sales analytics, and capacity metrics)
- Authentication: `/Account/Login`, `/Account/Register`, `/Account/ForgotPassword`

Default Seed Accounts for Development:
- Admin: Configurable via `Seed:AdminEmail` and `Seed:AdminPassword`
- Vendor: `vendor@mela.com` / `Vendor@123456`
- Visitor: `visitor@mela.com` / `Visitor@123456`
- Employee: `employee@mela.com` / `Employee@123456`

## Screenshots

<!-- Add interface screenshots here -->
- Home and festival catalog
- Stall leasing and floorplan selector
- Visitor ticket booking and checkout
- Administrator operations dashboard

## Future Improvements

- Payment Gateway Integration: Support for online payment processors (bKash, Nagad, SSLCommerz, Stripe) beyond simulated checkouts.
- QR Code Scanning App: Mobile camera turnstile scanner for gate stewards to validate visitor ticket codes in real time.
- Dynamic Stall Floorplans: Vector-based interactive SVG/Canvas stall map builder for custom pavilion geometry.
- Automated Email Dispatcher: Transition from `System.Net.Mail.SmtpClient` to MailKit with template support for ticket PDFs and receipts.
- Comprehensive Unit and Integration Tests: Expanding test coverage for stored procedure edge cases, trigger rollbacks, and concurrent booking stress tests.

## Author and License

- Author: Turjo (GitHub: [@Turjo101365](https://github.com/Turjo101365))
- License: This project is licensed under the MIT License.
