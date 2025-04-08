# Project Modernization Plan: Chatty McChatface

This document outlines the plan to modernize the Angular frontend and rewrite the Node.js backend in
.NET 8 with SQLite.

## Phase 1: Backend Rewrite (.NET 8 & SQLite)

The goal is to replace the existing Node.js/Express/MongoDB backend with a new ASP.NET Core 8 Web
API using EF Core and SQLite (in-memory).

**1. Setup .NET 8 Project:** _ Create a new .NET 8 Web API project solution (e.g.,
`ChattyMcChatface.sln`). _ Create projects within the solution: _ `ChattyMcChatface.Api`: ASP.NET
Core Web API project (Controllers/Minimal APIs, Startup/Program.cs). _ `ChattyMcChatface.Data`:
Class library for EF Core (DbContext, Migrations, Models/Entities). _ `ChattyMcChatface.Core`: Class
library for core logic/services (optional, but good practice). _ Install necessary NuGet packages: _
`Microsoft.EntityFrameworkCore.Sqlite` _ `Microsoft.EntityFrameworkCore.Design` (for migrations,
though less critical for in-memory) _ `Microsoft.AspNetCore.Authentication.JwtBearer` (for JWT auth)
_ `Microsoft.AspNetCore.SignalR` (for real-time)

**2. Define Data Models (Entities):** _ Location: `ChattyMcChatface.Data/Entities/` _ Create C#
classes corresponding to the existing Node models (`server/api/*/model/`): _ `User.cs` (map
`user-model.js`) _ `Chatroom.cs` (map `chatroom-model.js`) _ `ChatMessage.cs` (map `chat-model.js`)
_ `AbbreviatedUser.cs` (map `abbreviated-user-model.js` - consider if this is needed or can be
derived via projection). _ `LastRead.cs` (map `lastread-model.js` - consider relationship to
User/Chatroom). _ Add necessary properties and relationships (e.g., navigation properties in EF
Core).

**3. Configure EF Core DbContext:** _ Location: `ChattyMcChatface.Data/AppDbContext.cs` _ Create
`AppDbContext` inheriting from `DbContext`. _ Add `DbSet<>` properties for each entity. _ Configure
relationships using Fluent API or attributes. _ In `ChattyMcChatface.Api/Program.cs` (or
`Startup.cs`): _ Configure the DbContext to use SQLite in-memory:
`options.UseSqlite("DataSource=:memory:")`. * Ensure the database is created on startup:
`dbContext.Database.EnsureCreated();`. *Note: For in-memory, the DB exists only for the lifetime of
the connection.\* Keep the connection open if needed across requests or manage scope carefully. A
singleton service holding the connection might be required.

**4. Implement Authentication & Authorization:** _ Location: `ChattyMcChatface.Api/`
(Configuration), `ChattyMcChatface.Core/Services/` (Logic) _ Configure JWT Bearer authentication in
`Program.cs`. _ Implement user registration logic (hashing passwords securely). _ Implement user
login logic (validating credentials, generating JWT). \* Replicate authorization checks
(`auth.authorize`, `auth.userContextRequired`) using ASP.NET Core authorization policies or
attributes (`[Authorize]`).

**5. Implement API Endpoints:** _ Location: `ChattyMcChatface.Api/Controllers/` or
`ChattyMcChatface.Api/Endpoints/` (for Minimal APIs) _ Recreate endpoints based on
`server/api/*/routes/`: _ **User API (`UserController` or User Endpoints):** _ `POST /api/user`
(Register - map `UserController.createUser`) _ `POST /api/user/login` (Login - map
`UserController.loginUser`, change from PUT to POST) _ `GET /api/user` (Get others - map
`UserController.getAllOthers`, requires auth) _ `GET /api/user/{id}` (Get specific - map
`UserController.get`) _ `DELETE /api/user/{id}` (Delete - map `UserController.deleteUser`, requires
auth/policy) _ **Chatroom API (`ChatroomController` or Chatroom Endpoints):** _ `GET /api/chatroom`
(Get all - map `ChatController.getAll`, requires auth) _ `POST /api/chatroom` (Create - map
`ChatController.createChatroom`, requires auth) _ `PUT /api/chatroom/{id}` (Update - map
`ChatController.updateChatroom`, clarify requirements, requires auth) _ `GET /api/chatroom/{id}`
(Get specific - map `ChatController.get`) _ `POST /api/chatroom/{id}/chats` (Add chat - map
`ChatController.addChat`, change from PUT, requires auth) _ `DELETE /api/chatroom/{id}` (Delete -
map `ChatController.delete`, requires auth) _ Inject DbContext/Repositories/Services into
controllers/endpoints. \* Use async/await for database operations.

**6. Implement Real-time Communication (SignalR):** _ Location: `ChattyMcChatface.Api/Hubs/` _
Create a SignalR Hub (e.g., `ChatHub.cs`). _ Define methods the client can call (e.g.,
`SendMessage`, `JoinRoom`) and methods the server can call on clients (e.g., `ReceiveMessage`,
`UserJoined`). _ Map the SignalR hub endpoint in `Program.cs`. \* Integrate Hub logic with chatroom
services (e.g., broadcast messages when added via API).

