<div align="center">

# 🏢 Office Facility Maintenance Management System

**Enterprise-grade internal facility & equipment maintenance platform — Built as a Vertical Slice Modular Monolith (.NET 10 & React 19).**  
*Hệ thống quản lý bảo trì trang thiết bị văn phòng tập trung — Xây dựng theo kiến trúc Vertical Slice Modular Monolith (.NET 10 & React 19).*

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![React 19](https://img.shields.io/badge/React-19-61DAFB?style=flat-square&logo=react&logoColor=black)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-6.0-3178C6?style=flat-square&logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![SQL Server 2022](https://img.shields.io/badge/SQL%20Server-2022-CC2927?style=flat-square&logo=microsoftsqlserver)](https://www.microsoft.com/sql-server)
[![Cloudflare R2](https://img.shields.io/badge/Storage-Cloudflare%20R2-F38020?style=flat-square&logo=cloudflare&logoColor=white)](https://developers.cloudflare.com/r2/)
[![Resend](https://img.shields.io/badge/Email-Resend%20API-000000?style=flat-square&logo=resend&logoColor=white)](https://resend.com/)
[![Status](https://img.shields.io/badge/Status-Active-22C55E?style=flat-square)]()

[Introduction](#-introduction--giới-thiệu) • [Features](#-key-features--tính-năng-chính) • [Screenshots](#-ui-showcase--hình-ảnh-giao-diện) • [Architecture](#-architecture-overview--kiến-trúc-tổng-quan) • [Installation](#-installation--cài-đặt) • [Running](#-running-the-project--chạy-project) • [Env Config](#-environment-configuration--cấu-hình-môi-trường) • [Roadmap](#-roadmap--lộ-trình-phát-triển)

</div>

---

## 📖 Introduction / Giới thiệu

**EN:** **Office Facility Maintenance Management System** is an internal operations platform designed to centralize and automate the entire lifecycle of office equipment maintenance (printers, routers, air conditioners, meeting-room devices, and shared assets). It replaces fragmented channels (phone calls, emails, chat messages) with a single auditable workflow featuring automated SLA tracking, external vendor dispatch, real-time dashboards, and executive reporting.

**VI:** **Office Facility Maintenance Management System** là nền tảng quản lý bảo trì nội bộ giúp doanh nghiệp chuẩn hóa và tự động hóa toàn bộ vòng đời xử lý sự cố thiết bị văn phòng (máy in, router mạng, điều hòa, thiết bị phòng họp, tài sản dùng chung). Hệ thống thay thế các kênh rời rạc (điện thoại, email, tin nhắn) bằng một quy trình tập trung, minh bạch, có kiểm toán (audit trail), giám sát SLA tự động, điều phối nhà cung cấp ngoài và báo cáo điều hành.

### Role-Based Access Model / Mô hình Phân quyền (RBAC)

| Role / Vai trò | English Description | Mô tả Tiếng Việt |
| :--- | :--- | :--- |
| **Admin** | Full system access: manage users, departments, equipment, vendors, all tickets, and organization-wide analytics. | Toàn quyền hệ thống: quản lý người dùng, phòng ban, thiết bị, nhà cung cấp, toàn bộ ticket và báo cáo toàn công ty. |
| **Manager** | Department-scoped management: assign primary & support technicians, track department tickets, manage vendors, and view department reports. | Quản lý phạm vi phòng ban: phân công kỹ thuật viên chính/hỗ trợ, theo dõi ticket phòng ban, quản lý vendor và xem báo cáo phòng ban. |
| **Technician** | Execute assigned repairs (primary or support), update workflow status, dispatch to external vendors (`WaitingForVendor`), upload evidence, and log resolution notes. | Tiếp nhận và xử lý ticket được giao (chính hoặc hỗ trợ), cập nhật trạng thái, gửi sửa chữa ngoài (`WaitingForVendor`), đính kèm minh chứng và ghi nhận kết quả. |
| **Staff** | Submit maintenance requests, monitor real-time SLA progress, collaborate via ticket comments, and confirm ticket closure (`Closed`). | Tạo yêu cầu bảo trì, theo dõi tiến độ SLA theo thời gian thực, trao đổi qua bình luận và xác nhận đóng ticket (`Closed`). |

---

## ✨ Key Features / Tính năng chính

### 1. Authentication & Access Control / Xác thực & Bảo mật
- **EN:** JWT authentication with Refresh Token rotation & revocation, Google OAuth 2.0 Single Sign-On (SSO), password policy enforcement, and role-based access policies (`TicketAccessPolicy`).
- **VI:** Xác thực JWT kết hợp xoay vòng & thu hồi Refresh Token, đăng nhập một lần qua **Google OAuth 2.0 SSO**, kiểm soát mật khẩu và phân quyền chặt chẽ ở cả API lẫn UI.

### 2. Maintenance Tickets & SLA Workflow / Quản lý Ticket & SLA Tự động
- **EN:** Controlled state machine (`Pending` → `Assigned` → `InProgress` ⇄ `WaitingForVendor` → `Resolved` → `Closed`, or `Cancelled` with mandatory reason). Automated SLA deadlines by priority (`Critical`: 2h, `High`: 8h, `Medium`: 24h, `Low`: 48h) with real-time countdowns, status badges (`InSLA`, `NearBreach`, `Breached`, `Paused`, `MetSLA`, `MissedSLA`), comment threads, and immutable audit history.
- **VI:** Luồng trạng thái chặt chẽ, bắt buộc ghi chú hoàn tất (`ResolutionNote`) hoặc lý do hủy (`CancellationReason`). Tự động tính hạn SLA theo mức độ ưu tiên (`Critical`: 2h, `High`: 8h, `Medium`: 24h, `Low`: 48h), hiển thị đếm ngược real-time, luồng bình luận và lịch sử thay đổi trạng thái.

### 3. Vendor Dispatch & SLA Pause / Điều phối Nhà cung cấp & Tạm dừng SLA
- **EN:** Dispatch equipment to external repair partners (`WaitingForVendor`) to automatically **pause** the SLA timer (`Paused`), log estimated return dates and `RepairCost`, and **resume** SLA calculation upon return (`InProgress`).
- **VI:** Khi gửi thiết bị ra ngoài sửa chữa (`WaitingForVendor`), hệ thống tự động **tạm dừng đồng hồ SLA**, lưu thông tin đối tác, ngày dự kiến trả và chi phí sửa chữa (`RepairCost`), sau đó tự động **tiếp tục tính SLA** khi nhận lại thiết bị.

### 4. Automated Alerts via Outbox & Resend / Cảnh báo Email qua Outbox & Resend
- **EN:** `SlaMonitorWorker` periodically evaluates active tickets using high-performance `ExecuteUpdateAsync`, enqueues notifications into `SlaNotificationOutboxMessage`, and dispatches HTML alerts via **Resend HTTP API** for near-breach (< 1h), breach, manager escalation, and completion.
- **VI:** `SlaMonitorWorker` định kỳ quét các ticket đang mở, đẩy thông báo vào bảng **Outbox (`SlaNotificationOutboxMessage`)** và gửi email cảnh báo qua **Resend HTTP API** khi sắp quá hạn (< 1 giờ), vi phạm SLA, leo thang quản lý hoặc hoàn tất.

### 5. Ticket Attachments on Cloudflare R2 / Tệp đính kèm trên Cloudflare R2
- **EN:** Direct browser-to-storage uploads via **Cloudflare R2 Presigned URLs**, in-app preview modal for images, videos, and PDFs, plus a background cleanup worker for unconfirmed/orphaned uploads.
- **VI:** Upload file trực tiếp từ trình duyệt lên **Cloudflare R2** bằng **Presigned URL**, hỗ trợ xem trước ảnh, video, PDF ngay trên giao diện và có worker dọn dẹp file rác định kỳ.

### 6. Dashboard & Executive Reporting / Dashboard & Báo cáo Phân tích
- **EN:** Role-scoped KPI dashboards and 3 analytical pillars (Admin/Manager): **Technician Performance** (MTTR, compliance rate), **SLA Compliance** (6-month trends, priority breakdown), and **Maintenance Cost** (vendor spend, top 5 costliest assets). Exportable to multi-sheet **Excel (.xlsx)** via ClosedXML or **PDF** print.
- **VI:** Dashboard KPI theo thời gian thực và 3 báo cáo chuyên sâu: **Hiệu suất kỹ thuật viên (MTTR)**, **Tuân thủ SLA (xu hướng 6 tháng)** và **Chi phí bảo trì (Top 5 thiết bị tốn kém nhất)**. Hỗ trợ xuất **Excel (.xlsx)** 3 sheet bằng ClosedXML và in **PDF**.

---

## 📸 UI Showcase / Hình ảnh giao diện

### 1. Dashboard & Analytics / Tổng quan & Phân tích

| Overview Dashboard / Tổng quan KPI | Charts & Breakdown / Biểu đồ phân tích |
| :---: | :---: |
| ![Dashboard Overview](./clients/public/screenshots/dashboard.png) | ![Dashboard Charts](./clients/public/screenshots/dashboard2.png) |

### 2. Ticket Queue, Detail & Workflow / Danh sách, Chi tiết & Luồng xử lý Ticket

| Ticket Queue / Danh sách Ticket | Ticket Detail / Chi tiết Ticket |
| :---: | :---: |
| ![Ticket Queue 1](./clients/public/screenshots/equipment1.png) | ![Ticket Queue 2](./clients/public/screenshots/equipment2.png) |
| ![Ticket Detail 1](./clients/public/screenshots/equipment3.png) | ![Ticket Detail 2](./clients/public/screenshots/equipment4.png) |

<details>
<summary>🖼️ View all Ticket Workflow & Attachment screenshots (15 images) / Xem toàn bộ ảnh quy trình Ticket & Đính kèm (15 ảnh)</summary>

| | | |
| :---: | :---: | :---: |
| ![Ticket 5](./clients/public/screenshots/equipment5.png) | ![Ticket 6](./clients/public/screenshots/equipment6.png) | ![Ticket 7](./clients/public/screenshots/equipment7.png) |
| ![Ticket 8](./clients/public/screenshots/equipment8.png) | ![Ticket 9](./clients/public/screenshots/equipment9.png) | ![Ticket 10](./clients/public/screenshots/equipment10.png) |
| ![Ticket 11](./clients/public/screenshots/equipment11.png) | ![Ticket 12](./clients/public/screenshots/equipment12.png) | ![Ticket 13](./clients/public/screenshots/equipment13.png) |
| ![Ticket 14](./clients/public/screenshots/equipment14.png) | ![Ticket 15](./clients/public/screenshots/equipment15.png) | ![Ticket 16](./clients/public/screenshots/equipment16.png) |
| ![Ticket 17](./clients/public/screenshots/equipment17.png) | ![Ticket 18](./clients/public/screenshots/equipment18.png) | ![Ticket 19](./clients/public/screenshots/equipment19.png) |

</details>

### 3. Equipment Management / Quản lý Thiết bị

| Equipment Directory / Danh mục Thiết bị | Asset Details / Chi tiết Tài sản | Asset Status / Trạng thái Thiết bị |
| :---: | :---: | :---: |
| ![Equipment 20](./clients/public/screenshots/equipment20.png) | ![Equipment 21](./clients/public/screenshots/equipment21.png) | ![Equipment 22](./clients/public/screenshots/equipment22.png) |

### 4. User Management & Access Control / Quản lý Người dùng & Phân quyền

| User Directory / Danh sách Người dùng | Create & Edit User / Tạo & Sửa User |
| :---: | :---: |
| ![User 1](./clients/public/screenshots/user1.png) | ![User 2](./clients/public/screenshots/user2.png) |

<details>
<summary>🖼️ View additional User Management screens (3 images) / Xem thêm màn hình Quản lý Người dùng (3 ảnh)</summary>

| | | |
| :---: | :---: | :---: |
| ![User 3](./clients/public/screenshots/user3.png) | ![User 4](./clients/public/screenshots/user4.png) | ![User 5](./clients/public/screenshots/user5.png) |

</details>

---

## 🏛️ Architecture Overview / Kiến trúc tổng quan

**EN:** The backend follows a **Vertical Slice Modular Monolith** architecture across **7 isolated Bounded Contexts** (`Identity`, `Organization`, `Equipment`, `Maintenance`, `Vendors`, `TicketAttachments`, `Reporting`). Cross-module communication is strictly routed through `*.Contracts` (zero cross-module domain entity navigation), enforced automatically by `Architecture.Tests`.

**VI:** Backend được tổ chức theo kiến trúc **Vertical Slice Modular Monolith** gồm **7 Bounded Contexts độc lập**. Giao tiếp liên module bắt buộc đi qua `*.Contracts` (không tham chiếu chéo Entity Domain) và được kiểm soát tự động bằng bộ test **`Architecture.Tests`**.

### System Architecture Diagram / Sơ đồ Kiến trúc Hệ thống

```mermaid
flowchart TB
    Client["React 19 + TypeScript SPA<br/>(Vite, TanStack Query, Zustand, Recharts)"]
    R2["Cloudflare R2<br/>(Object Storage)"]
    Resend["Resend HTTP API<br/>(Email Alerts)"]
    Google["Google OAuth 2.0<br/>(SSO)"]

    subgraph APIHost ["ASP.NET Core 10 Host (InternalMaintenance.Api)"]
        Controllers["Thin Controllers & Minimal Endpoints"]
        Workers["Background Workers<br/>(SlaMonitorWorker, Outbox, Cleanup)"]
    end

    subgraph Modules ["7 Bounded Context Modules (Vertical Slices)"]
        M1["Identity<br/>(Auth, Users, Roles)"]
        M2["Organization<br/>(Departments)"]
        M3["Equipment<br/>(Assets)"]
        M4["Maintenance<br/>(Tickets, SLA, Workflow)"]
        M5["Vendors<br/>(External Partners)"]
        M6["TicketAttachments<br/>(Presign & Confirm)"]
        M7["Reporting<br/>(Analytics & Excel)"]
    end

    subgraph Shared ["Shared Kernel & Persistence"]
        BB["BuildingBlocks<br/>(ServiceResult, Pagination, Security)"]
        DB["Persistence<br/>(AppDbContext + Outbox)"]
    end

    SQL[("SQL Server 2022")]

    Client -->|REST / JWT| Controllers
    Client -->|Presigned PUT/GET| R2
    Client -->|ID Token| Google
    Controllers --> Modules
    Workers --> M4
    Workers --> M6
    Workers -->|Send Email| Resend
    M6 -->|Generate Presign URL| R2
    Modules --> BB
    Modules --> DB
    DB --> SQL
```

### Ticket & SLA Lifecycle / Vòng đời Ticket & Trạng thái SLA

```mermaid
stateDiagram-v2
    [*] --> Pending: Staff creates Ticket (SLA DueAt calculated)
    Pending --> Assigned: Manager assigns Technician
    Pending --> Cancelled: Cancelled (Reason required)
    Assigned --> InProgress: Technician starts work
    Assigned --> Cancelled: Cancelled (Reason required)
    InProgress --> WaitingForVendor: Dispatch to Vendor (SLA Paused)
    WaitingForVendor --> InProgress: Equipment returned (SLA Resumed)
    InProgress --> Resolved: Fixed (ResolutionNote + MetSLA/MissedSLA)
    Resolved --> Closed: Staff confirms closure
    Closed --> [*]
    Cancelled --> [*]
```

### Tech Stack / Công nghệ sử dụng

| Layer / Tầng | Technologies / Công nghệ |
| :--- | :--- |
| **Backend** | .NET 10, ASP.NET Core Web API, Entity Framework Core, ClosedXML, DotNetEnv, BCrypt.Net-Next, Swagger/OpenAPI |
| **Frontend** | React 19, TypeScript 6, Vite 8, TanStack Query v5, TanStack Table v8, React Router v7, Zustand v5, React Hook Form, Zod, Recharts, Tailwind CSS, Radix UI |
| **Database** | Microsoft SQL Server 2022 (Docker Container) |
| **Cloud & Integrations** | Cloudflare R2 (S3-compatible Presigned Storage), Resend HTTP API (Email), Google OAuth 2.0 (SSO) |
| **Testing & Quality** | xUnit (`Architecture.Tests`, `Maintenance.Domain.Tests`, `Reporting.Tests`, `Api.IntegrationTests`), Vitest, MSW, Husky, lint-staged, ESLint, Prettier |

---

## 🛠️ Installation / Cài đặt

### Prerequisites / Yêu cầu hệ thống

- **[.NET 10 SDK](https://dotnet.microsoft.com/download)**
- **[Node.js LTS](https://nodejs.org/)** & **[pnpm](https://pnpm.io/)** (`v10+`)
- **[Docker Desktop](https://www.docker.com/products/docker-desktop/)**

### Step 1: Clone & Configure Environment / Clone & Tạo file cấu hình

```bash
git clone https://github.com/<your-username>/internal-maintenance-management.git
cd internal-maintenance-management

# Backend & Docker environment variables
cp .env.example .env

# Frontend environment variables
cp clients/.env.example clients/.env
```

### Step 2: Install Dependencies / Cài đặt thư viện

```bash
# Restore .NET solution dependencies
dotnet restore InternalMaintenanceManagement.slnx

# Install Frontend packages & initialize Husky Git hooks
cd clients
pnpm install
cd ..
```

---

## 🚀 Running the Project / Chạy project

### 1. Start SQL Server 2022 / Khởi động Database

```bash
# Create external volume once on a fresh machine / Tạo volume nếu chạy lần đầu
docker volume create internal-maintenance-management_internal_sqlserver_data_dev

# Start SQL Server container
docker compose up -d
```

### 2. Start Backend API / Khởi động Backend API

```bash
dotnet run --project src/Host/InternalMaintenance.Api
```

- **API URL**: `http://localhost:5253`
- **Swagger UI**: `http://localhost:5253/swagger`
- *(Development mode automatically applies EF Core Migrations and seeds demo data / Môi trường Dev tự động chạy Migration và nạp dữ liệu mẫu.)*

### 3. Start Frontend SPA / Khởi động Frontend

Open a new terminal / Mở terminal mới:

```bash
cd clients
pnpm dev
```

- **Frontend URL**: `http://localhost:5173`

### 4. Run Test Suites / Chạy Kiểm thử

```bash
# Run all Backend tests (Architecture, Domain, Reporting, Integration)
dotnet test InternalMaintenanceManagement.slnx

# Run Frontend unit tests
cd clients
pnpm test:run
```

### 🔑 Seeded Demo Accounts / Tài khoản Demo

Default password for all seeded development accounts / Mật khẩu chung: **`Temp@123456`**

| Role / Vai trò | Email | Description / Mô tả |
| :--- | :--- | :--- |
| **Admin** | `admin@test.com` | System administrator / Quản trị viên toàn hệ thống |
| **Manager** | `manager@test.com` | Department manager / Quản lý phòng ban |
| **Technician** | `technician@test.com` | Maintenance technician / Kỹ thuật viên |
| **Staff** | `staff@test.com` | General staff / Nhân viên tạo yêu cầu |

*(Seeded equipment / Thiết bị mẫu: `PRN-ACC-001` — Canon Printer, `RTR-IT-001` — Main Office Router)*

---

## ⚙️ Environment Configuration / Cấu hình môi trường

### Backend & Docker (`.env` at root / tại thư mục gốc)

Loaded automatically by `DotNetEnv` on API startup / Được nạp tự động qua `DotNetEnv` khi khởi chạy API.

| Group / Nhóm | Variable / Biến | Description (EN / VI) | Example / Giá trị mẫu |
| :--- | :--- | :--- | :--- |
| **Database** | `MSSQL_SA_PASSWORD` | SQL Server `sa` password / Mật khẩu `sa` | `YourStrong!ProductionPassword123` |
| | `SQLSERVER_PORT` | Host port mapped to container / Cổng SQL Server | `1433` |
| | `ConnectionStrings__DefaultConnection` | EF Core SQL Server connection string / Chuỗi kết nối DB | `Server=127.0.0.1,1433;Database=InternalMaintenanceDb;...` |
| **JWT** | `Jwt__Key` | Signing secret (min 32 chars) / Khóa bí mật ký JWT | `replace-with-a-random-secret-key-...` |
| | `Jwt__Issuer` | Token issuer / Bên phát hành token | `InternalMaintenanceApi` |
| | `Jwt__Audience` | Token audience / Bên nhận token | `InternalMaintenanceClient` |
| | `Jwt__ExpiresInMinutes` | Access token lifetime (mins) / Thời hạn Access Token | `60` |
| | `Jwt__RefreshTokenExpiresInDays` | Refresh token lifetime (days) / Thời hạn Refresh Token | `7` |
| **CORS** | `Cors__AllowedOrigins__0` | Allowed frontend origins (`0`, `1`, ...) / Origin được phép | `http://localhost:5173` |
| **Email (Resend)** | `Resend__ApiKey` | Resend HTTP API key / API Key dịch vụ Resend | `re_xxxxxxxxxxxxxxxxxxxxxxxxxxxx` |
| | `Resend__FromEmail` | Verified sender address / Email người gửi | `no-reply@mail.shopmini.io.vn` |
| | `Resend__FromName` | Sender display name / Tên hiển thị người gửi | `Maintenance System` |
| **Cloudflare R2** | `R2__AccountId` | Cloudflare Account ID | `your_cloudflare_account_id` |
| | `R2__BucketName` | R2 bucket name / Tên bucket lưu file | `internal-maintenance-attachments` |
| | `R2__AccessKeyId` | R2 S3 Access Key ID | `your_r2_access_key_id` |
| | `R2__SecretAccessKey` | R2 S3 Secret Access Key | `your_r2_secret_access_key` |
| | `R2__Endpoint` | R2 S3 API endpoint | `https://<account_id>.r2.cloudflarestorage.com` |
| | `R2__Region` | R2 region (`auto`) / Vùng lưu trữ | `auto` |
| | `R2__PresignExpiresMinutes` | Presigned URL TTL (mins) / Thời hạn link upload/tải | `15` |
| **Google SSO** | `Google__ClientId` | Google OAuth 2.0 Client ID | `your_id.apps.googleusercontent.com` |

### Frontend (`clients/.env`)

```env
# Backend API Base URL
VITE_API_BASE_URL=http://localhost:5253/api

# Google OAuth 2.0 Client ID (must match Google__ClientId in backend)
VITE_GOOGLE_CLIENT_ID=your_google_client_id.apps.googleusercontent.com
```

---

## 📁 Project Structure / Cấu trúc thư mục

```text
InternalMaintenanceManagement.slnx
├── .github/workflows/                 # CI/CD pipelines (backend-ci, frontend-ci, dotnet)
├── .husky/                            # Git hooks (pre-commit running lint-staged & Prettier)
├── clients/                           # Frontend SPA (React 19 + TypeScript + Vite)
│   ├── public/screenshots/            # Application UI screenshots
│   └── src/
│       ├── app/                       # Router, Providers, global store setup
│       ├── components/                # Shared UI components (Radix UI / Tailwind)
│       ├── entities/                  # Client-side domain models (ticket, vendor, ...)
│       ├── features/                  # Feature slices (auth, tickets, reports, vendors, ...)
│       ├── pages/                     # Route pages (Dashboard, Tickets, Reports, ...)
│       └── shared/                    # Axios client, OpenAPI generated types, helpers
├── deploy/                            # Production deployment scripts & configs
├── docs/                              # Architecture plans, gap analyses & refactor logs
├── sql/                               # Database maintenance & repair SQL scripts
├── src/
│   ├── Host/
│   │   └── InternalMaintenance.Api/   # Composition Root, Controllers, Migrations, Seed
│   ├── Modules/                       # 7 Independent Bounded Contexts (Vertical Slices)
│   │   ├── Equipment/                 # Contracts + Infrastructure (Features/)
│   │   ├── Identity/                  # Contracts + Infrastructure (Features/Auth, Features/Users)
│   │   ├── Maintenance/               # Contracts + Domain + Application + Infrastructure
│   │   ├── Organization/              # Contracts + Infrastructure (Features/)
│   │   ├── Reporting/                 # Contracts + Infrastructure (Features/ + ClosedXML)
│   │   ├── TicketAttachments/         # Contracts + Infrastructure (Features/ + R2 Storage)
│   │   └── Vendors/                   # Contracts + Infrastructure (Features/)
│   └── Shared/
│       ├── InternalMaintenance.BuildingBlocks/  # ServiceResult, PagedResponse, ClaimsPrincipalExt
│       └── InternalMaintenance.Persistence/     # AppDbContext, Configurations, Outbox
├── tests/                             # Automated Test Suites
│   ├── Api.IntegrationTests/          # End-to-end API integration tests
│   ├── Architecture.Tests/            # Module boundary & dependency rule enforcement
│   ├── Maintenance.Domain.Tests/      # SLA & Ticket Workflow domain unit tests
│   └── Reporting.Tests/               # Reporting & analytics unit tests
├── docker-compose.yml                 # SQL Server 2022 for Development
├── docker-compose.prod.yml            # Resource-tuned SQL Server 2022 for Production
└── .env.example                       # Environment variables template
```

---

## 🤝 Contributing / Hướng dẫn đóng góp

**EN:** Contributions are welcome! For major architectural changes, please open an issue first to discuss scope and design.  
**VI:** Mọi đóng góp đều được hoan nghênh! Với các thay đổi lớn về kiến trúc, vui lòng mở issue trước để thảo luận.

1. **Create a feature branch / Tạo nhánh mới**:
   ```bash
   git checkout -b feat/your-feature-name
   ```
2. **Follow Vertical Slice & Module Boundaries / Tuân thủ ranh giới kiến trúc**:
   - Place new use cases inside `Features/` of the target module. / Đặt tính năng mới trong thư mục `Features/` của module tương ứng.
   - Never reference another module's Domain entities directly; communicate via `*.Contracts`. / Giao tiếp liên module qua `*.Contracts`, không tham chiếu chéo Entity Domain.
   - Ensure `dotnet test InternalMaintenanceManagement.slnx` (especially `Architecture.Tests`) passes 100%.
3. **Code Formatting / Chuẩn hóa Code**:
   - Frontend commits are automatically checked & formatted by **Husky + lint-staged + Prettier**.
   - Backend code follows the root `.editorconfig`.

---

## 📄 License / Giấy phép

**EN:** No `LICENSE` file is currently included in this repository. The codebase is maintained for internal facility management and technical showcase purposes. Please contact the repository owner before redistributing or using it commercially.

**VI:** Hiện tại repository **chưa đính kèm file `LICENSE`**. Mã nguồn đang được phát triển cho mục đích quản lý bảo trì nội bộ và trình diễn kỹ thuật. Vui lòng liên hệ chủ sở hữu repository trước khi phân phối lại hoặc sử dụng cho mục đích thương mại.

---

## 🗺️ Roadmap / Lộ trình phát triển

### Completed / Đã hoàn thành

- [x] **Phase 1 — Core Operational**: JWT Auth, Google SSO, RBAC, User/Department/Equipment CRUD, Ticket Lifecycle, SLA Monitor Worker, Comments & Audit History.
- [x] **Phase 2 — Vendor & External Repair**: Vendor directory, `WaitingForVendor` dispatch workflow, automatic SLA Pause/Resume, and `RepairCost` tracking.
- [x] **Phase 3 — Reporting & Analytics**: Technician Performance (MTTR), SLA Compliance trends, Maintenance Cost breakdown, multi-sheet **Excel (.xlsx)** export, and PDF print.
- [x] **Phase 4 — Architecture & Reliability**: Full refactoring into a **Vertical Slice Modular Monolith** across 7 modules, **Outbox Pattern** with Resend Email, **Cloudflare R2 Presigned URLs** with background cleanup, and automated `Architecture.Tests`.

### Upcoming / Kế hoạch tiếp theo

- [ ] **Phase 5 — Asset Lifecycle & Preventive Maintenance**:
  - Printable **QR Code** labels & mobile camera scan for instant ticket creation / Sinh và quét QR Code thiết bị.
  - Recurring **Preventive Maintenance** schedules & warranty expiry alerts / Lập lịch bảo trì định kỳ & theo dõi bảo hành.
- [ ] **Phase 6 — Real-time & Collaboration**:
  - Real-time in-app notifications via **SignalR**, `@mention` in comments, and mobile **PWA** support for field technicians / Thông báo real-time SignalR & giao diện PWA.
- [ ] **Phase 7 — Location & Scale**:
  - Physical location hierarchy (**Building → Floor → Room**) & **Multi-tenancy** architecture / Mô hình vị trí vật lý & kiến trúc đa tổ chức.


### Technical TODO / Ghi chú kỹ thuật

- [ ] **TicketCode collision**: TicketCode có xác suất va chạm thấp, cân nhắc SEQUENCE nếu log cho thấy. (Hiện không retry; lỗi trùng `TicketCode` nổi lên thành exception thay vì bị báo là `DuplicateRace`.)
