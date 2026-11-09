# RaceDay - PROG6212 POE

[![Build and Test](https://github.com/FRANS717/Prog6212-POE-PART-2/actions/workflows/dotnet-build-and-test.yml/badge.svg?branch=part-3)](https://github.com/FRANS717/Prog6212-POE-PART-2/actions/workflows/dotnet-build-and-test.yml)

RaceDay is an event management platform for South African road running, walking and cycling events. Organisers create events, categories and results. Participants browse events, enrol by category and track their results.

This branch (`part-3`) holds the **complete system**: the Part 2 RESTful API, and the Part 3 MVC web application that consumes it, with Azure Blob Storage for images and a Docker container for the MVC app. The Part 1 planning documents are in `docs/`.

| Part | Status | Where |
|------|--------|-------|
| Part 1 - Planning (ERD, endpoint plan, SQL script) | Done | `docs/` |
| Part 2 - RESTful API (EF Core, sessions, Swagger, unit tests, CI) | Done on `part-2` | `src/RaceDay.Api`, `tests/RaceDay.Api.Tests` |
| Part 3 - MVC front end, Azure Blob Storage, Docker | Done on `part-3` | `src/RaceDay.Web`, `tests/RaceDay.Web.Tests`, `Dockerfile` |

## Solution Structure
```
RaceDay.slnx                 Visual Studio solution (open this)
RaceDay.slnLaunch            Start profile "API + Web (Part 3)": runs both projects with F5
Dockerfile, .dockerignore    Container image for the MVC app (Part 3)
docs/                        Part 1: ERD, endpoint plan, SQL script, CI screenshots
src/RaceDay.Api/             Part 2: ASP.NET Core Web API, EF Core, sessions, Swagger, Azure Blob storage
src/RaceDay.Web/             Part 3: ASP.NET Core MVC front end (talks to the API only)
tests/RaceDay.Api.Tests/     xUnit tests for the API (51)
tests/RaceDay.Web.Tests/     xUnit tests for the MVC app (24)
.github/workflows/           CI: build and test everything, build the Docker image, validate docs
```

## The Two Roles
- **Organiser:** creates, edits and deletes their own events; manages their categories; views every enrolment for their events; sets enrolment status; captures and corrects finish times and positions. Registers with an organisation name and contact number.
- **Participant:** registers with a date of birth; browses events; enrols in an event by choosing a category; views their own enrolments and race history; updates their profile and picture.

Anyone can browse events and categories without logging in. Other endpoints return **401** without a session and **403** when the wrong role calls them.

## Part 3 - MVC Web Application

### How it fits together
```
Browser -> RaceDay.Web (MVC, port 5078, or 8080 in Docker) -> HTTP + API session cookie -> RaceDay.Api (port 5043) -> SQL Server
                                                                                                   \-> Azure Blob Storage (images)
```
- **No database access in the MVC app.** `RaceDay.Web` has no EF Core reference. Every page gets its data from `RaceDayApiClient`, a typed `HttpClient` with one method per API endpoint.
- **Sessions.** On login the MVC app calls `POST /api/auth/login`, keeps the API's `RaceDay.Session` cookie in its own server-side session (with the user ID, role and name) and sends that cookie on every API call. The API still enforces every role rule. The MVC `[RequireRole]` filter handles navigation: guests go to Log in, the wrong role goes to Access Denied. Logout ends both sessions. If the API session expires, the user is sent back to Log in with a message.
- **Errors.** API errors come back as friendly messages on the page. Unexpected errors and 404s show branded error pages, never a stack trace.

### Pages
| Role | Page | What it does |
|------|------|--------------|
| Everyone | Home | Upcoming events as cards, filter by type, location and date, option to include past events |
| Everyone | Event details | Banner, full details and categories |
| Guest | Log in / Join RaceDay | Register as Organiser (organisation and contact number) or Participant (date of birth) |
| Organiser | Dashboard | Totals, next-event countdown, every event with enrolment counts and upcoming dates |
| Organiser | Manage events | Create, edit and delete events (name, description, date, location, distance, type) with banner upload |
| Organiser | Categories | Add, edit and remove age or distance categories per event |
| Organiser | Enrolments | Everyone entered in an event with category and status; confirm, cancel or set to pending |
| Organiser | Results | Capture and publish finish times and positions after the event |
| Participant | Enter event | Pick a category on the event page; recorded immediately |
| Participant | My events | Every entry with a colour-coded status (green Confirmed, amber Pending, red Cancelled) |
| Participant | My results | Event, date, category, finish time and "47th out of 312 finishers" |
| Both | Profile | View and update details; Participants upload a profile picture |

### Azure Blob Storage
Images are stored by the **API**, never by the MVC app. The MVC app posts the file to `POST /api/events/{id}/upload-banner` or `POST /api/users/profile/picture`. The API validates it (JPG/PNG/WEBP, 5 MB, real image bytes) and `BlobFileStorage` uploads it to the `raceday-images` container with a random name. The blob URL is saved on the event or user row and the MVC pages display it.

The API uses Blob Storage when `AzureBlob:ConnectionString` is set; otherwise it falls back to local disk (`wwwroot/uploads`), which is what the unit tests and CI use. The connection string is a secret and is **not** in the repository. Set it once per machine:
```powershell
cd src/RaceDay.Api
dotnet user-secrets set "AzureBlob:ConnectionString" "<storage account connection string>"
dotnet user-secrets set "AzureBlob:ContainerName" "raceday-images"
```
To create the storage account: Azure portal > Storage accounts > Create (Standard LRS), turn on *Allow Blob anonymous access* so browsers can show the images, then copy the connection string from **Access keys**. The API creates the container on first upload.

### Docker
The MVC app runs in a Linux container built from the multi-stage `Dockerfile`: the SDK image publishes the app, and a small ASP.NET runtime image runs it as a non-root user on port 8080. Start the API on the host first, then:

```bash
docker build -t raceday-web .
docker run -d -p 8080:8080 --name raceday-web raceday-web
```
Open **http://localhost:8080**. Inside the container the app reaches the API at `http://host.docker.internal:5043`. To use another API address, add `-e Api__BaseUrl=http://<host>:<port>` to `docker run`. Stop it with `docker rm -f raceday-web`.

## Prerequisites
- **.NET 10 SDK** (`dotnet --version` should report 10.x)
- **SQL Server.** The default connection is SQL Server LocalDB, which comes with Visual Studio. On another machine, point the app at any SQL Server (see below).
- **Visual Studio 2022 (17.14 or later) or 2026** with the *ASP.NET and web development* workload. Open `RaceDay.slnx`, choose **API + Web (Part 3)** in the start drop-down, and press F5. If that entry is missing: right-click the solution > *Configure Startup Projects* > *Multiple startup projects* > set `RaceDay.Api` and `RaceDay.Web` to **Start**.
- **Docker Desktop** for the Part 3 container.

## Setup and Run
```bash
git clone https://github.com/FRANS717/Prog6212-POE-PART-2.git
cd Prog6212-POE-PART-2
git checkout part-3
dotnet run --project src/RaceDay.Api
```
In a second terminal:
```bash
dotnet run --project src/RaceDay.Web
```

- Web app: **http://localhost:5078**
- Swagger UI: **http://localhost:5043/swagger**

The first API run creates `RaceDayDB`, applies the EF Core migrations and adds the sample data. Images are saved to local disk until the Azure connection string is set (see Azure Blob Storage above).

Log in with `POST /api/auth/login` first. Swagger keeps the session cookie, so the protected endpoints work straight away.

### Using another machine or SQL Server
The connection string is `ConnectionStrings:RaceDayDb` in `src/RaceDay.Api/appsettings.json`. Any value there can be overridden with an environment variable, so you don't need to edit the file.

PowerShell:
```powershell
$env:ConnectionStrings__RaceDayDb = "Server=localhost\SQLEXPRESS;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet run --project src/RaceDay.Api
```

Bash:
```bash
export ConnectionStrings__RaceDayDb="Server=localhost,1433;Database=RaceDayDB;User Id=sa;Password=...;TrustServerCertificate=True;"
dotnet run --project src/RaceDay.Api
```

Other things to know on a new machine:
- **Port in use:** change the port with `--urls http://localhost:5050`. Swagger then moves to that port.
- **HTTPS profile:** `dotnet run --launch-profile https` needs a trusted dev certificate. Run `dotnet dev-certs https --trust` once.
- **Using the Part 1 SQL script instead:** running `docs/RaceDay-Schema.sql` in SSMS creates the same database. Don't also run the API against it, because the migrations would try to create tables that already exist. Drop the database first or use a new server.

## Seeded Accounts
The sample data matches the Part 1 SQL script. Every seeded account uses the password **`Password123!`**.

| Email | Role |
|-------|------|
| organiser1@raceday.com | Organiser (Cape Town Events) |
| organiser2@raceday.com | Organiser (Durban Sports) |
| participant1@raceday.com | Participant |
| participant2@raceday.com | Participant |
| participant3@raceday.com | Participant |

## Running the Unit Tests
```bash
dotnet test RaceDay.slnx
```
- **RaceDay.Api.Tests (51):** run the real API against an in-memory database, so they need no SQL Server. They cover registration and login for both roles, sessions, password hashing, event management and ownership, role enforcement (401 and 403), enrolments, results, categories, the Swagger document and the Azure Blob / local storage switch.
- **RaceDay.Web.Tests (24):** use a fake HTTP handler to check the MVC API client (login keeps the API session cookie, logged-in calls forward it, errors and an unreachable API become friendly messages, an expired session is detected), the role-based redirects, status colours, positions and image URLs.

## Endpoints
Full details are in `docs/endpoint-plan.pdf` and in Swagger UI.

| Method | Route | Role | Purpose |
|--------|-------|------|---------|
| POST | /api/auth/register | Public | Register an Organiser or Participant |
| POST | /api/auth/login | Public | Log in and start a session |
| POST | /api/auth/logout | Logged in | End the session |
| GET | /api/users/profile | Logged in | Get own profile |
| PUT | /api/users/profile | Logged in | Update own profile |
| POST | /api/users/profile/picture | Participant | Upload profile picture |
| GET | /api/events | Public | List events (filters: type, date, location, mine) |
| GET | /api/events/{id} | Public | Event details with categories |
| POST | /api/events | Organiser | Create event |
| PUT | /api/events/{id} | Organiser (owner) | Update event |
| DELETE | /api/events/{id} | Organiser (owner) | Delete event |
| POST | /api/events/{eventId}/upload-banner | Organiser (owner) | Upload banner image |
| GET | /api/events/{eventId}/categories | Public | List categories |
| POST | /api/events/{eventId}/categories | Organiser (owner) | Add category |
| PUT | /api/categories/{id} | Organiser (owner) | Update category |
| DELETE | /api/categories/{id} | Organiser (owner) | Delete category |
| GET | /api/enrolments | Logged in | Organiser: enrolments for their events. Participant: own enrolments |
| GET | /api/events/{eventId}/enrolments | Organiser (owner) | Enrolments for one event |
| POST | /api/events/{eventId}/enrol | Participant | Enrol in an event by category |
| PUT | /api/enrolments/{id}/status | Organiser (owner) | Set Pending, Confirmed or Cancelled |
| GET | /api/results/me | Participant | Own race history |
| GET | /api/events/{eventId}/results | Organiser (owner) | Results for one event |
| POST | /api/events/{eventId}/results | Organiser (owner) | Capture a finish time and position |
| PUT | /api/results/{id} | Organiser (owner) | Correct a result |

## Deviations from the Part 1 Plan
The API follows the approved plan. These differences were added or needed:

1. **`contactNumber`** is sent on Organiser register and profile update. The Organisers table requires it, and the plan's body didn't include it.
2. **`POST /api/users/profile/picture`** was added for the profile picture that Part 3 needs.
3. **`GET /api/events?mine=true`** was added so the Organiser dashboard can list only that Organiser's events.
4. **`PUT /api/users/profile`** returns **409** if the new email belongs to another account. The plan listed only 200 and 400.
5. **`DELETE /api/events/{id}`** also removes the event's categories, enrolments and results, in one transaction. Foreign keys would otherwise block the delete.
6. **`DELETE /api/categories/{id}`** returns **409** if participants are enrolled in it, so their enrolments aren't lost.
7. **Enrolment age check:** `POST /api/events/{eventId}/enrol` returns 400 when the participant's age on the event date is outside the category's range.
8. **Results** include `totalFinishers`, so the Participant view can show "47th out of 312".
9. **Errors** always return `{ "message": "..." }`.

## Design Notes
- **Schema:** `RaceDayDbContext` maps the tables, keys, unique indexes, check constraints, defaults and delete rules from the Part 1 SQL script. The initial migration in `Data/Migrations/` creates the same tables.
- **Sessions:** login stores the user ID and role on the server. `[RequireSession]` returns 401 with no session and 403 for the wrong role. Ownership checks return 403.
- **Passwords:** stored only as BCrypt hashes.
- **Files:** uploads go through `IFileStorage`. Part 2 saves them under `wwwroot/uploads` (git-ignored). Part 3 adds an Azure Blob implementation without changing the controllers.

## CI/CD
- `.github/workflows/dotnet-build-and-test.yml` builds the whole solution (API, MVC app and both test projects), runs all unit tests, then builds the MVC Docker image, on every push and pull request.
- `.github/workflows/validate.yml` checks that the Part 1 planning documents are in `docs/`.

**Part 3 green build:** *(after the first green run on `part-3`, save the screenshot as `docs/screenshots/ci-green-build-part3.png` and it appears here)*

![Part 3 green CI build](docs/screenshots/ci-green-build-part3.png)

**Part 2 green build:** https://github.com/FRANS717/Prog6212-POE-PART-2/actions/runs/38826787930

![Green CI build](docs/screenshots/ci-green-build.png)

## Video Presentation
- **Part 1 (planning):** https://youtu.be/RrkhZZGbrcM
- **Part 2 (API, authentication, Swagger, unit tests):** *(add the unlisted YouTube link before submitting)*
- **Part 3 (MVC pages, Blob Storage, Docker):** *(add the unlisted YouTube link before submitting)*

## AI Use Disclosure
AI tools (Claude) helped plan the structure, write parts of the code and tests, and draft this README. Every change was reviewed and run locally. The design choices are explained in the video.

## Student Declaration
I declare that this work is my own original work and all sources have been properly referenced.
Name: Brain Frans Mashabela
Student Number: ST10476451
Date: 02/09/2026
