# Chatty McChatface: Azure Deployment Plan

This document outlines the recommended strategy for deploying the modernized Chatty McChatface
application (separate .NET backend and Angular frontend) to the existing Azure Web App.

## 1. Target Environment

-   **Azure Web App Name:** `chattymcchatface`
-   **Resource Group:** `KlyveressCelestialKingdom`
-   **Default HostName:** `chattymcchatface.azurewebsites.net`
-   **Deployment Model:** Single Azure Web App instance.

## 2. Deployment Strategy: API at Root, Frontend in Subdirectory

The chosen strategy involves deploying the .NET API to the root of the Web App (`/site/wwwroot`) and
the built Angular frontend application to a subdirectory (`/site/wwwroot/app`).

**Rationale:** This approach aims to simplify Azure App Service configuration compared to hosting
the API in a virtual directory, especially regarding URL rewrite rules needed for the frontend's
client-side routing.

## 3. Backend (.NET API) Deployment Steps

-   **Code Preparation:**
    -   **CORS:** Ensure `Program.cs` in `ChattyMcChatface.Api` is configured to allow requests from
        the frontend origin: `https://chattymcchatface.azurewebsites.net`.
    ```csharp
    // Example CORS configuration in Program.cs
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowSpecificOrigin",
            policy => policy.WithOrigins("https://chattymcchatface.azurewebsites.net") // Frontend URL
                           .AllowAnyHeader()
                           .AllowAnyMethod()
                           .AllowCredentials()); // Important for SignalR with auth
    });
    // ... later, before app.MapHub or app.UseAuthorization ...
    app.UseCors("AllowSpecificOrigin");
    ```
-   **Build:** Generate the production build artifacts:
    ```bash
    cd dotnet_server/ChattyMcChatface.Api
    dotnet publish --configuration Release -o ./publish_output
    cd ../..
    ```
-   **Deployment:** The contents of the `./publish_output` directory will be deployed to the root of
    the Web App (`/site/wwwroot`).

## 4. Frontend (Angular) Deployment Steps

-   **Code Preparation:**
    -   **Environment Configuration:** Update `angular-client/src/environments/environment.prod.ts`
        to point to the API and SignalR endpoints relative to the root:
    ```typescript
    export const environment = {
        production: true,
        apiUrl: "/api", // API controllers are relative to the root
        signalrUrl: "/chathub", // SignalR hub is relative to the root
    }
    ```
-   **Build:** Generate the production build artifacts, specifying the base URL for deployment
    within the `/app` subdirectory:
    ```bash
    cd angular-client
    ng build --base-href /app/ --configuration production
    cd ..
    ```
    _Note: The build output will be located in `angular-client/dist/angular-client/browser/`._
-   **Deployment:** The _contents_ of the `angular-client/dist/angular-client/browser/` directory
    will be deployed into the `/site/wwwroot/app` subdirectory of the Web App.

## 5. Azure Web App Configuration (`chattymcchatface`)

-   **General Settings:**
    -   Ensure **WebSockets** is **On** (Configuration -> General settings). Required for integrated
        SignalR.
    -   Verify the **Stack** settings (e.g., .NET version, Windows/Linux). This plan assumes .NET 8.
-   **Application Settings:**
    -   Add necessary key-value pairs under Configuration -> Application settings:
        -   `ASPNETCORE_ENVIRONMENT`: `Production`
        -   `JWT_SECRET`: _[Your Secure JWT Secret Key]_
        -   _(Add any other required settings, e.g., future database connection strings)_
-   **Path Mappings / URL Rewrite (`web.config` for Windows):**

    -   If using a **Windows** App Service, a `web.config` file is required in the deployment root
        (`/site/wwwroot`) to:
        1.  Configure the ASP.NET Core Module to run the .NET API DLL.
        2.  Add a URL rewrite rule to handle Angular's client-side routing within the `/app`
            subdirectory.
    -   **Example `web.config` (place in `/site/wwwroot`):**

        ```xml
        <?xml version="1.0" encoding="utf-8"?>
        <configuration>
          <system.webServer>
            <!-- 1. Rewrite rule for Angular client-side routing within /app/ -->
            <rewrite>
              <rules>
                <rule name="AngularAppRouting" stopProcessing="true">
                  <match url="^app(/.*)?$" />
                  <conditions logicalGrouping="MatchAll">
                    <!-- Only rewrite if the request doesn't match a physical file within /app -->
                    <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
                    <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
                  </conditions>
                  <!-- Rewrite to the Angular entry point -->
                  <action type="Rewrite" url="/app/index.html" />
                </rule>
              </rules>
            </rewrite>

            <!-- 2. Handler for the root .NET Core application -->
            <handlers>
              <!-- Remove existing handlers if necessary -->
              <remove name="aspNetCore"/>
              <!-- Add the ASP.NET Core handler for all paths not handled by static files or rewrite rules -->
              <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
            </handlers>

            <!-- 3. ASP.NET Core Module configuration -->
            <aspNetCore processPath="dotnet" arguments=".\ChattyMcChatface.Api.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess">
              <environmentVariables>
                <!-- Environment is set via App Settings, but can be overridden here if needed -->
                <!-- <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" /> -->
              </environmentVariables>
            </aspNetCore>

          </system.webServer>
        </configuration>
        ```

    -   If using a **Linux** App Service, similar rewrite logic might be needed depending on the
        specific server configuration, potentially handled within the .NET application's
        `Startup.cs`/`Program.cs` or via Nginx/Apache configuration if used as a reverse proxy (less
        common on standard App Service). The `web.config` is specific to IIS/Windows.

## 6. SignalR Strategy

-   **Initial Deployment:** Utilize the SignalR hub running **integrated** within the .NET API
    process on the App Service instance. The enabled WebSockets setting is sufficient for this.
-   **Scalability:** For future needs requiring higher scale, reliability, or reduced load on the
    Web App instance, plan to migrate to the **Azure SignalR Service**. This involves:
    -   Provisioning an Azure SignalR Service resource.
    -   Adding the `Microsoft.Azure.SignalR` NuGet package to the API project.
    -   Updating `Program.cs` to use `.AddSignalR().AddAzureSignalR()` and configure the connection
        string (via App Settings).
    -   Updating the Angular client's `signalrUrl` if necessary (often the negotiation endpoint
        remains the same, but the service handles the connection).

## 7. Deployment Process

-   **Artifact Preparation:** Create a deployment package (e.g., a `.zip` file) with the following
    structure:
    ```
    /ChattyMcChatface.Api.dll  (and all other .NET publish output)
    /web.config                (if Windows)
    /wwwroot                   (if .NET includes static files here)
    /app/
        /index.html            (and all other Angular build output)
        /main.js
        /styles.css
        /assets/
        ...etc
    ```
-   **Deployment Method:** Choose a suitable method:
    -   **Zip Deploy:** Using Azure CLI (`az webapp deployment source config-zip`), Azure Portal, or
        Visual Studio Publish.
    -   **CI/CD:** Set up a pipeline using Azure DevOps or GitHub Actions to automate the build and
        deployment process. This is the recommended approach for repeatable deployments.

## 8. Post-Deployment Verification

-   Access the frontend at `https://chattymcchatface.azurewebsites.net/app`.
-   Test API endpoints directly (e.g., `https://chattymcchatface.azurewebsites.net/api/users`).
-   Verify real-time chat functionality via SignalR.
-   Check Azure Portal -> Monitoring -> Log stream for any startup or runtime errors.
