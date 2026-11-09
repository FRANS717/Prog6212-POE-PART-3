# RaceDay.Web (Part 3 MVC front end) container image.
#
# Multi-stage build: the large .NET SDK image restores, builds and publishes the app, and only the published
# output is copied into the small ASP.NET runtime image. The final image has no SDK, no source code and runs
# as the non-root "app" user that Microsoft's images provide.
#
# Build:  docker build -t raceday-web .
# Run:    docker run -d -p 8080:8080 --name raceday-web raceday-web
# Open:   http://localhost:8080   (the Part 2 API must be running on the host at http://localhost:5043)

# ---------- Stage 1: build and publish ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project file and NuGet config first, so the restore layer is cached until dependencies change.
COPY NuGet.config ./
COPY src/RaceDay.Web/RaceDay.Web.csproj src/RaceDay.Web/
RUN dotnet restore src/RaceDay.Web/RaceDay.Web.csproj

# Now copy the rest of the MVC source and publish a Release build.
COPY src/RaceDay.Web/ src/RaceDay.Web/
RUN dotnet publish src/RaceDay.Web/RaceDay.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---------- Stage 2: runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# The MVC app calls the API that runs on the host machine. host.docker.internal is Docker Desktop's name for
# the host. The browser loads any locally stored images from the API directly, so it uses localhost.
# Both can be changed at run time with -e Api__BaseUrl=... and -e Api__PublicBaseUrl=...
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    Api__BaseUrl=http://host.docker.internal:5043 \
    Api__PublicBaseUrl=http://localhost:5043

COPY --from=build /app/publish .

# Run as the unprivileged user built into the official image, not as root.
USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
