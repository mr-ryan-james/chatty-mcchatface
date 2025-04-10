# Chatty McChatface Modernization: Local Dev & Deployment Plan

This document outlines the plan for setting up local development and deploying the modernized Chatty
McChatface application to Azure.

## Task 1: Local Development Setup

**Goal:** Provide a single command to run both the .NET backend API and the Angular frontend
development server concurrently.

**Information Gathered:**

-   **Backend:** Runs via `dotnet run` in `dotnet_server/ChattyMcChatface.Api`, available at
    `http://localhost:5124` (and `https://localhost:7045`). Checked via
    `dotnet_server/ChattyMcChatface.Api/Properties/launchSettings.json`.
-   **Frontend:** Runs via `npm start` (`ng serve`) in `angular-client`, available at
    `http://localhost:4200` (default port). Checked via `angular-client/package.json` and
    `angular-client/angular.json`.
-   **Connection:** Frontend (`angular-client/src/environments/environment.ts`) is correctly
    configured to connect to `http://localhost:5124`.
-   **Tooling:** `concurrently` is already available as a dev dependency in the root `package.json`.
    Checked via `package.json`.

**Implementation Plan:**

1.  **Prerequisites:**

    -   Ensure .NET 9 SDK is installed.
    -   Ensure Node.js (compatible with Angular 17+, likely v18+ recommended) and npm are installed.
    -   Run `npm install` in the `/Users/ryanpfister/Dev/chatty-mcchatface/angular-client` directory
        to install frontend dependencies. (The root `package.json` dependencies seem outdated and
        related to the old Node.js server/Angular RC client, so running `npm install` in the root
        might not be necessary or even desirable unless other root-level tools are needed later).
        ```bash
        cd angular-client
        npm install
        cd ..
        ```

    2.  **Modify Root `package.json`:**
        -   Open `/Users/ryanpfister/Dev/chatty-mcchatface/package.json`.
        -   Add a new script to the `"scripts"` section:
            ```json
            "dev": "concurrently \\\"cd dotnet_server/ChattyMcChatface.Api && dotnet run\\\" \\\"cd angular-client && npm start\\\""
            ```
            _(Note: Using escaped quotes for robustness across shells)._
    3.  **Update `README.md`:**

        -   Open `/Users/ryanpfister/Dev/chatty-mcchatface/README.md`.
        -   Add a new section, for example:

            ````markdown
            ## Running Locally (Modernized Version)

            This project now consists of a .NET 9 backend API and an Angular frontend.

            **Prerequisites:**

            -   .NET 9 SDK
            -   Node.js (v18+ recommended) and npm
            -   Run `npm install` inside the `angular-client` directory:
                ```bash
                cd angular-client
                npm install
                cd ..
                ```
            ````

        **Running the Application:**

        From the root project directory (`/Users/ryanpfister/Dev/chatty-mcchatface`), run the
        following command:

        ```bash
        npm run dev
        ```

        ````

        This will concurrently:

        -   Start the .NET backend API (listening on `http://localhost:5124` and
            `https://localhost:7045`).
        -   Start the Angular frontend development server (available at `http://localhost:4200`).

        Open your browser to `http://localhost:4200` to use the application.

        ```

        ```
        ````

**Next Steps (Task 1):**

-   Confirm this plan for Task 1 is acceptable before proceeding with file modifications.

## Task 2: Azure Deployment Strategy

_(Planning for this task will commence after Task 1 plan is approved/refined)._