**7. Configure Middleware:** _ Location: `ChattyMcChatface.Api/Program.cs` _ Configure CORS (allow
requests from the Angular frontend's origin). _ Ensure Authentication and Authorization middleware
are correctly ordered. _ Add other necessary middleware (HTTPS redirection, etc.).

## Phase 2: Frontend Modernization (Angular Latest)

The goal is to migrate the old Angular 2 RC application to the latest stable version of Angular
using the Angular CLI.

**1. Setup New Angular Project:** _ Use Angular CLI:
`ng new chatty-mcchatface-client --standalone=false --routing=true --style=css` (or choose SCSS if
preferred). We'll start with NgModule based structure for easier migration, can refactor to
standalone later if desired. _ Copy assets: `client/favicon.ico`, `client/fonts/`, potentially
relevant parts of `client/css/` (like `logo-font.css`).

**2. Migrate Core Application Structure:** _ **`app.module.ts`:** _ Declare `AppComponent` in
`declarations` and `bootstrap`. _ Import `BrowserModule`, `HttpClientModule`, `AppRoutingModule`,
`FormsModule`. _ Import feature modules (Chat, User, Header). _ Add providers for services
(`AuthService`, `UserService`, `ChatService`). _ **`app-routing.module.ts`:** _ Define routes
previously in `AppComponent` (`@RouteConfig`) using `RouterModule.forRoot([...])`. _ Map paths like
`/register`, `/login`, `/mcchatface` (consider renaming `/mcchatface` to `/chat`). _ Use
`loadChildren` for lazy loading feature modules if desired. _ **`app.component.ts/html`:** _ Update
template (`client/app/app.component.html`) to use `<router-outlet>` and the new header component
selector. _ Remove deprecated `directives`, `providers`, `RouteConfig`. \* Update
constructor/imports as needed.

**3. Migrate Feature Modules & Components:** _ For each feature area (Chat, User, Header): _ Create
Angular Modules (e.g., `ChatModule`, `UserModule`, `HeaderModule`) using `ng generate module ...`. _
Create Routing Modules for features (e.g., `ChatRoutingModule`) using
`ng generate module ... --routing`. _ Migrate components (`.ts`, `.html`, `.css`) from
`client/app/*` to the new structure. _ Generate components using `ng generate component ...`. _
Copy/paste and adapt code/templates. _ Update selectors, template URLs, style URLs. _ Declare
components within their respective feature modules. _ Update imports (e.g., `ROUTER_DIRECTIVES` ->
`RouterModule`, remove deprecated ones). _ Refactor component logic as needed (e.g., lifecycle
hooks). _ **Specific Component Notes:** _ **Header:** Consolidate logged-in/logged-out logic
(`app.header.*`) into a single `HeaderComponent` driven by `AuthService` state. _ **Chat:** Update
`ChatComponent` routing (`/mcchatface/...`). Update `ChatService` for SignalR. _ **User:** Update
`UserService`, `AuthService`.

**4. Update Services:** _ **`AuthService`, `UserService`, `ChatService`:** _ Replace `@angular/http`
(`Http`) with `@angular/common/http` (`HttpClient`). Update methods (`get`, `post`, `put`, `delete`)
accordingly. _ Update API base URLs to point to the new .NET backend (use environment
configuration). _ Inject `HttpClient` in constructors. _ Refactor RxJS usage (replace `rxjs/Rx` with
specific operator imports like `map`, `catchError`, `tap` from `rxjs/operators`). _
**`ChatService`:** Replace Socket.IO client logic with the `@microsoft/signalr` client library to
connect to the .NET SignalR hub.

**5. Update Styling:** _ Copy `client/styles.css` content into the new project's `src/styles.css`. _
Integrate Bootstrap: Install using npm (`npm install bootstrap`) and import in `angular.json` or
`styles.css`. Remove CDN links from `index.html`. _ Migrate or replace theme CSS
(`theme-assets.min.css`, `theme-colors-css/b-different-bw.min.css`). Check if they are still needed
or can be replaced with modern Bootstrap/custom CSS. _ Address `logo-font.css` and font files.

**6. Handle Assets & Index.html:** _ Update `src/index.html`: Remove old script tags (SystemJS,
polyfills, CDN JS). Angular CLI handles bundling. Keep the `<chatty-mcchatface>` root element (or
update selector if changed). Set the correct `<base href="/">`. _ Configure assets (favicon, fonts)
in `angular.json`.

**7. Environment Configuration:** \* Use Angular's `src/environments/` files (`environment.ts`,
`environment.prod.ts`) to manage API base URLs and other configurations previously in
`client/app/config.ts`.

**8. Replace Custom JS (`logo.js`):** _ Analyze `client/logo.js`. This appears to be a canvas
animation. _ Decide whether to keep this feature. \* If keeping, re-implement using Angular
lifecycle hooks (`ngOnInit`, `ngOnDestroy`), potentially within the `AppComponent` or a dedicated
component. Avoid direct DOM manipulation where possible, use `@ViewChild` to get canvas reference.
Ensure `requestAnimationFrame` is cleaned up `ngOnDestroy`.

**9. Testing & Build:** _ Run `ng serve` for development. _ Run `ng build` for production builds. \*
Implement unit/integration tests as needed.

## Phase 3: Integration & Deployment

-   Ensure CORS is correctly configured on the .NET backend to allow requests from the Angular dev
    server and production domain.
-   Test frontend-backend integration thoroughly (API calls, authentication, SignalR).
-   Plan deployment strategy (e.g., Docker containers, Azure App Service, IIS).

---

This plan provides a high-level roadmap. Each step will involve detailed code analysis and
implementation.
