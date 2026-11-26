# Multi-stage Dockerfile cho InternalMaintenance.Api (build context: thư mục gốc repo)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
# Cài curl để Docker healthcheck gọi /health định kỳ
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution và toàn bộ .csproj trước để tận dụng Docker layer cache cho dotnet restore
COPY ["InternalMaintenanceManagement.slnx", "./"]
COPY ["src/Host/InternalMaintenance.Api/InternalMaintenance.Api.csproj", "src/Host/InternalMaintenance.Api/"]
COPY ["src/Shared/InternalMaintenance.BuildingBlocks/InternalMaintenance.BuildingBlocks.csproj", "src/Shared/InternalMaintenance.BuildingBlocks/"]
COPY ["src/Shared/InternalMaintenance.Persistence/InternalMaintenance.Persistence.csproj", "src/Shared/InternalMaintenance.Persistence/"]
COPY ["src/Modules/Equipment/InternalMaintenance.Equipment.Contracts/InternalMaintenance.Equipment.Contracts.csproj", "src/Modules/Equipment/InternalMaintenance.Equipment.Contracts/"]
COPY ["src/Modules/Equipment/InternalMaintenance.Equipment.Infrastructure/InternalMaintenance.Equipment.Infrastructure.csproj", "src/Modules/Equipment/InternalMaintenance.Equipment.Infrastructure/"]
COPY ["src/Modules/Identity/InternalMaintenance.Identity.Contracts/InternalMaintenance.Identity.Contracts.csproj", "src/Modules/Identity/InternalMaintenance.Identity.Contracts/"]
COPY ["src/Modules/Identity/InternalMaintenance.Identity.Infrastructure/InternalMaintenance.Identity.Infrastructure.csproj", "src/Modules/Identity/InternalMaintenance.Identity.Infrastructure/"]
COPY ["src/Modules/Maintenance/InternalMaintenance.Maintenance.Domain/InternalMaintenance.Maintenance.Domain.csproj", "src/Modules/Maintenance/InternalMaintenance.Maintenance.Domain/"]
COPY ["src/Modules/Maintenance/InternalMaintenance.Maintenance.Contracts/InternalMaintenance.Maintenance.Contracts.csproj", "src/Modules/Maintenance/InternalMaintenance.Maintenance.Contracts/"]
COPY ["src/Modules/Maintenance/InternalMaintenance.Maintenance.Application/InternalMaintenance.Maintenance.Application.csproj", "src/Modules/Maintenance/InternalMaintenance.Maintenance.Application/"]
COPY ["src/Modules/Maintenance/InternalMaintenance.Maintenance.Infrastructure/InternalMaintenance.Maintenance.Infrastructure.csproj", "src/Modules/Maintenance/InternalMaintenance.Maintenance.Infrastructure/"]
COPY ["src/Modules/Organization/InternalMaintenance.Organization.Contracts/InternalMaintenance.Organization.Contracts.csproj", "src/Modules/Organization/InternalMaintenance.Organization.Contracts/"]
COPY ["src/Modules/Organization/InternalMaintenance.Organization.Infrastructure/InternalMaintenance.Organization.Infrastructure.csproj", "src/Modules/Organization/InternalMaintenance.Organization.Infrastructure/"]
COPY ["src/Modules/Reporting/InternalMaintenance.Reporting.Contracts/InternalMaintenance.Reporting.Contracts.csproj", "src/Modules/Reporting/InternalMaintenance.Reporting.Contracts/"]
COPY ["src/Modules/Reporting/InternalMaintenance.Reporting.Infrastructure/InternalMaintenance.Reporting.Infrastructure.csproj", "src/Modules/Reporting/InternalMaintenance.Reporting.Infrastructure/"]
COPY ["src/Modules/TicketAttachments/InternalMaintenance.TicketAttachments.Contracts/InternalMaintenance.TicketAttachments.Contracts.csproj", "src/Modules/TicketAttachments/InternalMaintenance.TicketAttachments.Contracts/"]
COPY ["src/Modules/TicketAttachments/InternalMaintenance.TicketAttachments.Infrastructure/InternalMaintenance.TicketAttachments.Infrastructure.csproj", "src/Modules/TicketAttachments/InternalMaintenance.TicketAttachments.Infrastructure/"]
COPY ["src/Modules/Vendors/InternalMaintenance.Vendors.Contracts/InternalMaintenance.Vendors.Contracts.csproj", "src/Modules/Vendors/InternalMaintenance.Vendors.Contracts/"]
COPY ["src/Modules/Vendors/InternalMaintenance.Vendors.Infrastructure/InternalMaintenance.Vendors.Infrastructure.csproj", "src/Modules/Vendors/InternalMaintenance.Vendors.Infrastructure/"]

RUN dotnet restore "src/Host/InternalMaintenance.Api/InternalMaintenance.Api.csproj"

COPY ["src/", "src/"]
WORKDIR "/src/src/Host/InternalMaintenance.Api"
RUN dotnet publish "InternalMaintenance.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "InternalMaintenance.Api.dll"]
