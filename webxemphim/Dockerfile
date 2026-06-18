# ── Stage 1: Build ────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj và restore trước (tận dụng Docker layer cache)
COPY ["webxemphim.csproj", "./"]
RUN dotnet restore "webxemphim.csproj"

# Copy toàn bộ source rồi publish
COPY . .
RUN dotnet publish "webxemphim.csproj" -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ───────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Nhận PORT từ Railway (Railway inject biến môi trường PORT)
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "webxemphim.dll"]
