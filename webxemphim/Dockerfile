# ── Stage 1: Build ────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj và restore (build context = root repo, app nằm trong webxemphim/)
COPY ["webxemphim/webxemphim.csproj", "webxemphim/"]
RUN dotnet restore "webxemphim/webxemphim.csproj"

# Copy toàn bộ source rồi publish
COPY . .
RUN dotnet publish "webxemphim/webxemphim.csproj" -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ───────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Railway inject PORT tự động
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "webxemphim.dll"]
