# ScanVault – Study Guide

Everything you need to explain this project in an interview, in plain language. Read it top to bottom once, then open each file it points to.

---

## 1. The 30-second pitch

> "ScanVault is a small reality-capture data platform. Teams create sites, upload 3D scans or photos of them over time, view them in a browser 3D viewer, and pin notes at exact positions on the model. The backend is an ASP.NET Core Web API in C# with a layered design: controllers, services and repositories. Data lives in Azure SQL through Entity Framework Core, scan files live in Azure Blob Storage, and the React front end is served by the same Azure App Service. It has 25 xUnit tests and a GitHub Actions pipeline that builds, tests and deploys on every push."

Why this project for Hexagon: their Aura Reality platform manages reality-capture data (scans, point clouds, digital twins) in the cloud. ScanVault is a small version of that idea, built to learn the domain and the .NET/Azure stack.

---

## 2. Follow one request through the code

**"User uploads a 3D scan"** — this one walkthrough covers almost every interview question.

1. **React** (`frontend/src/components/UploadScanForm.jsx` → `api.js`) builds a `FormData` with the file, title and date, and sends `POST /api/sites/1/scans` as `multipart/form-data`.
2. **ASP.NET Core middleware pipeline** (`Program.cs`) receives it. Routing finds the matching controller action.
3. **Controller** (`Controllers/SitesController.cs` → `UploadScan`). `[ApiController]` validates the request automatically (title required, length limits) and returns **400** if it fails. The controller converts the `IFormFile` into a plain `ScanUpload` record and calls the service.
4. **Service** (`Services/ScanService.cs` → `UploadAsync`) holds the business rules:
   - Does the site exist? If not → `NotFoundException` → **404**.
   - Is the extension allowed (`.glb`, `.jpg`, `.png`)? Is the file empty or over 50 MB? → `BadRequestException` → **400**.
   - Saves the file through `IFileStorage` and gets back a unique key.
   - Saves the metadata row through the repository. If the database save fails, it deletes the file again so storage and SQL stay consistent.
5. **Storage** (`Storage/AzureBlobFileStorage.cs`) uploads the bytes to the `scans` container in Azure Blob Storage. Locally, `LocalFileStorage` writes to a folder instead.
6. **Repository** (`Repositories/ScanRepository.cs`) adds the `Scan` entity; **EF Core** turns `SaveChangesAsync()` into an SQL `INSERT`.
7. The controller returns **201 Created** with the new scan as JSON and a `Location` header.
8. Errors anywhere are caught by `Errors/GlobalExceptionHandler.cs` and returned as consistent JSON (`ProblemDetails`).

---

## 3. Project structure

```
src/ScanVault.Api/
  Controllers/   HTTP endpoints (thin: no business logic)
  Services/      Business rules
  Repositories/  Database queries (EF Core)
  Storage/       IFileStorage + Local and Azure Blob implementations
  Models/        Database entities: Site, Scan, Annotation (+ EntityBase)
  Dtos/          What the API sends/receives + mapping from entities
  Data/          DbContext (table configuration) + design-time factory for migrations
  Migrations/    EF Core migration that creates the SQL tables
  Errors/        Custom exceptions + global exception handler
  Extensions/    Dependency-injection setup and database start-up
  Program.cs     Builds the app: services + middleware pipeline
tests/ScanVault.Tests/
  Services/      Unit tests per service
  Api/           End-to-end HTTP tests
frontend/        React app (Vite)
infra/           Azure setup script
.github/workflows/ci-cd.yml   Build, test, deploy pipeline
```

---

## 4. Data model (SQL)

```
Site 1 ──── * Scan 1 ──── * Annotation
```

| Table | Key columns |
|---|---|
| Sites | Id, Name, Location, Description, CreatedAt |
| Scans | Id, **SiteId (FK)**, Title, CapturedAt, Format, ContentType, SizeBytes, **StorageKey**, CreatedAt |
| Annotations | Id, **ScanId (FK)**, Text, Category, X, Y, Z, CreatedAt |

- **One-to-many** relationships with **foreign keys**, configured in `ScanVaultDbContext.OnModelCreating`.
- **Cascade delete**: deleting a site deletes its scans and their annotations in the database. The service then deletes the files from storage.
- **Index** on `(SiteId, CapturedAt)` makes the "scan history, newest first" query fast.
- `StorageKey` is the blob's name. The file itself is **not** in SQL.

**Why not store files in SQL?** Databases are expensive and slow for large binary files. Blob Storage is cheap, built for big files, and can stream them. SQL keeps the small, searchable metadata. This is the standard pattern.

---

## 5. OOP in this project (they will ask about "OOPs")

