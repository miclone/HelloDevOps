# ---------- Stage 1: build ----------
# The SDK image is big (compilers, NuGet, etc.) - we only use it to build.
FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /src

# Copy only the project file first and restore.
# Docker caches this layer, so packages are NOT re-downloaded
# unless the .csproj changes. Faster builds.
COPY src/HelloDevOps.Api/HelloDevOps.Api.csproj src/HelloDevOps.Api/
RUN dotnet restore src/HelloDevOps.Api/HelloDevOps.Api.csproj

# Now copy the rest of the source and publish.
COPY src/ src/
RUN dotnet publish src/HelloDevOps.Api/HelloDevOps.Api.csproj \
    -c Release -o /app/publish --no-restore

# ---------- Stage 2: runtime ----------
# Small runtime-only image. This is what actually ships.
FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# .NET images listen on port 8080 by default and include a non-root user.
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "HelloDevOps.Api.dll"]
