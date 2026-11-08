# Office Facility Maintenance Management System

> A web-based internal platform for managing office equipment maintenance across departments, technicians, vendors, and staff. It centralizes requests, assignment, status tracking, SLA monitoring, vendor dispatch, automated email notifications, comments, history, and management reporting into one auditable workflow.

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)]()
[![React 19](https://img.shields.io/badge/React-19-61DAFB)]()
[![SQL Server 2022](https://img.shields.io/badge/SQL%20Server-2022-D83B01)]()
[![Status](https://img.shields.io/badge/status-active-success)]()

## About

Office Facility Maintenance Management System is an internal maintenance platform built for an office environment. It is designed for office assets such as printers, routers, air conditioners, meeting-room devices, and other shared equipment that need structured maintenance handling.

The system helps:

- staff report issues in one place and monitor resolution timelines
- managers assign work to primary and support technicians with better visibility
- technicians update progress in a controlled workflow with real-time SLA countdowns
- technicians dispatch equipment to external vendor partners when internal repair is not possible, with SLA paused automatically during the external service period
- administrators maintain users, departments, equipment, and vendor partner directories
- the organization track SLA compliance, vendor costs, and technician performance through built-in analytics and exportable reports

## Business Context

Before this system, maintenance requests were typically handled through scattered channels such as phone calls, email, chat tools, or direct messages. That approach makes it easy to miss requests, duplicate work, and lose operational history.

This project replaces that fragmented process with a centralized workflow so the team can:

- capture every request consistently with automated SLA targets based on priority
- track who created, assigned, supported, and resolved each ticket
- enforce mandatory resolution notes and cancellation reasons
- monitor SLA deadlines and trigger automated email alerts when tickets are near breach or overdue
- connect each maintenance request to a department and an equipment item
- dispatch equipment to external vendor partners with automatic SLA pause and repair cost tracking
- review ticket history and comments later for audit and reporting
- export comprehensive management reports covering technician performance, SLA compliance, and maintenance cost analysis

## Current Scope

The repository currently contains:

- an ASP.NET Core Web API backend
- a React + Vite frontend
- JWT authentication with refresh tokens
- role-based access control (Admin, Manager, Staff, Technician)
- department, equipment, user, ticket, ticket attachment, vendor, and dashboard modules
- SLA calculation, real-time status badges, and background breach monitoring worker
- SMTP email alert service for SLA near-breach, breach, escalation, and ticket completion
- vendor dispatch workflow with SLA pause / resume and repair cost logging
- reporting & analytics module with technician performance, SLA compliance, and maintenance cost reports exportable to Excel
- pre-commit code formatting automation with Husky and lint-staged
- seeded demo data for local development

The frontend currently includes:

- `Login`
- `Dashboard`
- `Tickets`
- `Equipment`
- `Users`
- `Departments`
- `Vendors`
- `Reports & Analytics`

## Key Features

### Authentication

- JWT login
- current user profile lookup
- password change
- refresh token rotation
- logout via refresh-token revocation

### User Management

- admin-only user directory
- role-aware access
- department assignment
- active/inactive status control
- reset password support
- new Admin accounts are not created from the user-management form; existing Admin accounts can still be maintained

### Department Management

- create, update, read, delete departments
- unique department name protection
- dependency checks before deletion
- `IsMaintenanceTeam` flag to mark specialized maintenance departments

### Equipment Management

- create, update, read, delete equipment
- equipment assigned to a department
- immutable equipment code after creation
- support for office asset tracking

### Maintenance Tickets

- create maintenance requests with priority-based SLA deadlines
- view ticket queue and detail
- assign primary technician and optional support technician
- change ticket status through workflow rules
- cancel tickets with mandatory cancellation reason modal
- add comments and discussion threads
- track status history and audit log
- store resolution notes, cancellation reasons, and resolution timestamps

### SLA Management & Automated Escalation

- automated target resolution duration based on priority:
  - **Critical**: 2 hours
  - **High**: 8 hours
  - **Medium**: 24 hours (1 day)
  - **Low**: 48 hours (2 days)
- real-time SLA countdown timers and color-coded status badges on client UI (`InSLA`, `NearBreach`, `Breached`, `MetSLA`, `MissedSLA`, `Paused`, `Cancelled`)
- background `SlaMonitorWorker` running periodically to evaluate open tickets against SLA deadlines
- automated near-breach warnings (< 1 hour remaining) and breach detection
- automated escalation alerts when overdue duration exceeds threshold

### Email Notifications & Alerts

- SMTP integration using MailKit / MimeKit (`SmtpEmailService`)
- near-breach warning emails sent to assigned technicians
- SLA breach alerts sent to department managers
- escalation emails sent to system administrators for severely overdue tickets
- SLA outcome notification emails sent upon ticket completion (`MetSLA` / `MissedSLA`)

### Ticket Attachments & Storage

- presigned file upload flow (Cloudflare R2 / S3 / Local storage compatible)
- list attachments per ticket
- preview modal for images, videos, and PDF documents
- secure download link generation
- attachment deletion with role/owner permissions

### Vendor Management

- create and manage external vendor partner records (name, contact person, phone, email, address)
- active/inactive status toggle per vendor
- vendor directory accessible by Admin and Manager roles
- vendors selectable when dispatching equipment for external repair

### Vendor Dispatch & SLA Pause

- technicians can transition a ticket to `WaitingForVendor` status to dispatch equipment to an external partner
- when dispatched, the ticket SLA timer is automatically **paused** (`SlaStatus = Paused`) so the waiting period does not count against the internal team's SLA
- the dispatch log records the selected vendor, estimated return date, technician notes, and actual repair cost (`RepairCost`)
- when the equipment is returned, the technician resumes work (`InProgress`), and the SLA timer continues from the paused point
- repair cost data feeds directly into the maintenance cost analytics and Excel reports

### Dashboard & Analytics

- real-time KPI overview (tickets by status, active vs maintenance equipment, technician and department metrics)
- interactive status and priority distribution charts
- department equipment breakdown
- role-scoped data filtering

### Reporting & Analytics (Admin / Manager)

Three analytical pillars accessible from the `/reports` page:

**Technician Performance**
- total assigned, in-progress, waiting-for-vendor, resolved counts per technician
- SLA compliance rate and missed SLA count per technician
- average resolution time (MTTR) in hours
- bar chart comparison of SLA compliance rates across the team

**SLA Compliance**
- overall compliance rate across all tickets
- breakdown by priority (Critical / High / Medium / Low)
- monthly SLA compliance trend line chart (last 6 months)
- counts for active tickets: InSLA, NearBreach, Breached, Paused

**Maintenance Cost**
- total vendor repair spend, total vendor dispatches, and average cost per dispatch
- monthly cost trend bar chart (last 6 months)
- cost breakdown by department (pie chart)
- top 5 costliest equipment items (replacement candidates)

**Export**
- **Excel (.xlsx)**: one-click export generating a single workbook with all three report sheets
- **PDF**: browser print dialog for on-screen printing or PDF generation
- **Filters**: date range (with future-date validation), department, and technician filters

### Theme & UI Polish

- Light and Dark mode toggle with system preference support
- responsive tables, modal dialogs, SLA badges, and status indicators

### Code Quality & Git Hooks

- Husky pre-commit hook integration
- lint-staged automated formatting with Prettier and lint checking with ESLint prior to code commits

## UI & Features Screenshots

### 1. Dashboard & Analytics
Overview of maintenance tickets, KPI summaries, and equipment status.
![Dashboard UI](./clients/public/screenshots/dashboard.png)
![Dashboard UI](./clients/public/screenshots/dashboard2.png)

### 2. Ticket Management
List of maintenance tickets with color-coded status badges, priority indicators, and SLA tracking.
![Ticket Details](./clients/public/screenshots/equipment1.png)

![Ticket Details](./clients/public/screenshots/equipment2.png)

### 3. Ticket Details & Workflow
Detailed view of a ticket showing status transitions, assignee, support technician, SLA countdown, and discussion thread.

![Ticket Details](./clients/public/screenshots/equipment3.png)

![Ticket Details](./clients/public/screenshots/equipment4.png)

![Ticket Details](./clients/public/screenshots/equipment5.png)

![Ticket Details](./clients/public/screenshots/equipment6.png)

![Ticket Details](./clients/public/screenshots/equipment7.png)

![Ticket Details](./clients/public/screenshots/equipment8.png)

![Ticket Details](./clients/public/screenshots/equipment9.png)

![Ticket Details](./clients/public/screenshots/equipment10.png)

![Ticket Details](./clients/public/screenshots/equipment11.png)

![Ticket Details](./clients/public/screenshots/equipment12.png)

![Ticket Details](./clients/public/screenshots/equipment13.png)

![Ticket Details](./clients/public/screenshots/equipment14.png)

![Ticket Details](./clients/public/screenshots/equipment15.png)

![Ticket Details](./clients/public/screenshots/equipment16.png)

![Ticket Details](./clients/public/screenshots/equipment17.png)

![Ticket Details](./clients/public/screenshots/equipment18.png)

![Ticket Details](./clients/public/screenshots/equipment19.png)

### 4. Equipment Management
Asset tracking interface with department assignment and active/maintenance status toggle.
![Equipment Management](./clients/public/screenshots/equipment20.png)

![Equipment Management](./clients/public/screenshots/equipment21.png)

![Equipment Management](./clients/public/screenshots/equipment22.png)

### 5. User Management & Access Control
Admin directory for managing users, roles (Admin/Manager/Staff/Technician), and department assignments.
![User Management](./clients/public/screenshots/user1.png)

![User Management](./clients/public/screenshots/user2.png)

![User Management](./clients/public/screenshots/user3.png)

![User Management](./clients/public/screenshots/user4.png)

![User Management](./clients/public/screenshots/user5.png)

## Business Workflow

```mermaid
flowchart TB
    A[Staff] --> B[Create Ticket with SLA Deadline]
    B --> C[Pending]
    C --> D[Manager Assign Technician & Support Tech]
    D --> E[Assigned]
    E --> F[Technician Starts Work]
    F --> G[In Progress]
    G -->|Internal Fix| H[Resolved with Resolution Note & SLA Check]
    G -->|Needs External Repair| V[WaitingForVendor - SLA Paused]
    V -->|Equipment Returned| G
    H --> I[Staff Confirm]
    I --> J[Closed]
    C -. Cancel with Reason .-> K[Cancelled]
    E -. Cancel with Reason .-> K
```

## Access Model

The backend uses role-based access control:

- `Admin`
- `Manager`
- `Staff`
- `Technician`

Ticket visibility is filtered by role:

- `Admin` can access all tickets
- `Manager` can access tickets in their department
- `Staff` can access tickets they created
- `Technician` can access tickets assigned to them (as primary or support technician) or created by them

Report access is restricted to `Admin` and `Manager` roles only. Managers see data scoped to their department; Admins see the full organization view.

## Tech Stack

**Frontend:** React 19, TypeScript, Vite, TanStack Query, React Router, Zustand, React Hook Form, Zod, Recharts
**Backend:** ASP.NET Core 10, Entity Framework Core, JWT Authentication, BCrypt.Net-Next, MailKit / MimeKit (SMTP), ClosedXML (Excel export), BackgroundService (`SlaMonitorWorker`), Swagger/OpenAPI
**Database:** Microsoft SQL Server 2022
**Tooling:** Docker Compose, pnpm, ESLint, Prettier, Husky, lint-staged, Vitest

## Repository Structure

```text
InternalMaintenanceManagement.slnx
|
+-- .husky/                                # Git hook automation
|   +-- pre-commit                         # Pre-commit hook triggering lint-staged & Prettier
|
+-- InternalMaintenance.Api/               # ASP.NET Core 10 Web API Backend
|   +-- Constants/                         # Domain constants & policies
|   |   +-- SlaPolicy.cs                   # SLA threshold & status definitions
|   +-- Data/                              # Data access & database context
|   |   +-- AppDbContext.cs                # EF Core DbContext definition
|   |   +-- DbInitializer.cs               # Startup database migrations & demo data seeders
|   +-- Extensions/
|   |   +-- ServiceCollectionExtensions.cs # DI registrations
|   +-- Modules/                           # Feature-based API Modules (Modular Monolith)
|   |   +-- Auth/
|   |   +-- Dashboard/
|   |   +-- Departments/
|   |   +-- Equipment/
|   |   +-- Reports/                       # Reporting & analytics with Excel export
|   |   |   +-- Contracts/                 # DTOs: ReportFilterQuery, TechnicianPerformanceReportResponse, etc.
|   |   |   +-- Services/                  # ReportExportService (ClosedXML Excel generation)
|   |   |   +-- ReportsController.cs       # Endpoints for performance, SLA, cost & export
|   |   +-- TicketAttachments/
|   |   +-- Tickets/
|   |   +-- Users/
|   |   +-- Vendors/                       # Vendor partner CRUD & active/inactive toggle
|   +-- Services/                          # Core business services & background tasks
|   |   +-- SlaMonitorWorker.cs            # BackgroundService checking SLA breach & escalation
|   |   +-- SmtpEmailService.cs            # MailKit/MimeKit SMTP email sender
|   +-- Program.cs
|   +-- appsettings.json
|
+-- clients/                               # React 19 + TypeScript + Vite Frontend
|   +-- src/
|   |   +-- features/
|   |   |   +-- reports/                   # Reports API hooks (reports-api.ts)
|   |   |   +-- vendors/                   # Vendor table, modals & mutations
|   |   +-- pages/
|   |   |   +-- reports/                   # Reporting & Analytics page screen
|   |   |   +-- vendors/                   # Vendor partner management page screen
|   |   +-- entities/vendor/               # Vendor entity models
|
+-- docker-compose.yml
+-- .env.example
+-- README.md
```

## API Modules

### Authentication

- `POST /api/auth/login`
- `GET /api/auth/me`
- `POST /api/auth/change-password`
- `POST /api/auth/refresh-token`
- `POST /api/auth/logout`

### Users

- `GET /api/users`
- `GET /api/users/{id}`
- `POST /api/users`
- `PUT /api/users/{id}`
- `PATCH /api/users/{id}/status`
- `PATCH /api/users/{id}/reset-password`

### Departments

- `GET /api/departments`
- `GET /api/departments/{id}`
- `POST /api/departments`
- `PUT /api/departments/{id}`
- `DELETE /api/departments/{id}`

### Equipment

- `GET /api/equipment`
- `GET /api/equipment/{id}`
- `POST /api/equipment`
- `PUT /api/equipment/{id}`
- `DELETE /api/equipment/{id}`

### Vendors

- `GET /api/vendors`
- `GET /api/vendors/{id}`
- `POST /api/vendors`
- `PUT /api/vendors/{id}`
- `PATCH /api/vendors/{id}/toggle-active`

### Dashboard

- `GET /api/dashboard/summary`
- `GET /api/dashboard/charts`

### Ticket Attachments

- `POST /api/tickets/{ticketId}/attachments/presign`
- `POST /api/tickets/{ticketId}/attachments/confirm`
- `GET /api/tickets/{ticketId}/attachments`
- `GET /api/tickets/{ticketId}/attachments/{attachmentId}/download-url`
- `DELETE /api/tickets/{ticketId}/attachments/{attachmentId}`

### Maintenance Tickets

- `GET /api/tickets`
- `GET /api/tickets/{id}`
- `POST /api/tickets`
- `PUT /api/tickets/{id}`
- `PATCH /api/tickets/{id}/assign`
- `PATCH /api/tickets/{id}/status`
- `POST /api/tickets/{id}/comments`
- `GET /api/tickets/{id}/comments`
- `GET /api/tickets/{id}/history`

Ticket statuses: `Pending`, `Assigned`, `InProgress`, `WaitingForVendor`, `Resolved`, `Closed`, `Cancelled`

Ticket priorities: `Low`, `Medium`, `High`, `Critical`

SLA statuses: `InSLA`, `NearBreach`, `Breached`, `Paused`, `MetSLA`, `MissedSLA`, `Cancelled`

### Reports & Analytics (Admin / Manager only)

- `GET /api/reports/technician-performance`
- `GET /api/reports/sla-compliance`
- `GET /api/reports/maintenance-cost`
- `GET /api/reports/export/excel`

Supported query parameters: `fromDate`, `toDate`, `departmentId`, `technicianId`

## Ticket Workflow Rules

- A ticket can be cancelled from `Pending` or `Assigned` (requires a `cancellationReason`)
- SLA deadline (`dueAt`) is automatically determined based on priority at creation
- A ticket can move from `InProgress` to `WaitingForVendor`; SLA is **paused** automatically
- A ticket can move from `WaitingForVendor` back to `InProgress`; SLA **resumes**
- A ticket can move from `InProgress` to `Resolved` (requires a `resolutionNote`)
- Moving to `Resolved` or `Closed` checks completion against `dueAt` to record `MetSLA` or `MissedSLA`
- Closed tickets should not be modified
- Every status change is stored in history

## Seeded Demo Data

Seeded users (password: `Temp@123456`):

- `admin@test.com`
- `manager@test.com`
- `staff@test.com`
- `technician@test.com`

Seeded equipment: `PRN-ACC-001` (Canon Printer), `RTR-IT-001` (Main Office Router)

## Getting Started

### Prerequisites

- .NET 10 SDK
- Node.js LTS + pnpm
- Docker Desktop

### Start the Database

```powershell
docker compose up -d
```

### Start the API

```powershell
dotnet run --project InternalMaintenance.Api
```

API: `http://localhost:5253` | Swagger: `http://localhost:5253/swagger`

### Start the Frontend

```powershell
cd clients
pnpm install
pnpm dev
```

Frontend: `http://localhost:5173`

## Business Rules

- One equipment item cannot have multiple active maintenance tickets
- Equipment code is immutable after creation
- A department cannot be deleted if related users or equipment still exist
- SLA timer is paused when a ticket enters `WaitingForVendor` and resumed when it returns to `InProgress`
- Report endpoints reject date inputs in the future; `fromDate` must not exceed `toDate`
- Report data is scoped by role: Admins see all, Managers see their department only

## Feature Implementation Status

| Feature / Module | Status | Backend | Frontend | Description |
| --- | --- | --- | --- | --- |
| **JWT Auth & Refresh Token** | Completed | Yes | Yes | Login, logout, token rotation, current user lookup |
| **Role-Based Access (RBAC)** | Completed | Yes | Yes | Admin, Manager, Staff, Technician authorization |
| **User Management** | Completed | Yes | Yes | User directory, active status toggle, reset password |
| **Department Management** | Completed | Yes | Yes | Department CRUD, `IsMaintenanceTeam` flag |
| **Equipment Management** | Completed | Yes | Yes | Asset tracking, department link, immutable code |
| **Maintenance Tickets** | Completed | Yes | Yes | Create ticket, primary & support technician assignment, status workflow |
| **Ticket Cancellation Reason** | Completed | Yes | Yes | Required cancellation reason modal & detail display |
| **SLA Tracking & Breach Alerts** | Completed | Yes | Yes | Priority SLA deadlines, UI timers/badges, background monitor worker |
| **Email Notifications** | Completed | Yes | Yes | SMTP alerts for Near-Breach, SLA Breach, Escalation & Completion |
| **Ticket Comments & History** | Completed | Yes | Yes | Discussion thread, automated status audit log |
| **Ticket Attachments & Files** | Completed | Yes | Yes | Presigned upload, image/video/PDF preview, deletion |
| **Dashboard & Analytics** | Completed | Yes | Yes | Summary KPIs, status & priority charts, dept equipment stats |
| **Vendor Management** | Completed | Yes | Yes | Vendor CRUD, active/inactive toggle, contact directory |
| **Vendor Dispatch & SLA Pause** | Completed | Yes | Yes | WaitingForVendor workflow, SLA pause/resume, repair cost logging |
| **Reporting & Analytics** | Completed | Yes | Yes | Technician performance, SLA compliance, maintenance cost reports |
| **Excel Export** | Completed | Yes | Yes | ClosedXML multi-sheet workbook with all three report dimensions |
| **Dark / Light Theme** | Completed | N/A | Yes | Theme mode switcher with persistent UI state |
| **Pre-commit Formatting (Husky)** | Completed | N/A | Yes | Git pre-commit hooks with lint-staged & Prettier |
| **Equipment QR Code** | Planned | No | No | Generate & scan QR codes for rapid asset lookup |
| **Preventive Maintenance** | Planned | No | No | Scheduled recurring maintenance tasks & automatic tickets |
| **Push / In-App Notifications** | Planned | No | No | Real-time alerts via SignalR |
| **Multi-building / Location** | Planned | No | No | Location hierarchy (Building -> Floor -> Room) |
| **Multi-tenancy & SaaS** | Planned | No | No | Organization isolation & multi-tenant billing |

## Roadmap

### Phase 1 — Core Operational (Completed)

- [x] JWT Authentication & Refresh Tokens
- [x] Role-Based Access Control
- [x] User, Department & Equipment Management
- [x] Maintenance Ticket Lifecycle
- [x] SLA Tracking, Background Monitor Worker & Email Alerts
- [x] Ticket Comments, History & File Attachments
- [x] Dashboard KPIs & Analytics Charts
- [x] Husky Pre-commit Formatting

### Phase 2 — Vendor & External Repair (Completed)

- [x] Vendor Management (CRUD, active/inactive, contact directory)
- [x] Vendor Dispatch Workflow (`WaitingForVendor` status)
- [x] Automatic SLA Pause on dispatch and Resume on return
- [x] Repair cost logging per vendor dispatch (`RepairCost`)

### Phase 3 — Reporting & Analytics (Completed)

- [x] Technician Performance Report (assigned, resolved, MTTR, SLA compliance rate)
- [x] SLA Compliance Report (overall rate, by-priority breakdown, 6-month trend)
- [x] Maintenance Cost Report (total vendor spend, monthly trend, dept breakdown, top costly equipment)
- [x] One-click Excel export (3 sheets in one workbook)
- [x] PDF print export via browser
- [x] Date range, department, and technician filters with future-date validation

### Phase 4 — Asset Lifecycle & Preventive Maintenance (Planned)

- [ ] **Equipment QR Code**: Generate printable QR code labels per equipment item; enable camera-scan lookup on mobile web
- [ ] **Preventive Maintenance Scheduling**: Define recurring inspection schedules per equipment; automatically generate tickets before the due date
- [ ] **Maintenance History Timeline**: Equipment-level view of all past tickets, repairs, and costs
- [ ] **Warranty Tracking**: Store warranty expiry per equipment and surface warnings when creating tickets

### Phase 5 — Collaboration & Real-Time Features (Planned)

- [ ] **Push / In-App Notifications**: Real-time alerts inside the app when a ticket is assigned, commented on, or SLA is breached (SignalR)
- [ ] **@Mention in Comments**: Tag a colleague in a comment to trigger a notification
- [ ] **Technician Workload View**: Visual board showing current open ticket load per technician
- [ ] **Mobile-Responsive PWA**: Progressive Web App support for field technicians

### Phase 6 — Location & Physical Hierarchy (Planned)

- [ ] **Location Hierarchy**: Building -> Floor -> Room model so equipment can be pinned to a physical location
- [ ] **Location-Based Ticket Routing**: Suggest the nearest available technician based on room location

### Longer-Term Ideas

- [ ] **Multi-Tenant Architecture**: Data isolation for multiple organizations within a single platform instance
- [ ] **Subscription & Billing Model**: Usage tier management and invoicing
- [ ] **Predictive Maintenance & Forecasting**: Analyze repair history to forecast equipment failure rates

## Contributing

Pull requests are welcome. For larger changes, please open an issue first so scope and direction can be aligned.

## License

No license file is currently included in this repository.
