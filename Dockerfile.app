FROM node:24-bookworm-slim AS frontend-build
WORKDIR /src

COPY ["package.json", "pnpm-lock.yaml", "pnpm-workspace.yaml", "./"]
COPY ["src/frontend/apesdb/package.json", "src/frontend/apesdb/"]
COPY ["src/frontend/common/package.json", "src/frontend/common/"]
COPY ["src/frontend/ui/package.json", "src/frontend/ui/"]

RUN corepack enable && corepack prepare pnpm@11.17.0 --activate && pnpm install --frozen-lockfile --trust-lockfile

COPY ["nx.json", "tsconfig.base.json", "./"]
COPY ["src/frontend/apesdb", "src/frontend/apesdb"]
COPY ["src/frontend/common", "src/frontend/common"]
COPY ["src/frontend/ui", "src/frontend/ui"]

RUN pnpm build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ApesDb.slnx", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["global.json", "./"]
COPY ["src/backend/ApesDb.Api/ApesDb.Api.csproj", "src/backend/ApesDb.Api/"]
COPY ["src/backend/ApesDb.Auth/ApesDb.Auth.csproj", "src/backend/ApesDb.Auth/"]
COPY ["src/backend/ApesDb.Common/ApesDb.Common.csproj", "src/backend/ApesDb.Common/"]
COPY ["src/backend/ApesDb.Domain/ApesDb.Domain.csproj", "src/backend/ApesDb.Domain/"]
COPY ["src/backend/ApesDb.Shared/ApesDb.Shared.csproj", "src/backend/ApesDb.Shared/"]

RUN dotnet restore "src/backend/ApesDb.Api/ApesDb.Api.csproj"

COPY ["src/backend/ApesDb.Api", "src/backend/ApesDb.Api"]
COPY ["src/backend/ApesDb.Auth", "src/backend/ApesDb.Auth"]
COPY ["src/backend/ApesDb.Common", "src/backend/ApesDb.Common"]
COPY ["src/backend/ApesDb.Domain", "src/backend/ApesDb.Domain"]
COPY ["src/backend/ApesDb.Shared", "src/backend/ApesDb.Shared"]

RUN dotnet publish "src/backend/ApesDb.Api/ApesDb.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

COPY --from=build /app/publish .
COPY --from=frontend-build /src/src/frontend/apesdb/dist ./wwwroot

ENTRYPOINT ["dotnet", "ApesDb.Api.dll"]