| Concept | Where | One-line explanation |
|---|---|---|
| **Encapsulation** | Services hide their repositories and rules behind methods like `UploadAsync` | Callers can't skip validation because they can't reach the internals |
| **Abstraction** | `IFileStorage`, `IRepository<T>`, `IScanService` | Code depends on *what* something does, not *how* |
| **Inheritance** | `Site`, `Scan`, `Annotation` inherit `EntityBase`; `ScanRepository` inherits `Repository<Scan>` | Shared code (Id, CreatedAt, basic CRUD) is written once |
| **Polymorphism** | `LocalFileStorage` and `AzureBlobFileStorage` both implement `IFileStorage` | The service calls `SaveAsync` without knowing which one runs; config decides |
| **Generics** | `Repository<T> where T : EntityBase` | One class works for every entity type safely |

**SOLID, briefly:**
- **S**ingle responsibility: controller = HTTP, service = rules, repository = data.
- **D**ependency inversion: services depend on interfaces, and dependency injection supplies the real classes.

---

## 6. Key ASP.NET Core concepts

**Dependency injection (DI)** — `Extensions/ServiceCollectionExtensions.cs` registers interfaces with their implementations (`AddScoped<IScanService, ScanService>()`). ASP.NET Core then creates objects and passes them into constructors.
- *Scoped* = one instance per HTTP request (services, repositories, DbContext).
- *Singleton* = one for the whole app (file storage client).
- *Transient* = a new instance every time it's requested.

**Why DI?** Loose coupling. Tests swap in a fake (`InMemoryFileStorage`), and moving to Azure means changing config, not code.

**Middleware pipeline** (`Program.cs`) — each request passes through these in order: exception handler → Swagger → static files (React) → controllers → fallback to `index.html`. Order matters: the exception handler goes first so it catches errors from everything after it.

**`[ApiController]`** — automatic model validation (400 responses), binds JSON bodies, and requires attribute routing.

**Async/await** — every database and storage call is `async`. While waiting on I/O the thread goes back to the pool to serve other requests, so the server handles more traffic.

**DTOs vs entities** — entities are database rows; DTOs are the API contract. Keeping them separate means changing a table doesn't silently change the API, and internal fields like `StorageKey` stay hidden.

---

## 7. REST API design

| Verb | Meaning | Status codes used |
|---|---|---|
| GET | Read | 200, 404 |
| POST | Create | 201 Created (+ Location header), 400 |
| PUT | Replace / update | 200, 400, 404 |
| DELETE | Remove | 204 No Content, 404 |

- Resources are nouns, nested where it makes sense: `/api/sites/{id}/scans`.
- Errors use the **ProblemDetails** standard (RFC 7807): `{ "title", "status", "detail" }`.
- **Swagger / OpenAPI** documents every endpoint at `/swagger`, and you can try calls from there.

---

## 8. Entity Framework Core

- **ORM**: maps C# classes to tables. LINQ (`Where`, `OrderByDescending`, `Include`) becomes SQL.
- **DbContext** = a session with the database; `DbSet<T>` = a table.
- **`Include`** = eager loading of related rows (a site *with* its scans) in one query, avoiding the "N+1 queries" problem.
- **`AsNoTracking`** for read-only queries: faster, because EF doesn't track changes.
- **Migrations** = versioned, code-generated schema changes. `dotnet ef migrations add InitialCreate` produced `Migrations/…_InitialCreate.cs`. On Azure the app runs `Database.MigrateAsync()` at start-up to create or update the tables.
- **Locally** the app uses SQLite with `EnsureCreated()` so no SQL Server is needed. Migrations are generated for SQL Server because that is what production runs (`Data/DesignTimeDbContextFactory.cs`).
- `EnableRetryOnFailure()` retries short network blips to Azure SQL automatically.

---

## 9. Azure services used

| Service | What it does here | Tier |
|---|---|---|
| **App Service** (Linux) | Hosts the .NET API, which also serves the built React files | F1 (free) |
| **Azure SQL Database** | Stores sites, scans, annotations | Free offer (serverless, auto-pause) |
| **Blob Storage** | Stores scan files in the private `scans` container | Standard LRS |
| **Resource group** | Folder that holds all of the above | — |

- Settings like `Database__Provider` are **App Service application settings**, read by ASP.NET Core like environment variables (`__` means a nested key).
- The SQL connection string is stored in App Service, **not** in the code or the repo.
- `infra/azure-setup.sh` creates everything with the Azure CLI (repeatable, unlike clicking in the portal).

**AZ-900 words to know:** IaaS / PaaS / SaaS (App Service and Azure SQL are **PaaS**), region, resource group, subscription, scaling up vs out, LRS redundancy, consumption vs provisioned pricing.

---

## 10. Testing (xUnit)

