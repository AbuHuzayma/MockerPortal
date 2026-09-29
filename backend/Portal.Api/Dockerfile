# Build context is the repository root (see .gitlab-ci.yml / docs/13-deployment.md §5):
#   docker build -f backend/Portal.Api/Dockerfile .
#
# Only backend/ is copied in — Portal.sln also references ../tests, but the
# runtime image only ever builds/publishes Portal.Api.csproj directly, so the
# test projects (and their extra restore time) never enter this image.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/Portal.Domain/Portal.Domain.csproj backend/Portal.Domain/
COPY backend/Portal.Application/Portal.Application.csproj backend/Portal.Application/
COPY backend/Portal.Infrastructure/Portal.Infrastructure.csproj backend/Portal.Infrastructure/
COPY backend/Portal.Integrations/Portal.Integrations.csproj backend/Portal.Integrations/
COPY backend/Portal.Api/Portal.Api.csproj backend/Portal.Api/
RUN dotnet restore backend/Portal.Api/Portal.Api.csproj

COPY backend/ backend/
RUN dotnet publish backend/Portal.Api/Portal.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# docs/03-security.md §2 rule 9 / CLAUDE.md §6.9: no production connection
# path exists anywhere in this image — only DEV/QA/PREPROD are ever
# configured, via Environment__Name at deploy time (see deploy/openshift).
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Matches OpenShift's default restricted SCC: containers run as an arbitrary
# non-root UID with no assumption of write access outside /app, and may not
# bind ports below 1024.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Portal.Api.dll"]
