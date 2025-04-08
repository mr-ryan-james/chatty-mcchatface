# Azure App Service Deployment Issue Report

## Overview

This document describes an issue encountered while attempting to deploy the Chatty McChatface
application to Azure App Service using Zip Deploy.

## Detailed Deployment Process and Commands

### 1. Initial Environment Inspection

First, we examined the contents of the publish_output directory to confirm what files needed to be
deployed:

```bash
# Check the top-level contents of the publish directory
list_files publish_output
# Result: Confirmed web.config, api/ directory, and app/ directory exist

# Examine the Angular app contents
list_files publish_output/app
# Result: Confirmed existence of browser/ directory with Angular assets

# Examine the web.config to understand routing configuration
read_file publish_output/web.config
# Result: Confirmed web.config contained proper routing rules for API and Angular app
```

### 2. Preparing Deployment Package

Created a zip file containing all required components:

```bash
cd publish_output && zip -r chattymcchatface_deploy.zip web.config api app
# Result: Successfully created zip file with all application components
```

### 3. Azure Environment Verification

Verified Azure CLI installation and account status:

```bash
# Check Azure CLI version
az --version
# Result: azure-cli 2.70.0 installed

# Verify logged-in account
az account show
# Result: Successfully logged in as "ryan@pfister.nyc" to subscription "VS Subscription 2"
```

### 4. Initial Deployment Attempt

First deployment attempt used an incorrect resource group:

```bash
az webapp deployment source config-zip --resource-group chattymcchatface-rg --name chattymcchatface --src chattymcchatface_deploy.zip
# Result: FAILED - ResourceGroupNotFound error
```

### 5. Resource Group Discovery

Identified the correct resource group:

```bash
# List all resource groups
az group list --query "[].name" -o tsv
# Result: Found multiple resource groups but not "chattymcchatface-rg"

# Find the specific resource group containing the web app
az webapp list --query "[].{name:name, resourceGroup:resourceGroup}" -o table
# Result: Found webapp "chattymcchatface" in resource group "KlyveressCelestialKingdom"
```

### 6. Corrected Deployment Command

Attempted with correct resource group but incorrect parameter format:

```bash
az webapp deploy --resource-group KlyveressCelestialKingdom --name chattymcchatface --src chattymcchatface_deploy.zip
# Result: FAILED - ambiguous option: --src could match --src-path, --src-url
```

### 7. Successful Deployment

Executed correct deployment command:

```bash
az webapp deploy --resource-group KlyveressCelestialKingdom --name chattymcchatface --src-path chattymcchatface_deploy.zip --type zip
# Result: SUCCESS - "Deployment has completed successfully"
```

### 8. Application Verification

Verified application deployment status using multiple methods:

```bash
# Check HTTP endpoint (resulted in redirect)
curl -I http://chattymcchatface.azurewebsites.net
# Result: HTTP/1.1 301 Moved Permanently to HTTPS endpoint

# Check HTTPS endpoint
curl -I https://chattymcchatface.azurewebsites.net
# Result: HTTP/1.1 200 OK but was showing default Azure page, not our app

# Check Angular app endpoint
curl -I https://chattymcchatface.azurewebsites.net/app
# Result: HTTP/1.1 404 Not Found

# Check alternative Angular app path
curl -I https://chattymcchatface.azurewebsites.net/app/browser
# Result: HTTP/1.1 404 Not Found

# Check API endpoint
curl -I https://chattymcchatface.azurewebsites.net/api
# Result: HTTP/1.1 404 Not Found

# Detailed verbose check of main endpoint content
curl -v https://chattymcchatface.azurewebsites.net
# Result: Confirmed site was serving default Azure welcome page HTML,
#         not our application content
```

### 9. Restart Attempts

Tried restarting the web app to refresh configuration:

```bash
# Stop the web app
az webapp stop --resource-group KlyveressCelestialKingdom --name chattymcchatface
# Result: App stopped successfully

# Start the web app
az webapp start --resource-group KlyveressCelestialKingdom --name chattymcchatface
# Result: App started successfully but issue persisted
```

### 10. Deployment Validation

Attempted to access deployment credentials and validate deployed files:

```bash
# Get publishing credentials
az webapp deployment list-publishing-profiles --resource-group KlyveressCelestialKingdom --name chattymcchatface --query "[?contains(publishMethod, 'MSDeploy')].{userName:userName, userPWD:userPWD}[0]" -o json
# Result: Successfully retrieved credentials (redacted for security)

# Attempt to inspect deployed files via Kudu API
curl https://chattymcchatface.scm.azurewebsites.net/api/vfs/site/wwwroot/ -u '$chattymcchatface:REDACTED' -H "Accept: application/json"
# Result: No content returned, suggesting authentication issues or empty directory
```

### 11. Configuration Inspection

Examined app settings and configuration:

```bash
# List app settings
az webapp config appsettings list --resource-group KlyveressCelestialKingdom --name chattymcchatface
# Result: Only Application Insights settings found, no custom app settings

# Attempt to list deployments (command not valid)
az webapp deploy list --resource-group KlyveressCelestialKingdom --name chattymcchatface
# Result: FAILED - unrecognized arguments: list

# Fetch detailed app configuration
az webapp show --resource-group KlyveressCelestialKingdom --name chattymcchatface --query "siteConfig"
# Result: Confirmed Linux runtime with .NET Core 8.0, virtual path mapping to site/wwwroot
```

### 12. Log Inspection Attempt

Attempted to check application logs:

```bash
# Stream application logs
az webapp log tail --resource-group KlyveressCelestialKingdom --name chattymcchatface
# Result: Command initiated but was interrupted during execution
```

## Issues Encountered

Despite the deployment command completing successfully, the application failed to function properly:

-   The website showed the default Azure App Service welcome page instead of our application
-   The `/api` and `/app` endpoints returned 404 errors
-   The app was not properly recognizing our application structure

## Technical Details

-   The Azure App Service is configured with .NET Core 8.0 runtime
    (`"linuxFxVersion": "DOTNETCORE|8.0"`)
-   The application zip file was deployed to the default location (`/site/wwwroot`)
-   The web.config file contains rules for routing both the API and Angular app
-   App restart attempts did not resolve the issue
-   CURL commands confirmed proper HTTPS setup but incorrect content being served

## Potential Causes

1. **Application Path Configuration**: The files may not be in the expected location for the Azure
   App Service to recognize them.
2. **Runtime Configuration**: There might be a mismatch between the expected runtime environment and
   the actual configuration.
3. **Startup File Detection**: The Azure platform might not be correctly identifying the
   application's entry point.
4. **Web.config Processing**: The web.config routing rules might not be properly applied or
   recognized.
5. **Missing App Settings**: There might be required application settings not currently configured.

## Next Steps

Additional troubleshooting approaches to consider:

1. Deploy with a modified directory structure that more closely matches Azure's expected format
2. Modify the application configuration to explicitly set startup files
3. Check application logs for detailed error information
4. Test with alternative deployment methods (e.g., direct Git deployment)
5. Review Azure App Service configuration for any missing settings specific to our application needs
6. Set the WEBSITE_RUN_FROM_PACKAGE app setting to 1 to run directly from the package
7. Try different zip deployment techniques such as using the Kudu REST API directly