- **Unit tests** (`tests/…/Services`) check business rules: wrong file types rejected, history ordered newest first, deleting a site removes its files, image pins must be within 0–1.
- They use **SQLite in-memory**, a real relational database, so foreign keys and cascades behave like production. They also use a **fake `IFileStorage`**, which is only possible because of the interface.
- **Integration tests** (`tests/…/Api/ApiTests.cs`) use `WebApplicationFactory` to start the whole API in memory and send real HTTP requests: create → upload → annotate → download, plus 400 and 404 cases.
- Pattern: **Arrange, Act, Assert**. `[Fact]` = one test; `[Theory]` + `[InlineData]` = the same test with several inputs.

Run: `dotnet test` → 25 passing.

---

## 11. CI/CD (GitHub Actions)

`.github/workflows/ci-cd.yml`, on every push:
1. Install Node and .NET.
2. Build React → output goes into the API's `wwwroot`.
3. `dotnet test` — **a failing test stops the pipeline**, so broken code never deploys.
4. `dotnet publish` → upload the build as an artifact.
5. On `main` only: deploy to Azure App Service using a publish profile stored as a GitHub **secret**.

---

## 12. React front end

- **Components**: `SitesPage`, `SitePage`, `ScanViewer`, `UploadScanForm`, `AnnotationPanel`.
- **Hooks**: `useState` for component data, `useEffect` to load data when the page or selected scan changes.
- **React Router** for pages (`/`, `/sites/:siteId`); the selected scan is kept in the URL (`?scan=2`), so links can be shared.
- **3D viewer**: Google's `<model-viewer>` web component (built on three.js). It handles rotate and zoom. `positionAndNormalFromPoint` turns a mouse click into the X/Y/Z point on the model's surface; that point is saved as the annotation.
- **Photos**: pins are stored as fractions of the width and height (0–1), so they stay in place at any screen size.
- In development Vite proxies `/api` to the .NET server; in production the API serves the built files, so there's one site and no CORS setup needed.

---

## 13. Likely interview questions (with short answers)

**Q: Why controllers, services and repositories? Isn't that overkill?**
Each layer has one job, so each is easy to change and test. I can test upload rules without HTTP or a real database, and swap local storage for Azure Blob without touching business logic.

**Q: What happens if the blob upload succeeds but the database save fails?**
The service catches the error, deletes the uploaded blob, and rethrows, so no orphaned files are left. A production system might use a background clean-up job too.

**Q: How do you prevent invalid data?**
Three levels. DTO attributes (`[Required]`, `[StringLength]`) are checked automatically by `[ApiController]`. Business rules (file type, size, site exists) are checked in the service. Database constraints (max lengths, foreign keys) are the last line.

**Q: Difference between `AddScoped`, `AddSingleton` and `AddTransient`?**
Scoped = one instance per request. Singleton = one for the app's lifetime. Transient = new every time. DbContext must be scoped because it isn't thread-safe.

**Q: Why async?**
I/O (database, blob) is slow. `await` frees the thread while waiting, so the server can handle many more requests with the same threads.

**Q: What is a migration?**
A C# file describing a schema change, generated from the model. It's version-controlled and can be applied to any database in order, so dev and production schemas match.

**Q: How would you scale this?**
Scale App Service out to more instances (the app is stateless, since files are in Blob and data is in SQL). Add pagination to list endpoints. Upload large files straight from the browser to Blob using short-lived **SAS tokens**, so they don't pass through the API. Add caching (Azure Cache for Redis) for hot reads.

**Q: How would you secure it?**
Add authentication with Microsoft Entra ID (JWT bearer tokens) and per-site permissions. Use Managed Identity instead of connection strings for SQL and Storage, and put secrets in Key Vault. Blobs are already private; files are only reachable through the API.

**Q: What's the difference between PUT and PATCH?** PUT replaces the resource; PATCH changes part of it.

**Q: 401 vs 403?** 401 = not logged in (unauthenticated). 403 = logged in but not allowed (unauthorized).

**Q: Abstract class vs interface?** An abstract class can hold shared code and state, and a class inherits only one (`EntityBase`). An interface is a pure contract, and a class can implement many (`IFileStorage`).

**Q: What would you add next?** Authentication, pagination, SAS-token direct uploads, Application Insights monitoring, and side-by-side comparison of two scans of the same site.

---

## 14. Commands cheat sheet

```bash
dotnet build                                   # compile
dotnet test                                    # run the 25 tests
dotnet run --project src/ScanVault.Api --launch-profile http   # run on http://localhost:5297
dotnet ef migrations add <Name> --project src/ScanVault.Api    # new migration after model changes
cd frontend && npm run dev                     # React dev server with hot reload
bash infra/azure-setup.sh                      # create Azure resources (once)
```
