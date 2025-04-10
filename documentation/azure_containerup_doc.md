Use serverless containers on Azure Article 11/30/2023 2 contributors Serverless computing offers
services that manage and maintain servers, which relieve you of the burden of physically operating
servers yourself. Azure Container Apps is a serverless platform that handles scaling, security, and
infrastructure management for you - all while reducing costs. Once freed from server-related
concerns, you're able to spend your time focusing on your application code.

Container Apps make it easy to manage:

Automatic scaling: As requests for your applications fluctuate, Container Apps keeps your systems
running even during seasons of high demand. Container Apps meets the demand for your app at any
level by automatically creating new copies (called replicas) of your container. As demand falls, the
runtime removes unneeded replicas on your behalf.

Security: Application security is enforced throughout many layers. From authentication and
authorization to network-level security, Container Apps allows you to be explicit about the users
and requests allowed into your system.

Monitoring: Easily monitor your container app's health using observability tools in Container Apps.

Deployment flexibility: You can deploy from GitHub, Azure DevOps, or from your local machine.

Changes: As your containers evolve, Container Apps catalogs changes as revisions to your containers.
If you're experiencing a problem with a container, you can easily roll back to an older version.

Deploy Azure Container Apps with the az containerapp up command Article 07/24/2024 8 contributors In
this article Prerequisites Set up Deploy from an existing image Deploy from local source code Show 2
more The az containerapp up (or up) command is the fastest way to deploy an app in Azure Container
Apps from an existing image, local source code, or a GitHub repo. With this single command, you can
have your container app up and running in minutes.

The az containerapp up command is a streamlined way to create and deploy container apps that
primarily use default settings. However, you need to run other CLI commands to configure more
advanced settings:

Dapr: az containerapp dapr enable Secrets: az containerapp secret set Transport protocols: az
containerapp ingress update To customize your container app's resource or scaling settings, you can
use the up command and then the az containerapp update command to change these settings. The az
containerapp up command isn't an abbreviation of the az containerapp update command.

The up command can create or use existing resources including:

Resource group Azure Container Registry Container Apps environment and Log Analytics workspace Your
container app The command can build and push a container image to an Azure Container Registry (ACR)
when you provide local source code or a GitHub repo. When you're working from a GitHub repo, it
creates a GitHub Actions workflow that automatically builds and pushes a new container image when
you commit changes to your GitHub repo.

If you need to customize the Container Apps environment, first create the environment using the az
containerapp env create command. If you don't provide an existing environment, the up command looks
for one in your resource group and, if found, uses that environment. If not found, it creates an
environment with a Log Analytics workspace.

To learn more about the az containerapp up command and its options, see az containerapp up.

Prerequisites Requirement Instructions Azure account If you don't have one, create an account for
free. You need the Contributor or Owner permission on the Azure subscription to proceed. Refer to
Assign Azure roles using the Azure portal for details. GitHub Account If you use a GitHub repo, sign
up for free. Azure CLI Install the Azure CLI. Local source code You need to have a local source code
directory if you use local source code. Existing Image If you use an existing image, you need your
registry server, image name, and tag. If you're using a private registry, you need your credentials.
Set up Sign in to Azure with the Azure CLI.

Azure CLI

Copy az login Next, install the Azure Container Apps extension for the CLI.

Azure CLI

Copy az extension add --name containerapp --upgrade Now that the current extension or module is
installed, register the Microsoft.App namespace.

Azure CLI

Copy az provider register --namespace Microsoft.App Register the Microsoft.OperationalInsights
provider for the Azure Monitor Log Analytics workspace.

Azure CLI

Copy az provider register --namespace Microsoft.OperationalInsights Deploy from an existing image
You can deploy a container app that uses an existing image in a public or private container
registry. If you're deploying from a private registry, you need to provide your credentials using
the --registry-server, --registry-username, and --registry-password options.

In this example, the az containerapp up command performs the following actions:

Creates a resource group. Creates an environment and Log Analytics workspace. Creates and deploys a
container app that pulls the image from a public registry. Sets the container app's ingress to
external with a target port set to the specified value. Run the following command to deploy a
container app from an existing image. Replace the <PLACEHOLDERS> with your values.

Azure CLI

Copy az containerapp up \
 --name <CONTAINER_APP_NAME> \
 --image <REGISTRY_SERVER>/<IMAGE_NAME>:<TAG> \
 --ingress external \
 --target-port <PORT_NUMBER> You can use the up command to redeploy a container app. If you want to
redeploy with a new image, use the --image option to specify a new image. Ensure that the
--resource-group and environment options are set to the same values as the original deployment.

Azure CLI

Copy az containerapp up \
 --name <CONTAINER_APP_NAME> \
 --image <REGISTRY_SERVER>/<IMAGE_NAME>:<TAG> \
 --resource-group <RESOURCE_GROUP_NAME> \
 --environment <ENVIRONMENT_NAME> \
 --ingress external \
 --target-port <PORT_NUMBER>

az containerapp up Edit Create or update a container app as well as any associated resources (ACR,
resource group, container apps environment, GitHub Actions, etc.).

Azure CLI

Copy az containerapp up --name [--branch] [--browse] [--context-path] [--env-vars] [--environment]
[--image] [--ingress {external, internal}] [--location] [--logs-workspace-id] [--logs-workspace-key]
[--registry-password] [--registry-server] [--registry-username] [--repo] [--resource-group]
[--service-principal-client-id] [--service-principal-client-secret] [--service-principal-tenant-id]
[--source] [--target-port] [--token] [--workload-profile-name] Examples Create a container app from
a dockerfile in a GitHub repo (setting up github actions)

Azure CLI

Copy

Open Cloud Shell az containerapp up -n my-containerapp --repo https://github.com/myAccount/myRepo
Create a container app from a dockerfile in a local directory (or autogenerate a container if no
dockerfile is found)

Azure CLI

Copy

Open Cloud Shell az containerapp up -n my-containerapp --source . Create a container app from an
image in a registry

Azure CLI

Copy

Open Cloud Shell az containerapp up -n my-containerapp --image myregistry.azurecr.io/myImage:myTag
Create a container app from an image in a registry with ingress enabled and a specified environment

Azure CLI

Copy

Open Cloud Shell az containerapp up -n my-containerapp --image myregistry.azurecr.io/myImage:myTag
--ingress external --target-port 80 --environment MyEnv Required Parameters --name -n The name of
the Containerapp. A name must consist of lower case alphanumeric characters or '-', start with a
letter, end with an alphanumeric character, cannot have '--', and must be less than 32 characters.

Optional Parameters --branch -b The branch of the Github repo. Assumed to be the Github repo's
default branch if not specified.

--browse Open the app in a web browser after creation and deployment, if possible.

Default value: False --context-path Path in the repo from which to run the docker build. Defaults to
"./". Dockerfile is assumed to be named "Dockerfile" and in this directory.

--env-vars A list of environment variable(s) for the container. Space-separated values in
'key=value' format. Empty string to clear existing values. Prefix value with 'secretref:' to
reference a secret.

--environment Name or resource ID of the container app's environment.

--image -i Container image, e.g. publisher/image-name:tag.

--ingress The ingress type.

Accepted values: external, internal --location -l Location. Values from: az account list-locations.
You can configure the default location using az configure --defaults location=<location>.

--logs-workspace-id Workspace ID of the Log Analytics workspace to send diagnostics logs to. You can
use "az monitor log-analytics workspace create" to create one. Extra billing may apply.

--logs-workspace-key Log Analytics workspace key to configure your Log Analytics workspace. You can
use "az monitor log-analytics workspace get-shared-keys" to retrieve the key.

--registry-password The password to log in to container registry. If stored as a secret, value must
start with 'secretref:' followed by the secret name.

--registry-server The container registry server hostname, e.g. myregistry.azurecr.io.

--registry-username The username to log in to container registry.

--repo Create an app via Github Actions. In the format: https://github.com/<owner>/<repository-name>
or <owner>/<repository-name>.

--resource-group -g Name of resource group. You can configure the default group using az configure
--defaults group=<name>.

--service-principal-client-id --sp-cid The service principal client ID. Used by Github Actions to
authenticate with Azure.

--service-principal-client-secret --sp-sec The service principal client secret. Used by Github
Actions to authenticate with Azure.

--service-principal-tenant-id --sp-tid The service principal tenant ID. Used by Github Actions to
authenticate with Azure.

--source Local directory path containing the application source and Dockerfile for building the
container image. Preview: If no Dockerfile is present, a container image is generated using
buildpacks. If Docker is not running or buildpacks cannot be used, Oryx will be used to generate the
image. See the supported Oryx runtimes here:
https://github.com/microsoft/Oryx/blob/main/doc/supportedRuntimeVersions.md.

--target-port The application port used for ingress traffic.

--token A Personal Access Token with write access to the specified repository. For more information:
https://help.github.com/en/github/authenticating-to-github/creating-a-personal-access-token-for-the-command-line.
If not provided or not found in the cache (and using --repo), a browser page will be opened to
authenticate with Github.

--workload-profile-name -w The friendly name for the workload profile.

Global Parameters az containerapp up (containerapp extension) Create or update a container app as
well as any associated resources (ACR, resource group, container apps environment, GitHub Actions,
etc.).

Azure CLI

Copy az containerapp up --name [--artifact] [--branch] [--browse] [--build-env-vars]
[--connected-cluster-id] [--context-path] [--custom-location] [--env-vars] [--environment] [--image]
[--ingress {external, internal}] [--location] [--logs-workspace-id] [--logs-workspace-key]
[--registry-identity] [--registry-password] [--registry-server] [--registry-username] [--repo]
[--resource-group] [--revisions-mode {labels, multiple, single}] [--service-principal-client-id]
[--service-principal-client-secret] [--service-principal-tenant-id] [--source] [--system-assigned]
[--target-label] [--target-port] [--token] [--user-assigned] [--workload-profile-name] Examples
Create a container app from a dockerfile in a GitHub repo (setting up github actions)

Azure CLI

Copy

Open Cloud Shell az containerapp up -n my-containerapp --repo https://github.com/myAccount/myRepo
Create a container app from a dockerfile in a local directory (or autogenerate a container if no
dockerfile is found)

Azure CLI

Copy

Open Cloud Shell az containerapp up -n my-containerapp --source . Create a container app from an
image in a registry

Azure CLI

Copy

Open Cloud Shell az containerapp up -n my-containerapp --image myregistry.azurecr.io/myImage:myTag
Create a container app from an image in a registry with ingress enabled and a specified environment

Azure CLI

Copy

Open Cloud Shell az containerapp up -n my-containerapp --image myregistry.azurecr.io/myImage:myTag
--ingress external --target-port 80 --environment MyEnv Create a container app from an image in a
registry on a Connected cluster

Azure CLI

Copy

Open Cloud Shell az containerapp up -n my-containerapp --image myregistry.azurecr.io/myImage:myTag
--connected-cluster-id MyConnectedClusterResourceId Create a container app from an image in a
registry on a connected environment

Azure CLI

Copy

Open Cloud Shell az containerapp up -n my-containerapp --image myregistry.azurecr.io/myImage:myTag
--environment MyConnectedEnvironmentId Required Parameters --name -n The name of the Containerapp. A
name must consist of lower case alphanumeric characters or '-', start with a letter, end with an
alphanumeric character, cannot have '--', and must be less than 32 characters.

Optional Parameters --artifact Preview Local path to the application artifact for building the
container image. See the supported artifacts here: https://aka.ms/SourceToCloudSupportedArtifacts.

--branch -b The branch of the Github repo. Assumed to be the Github repo's default branch if not
specified.

--browse Open the app in a web browser after creation and deployment, if possible.

Default value: False --build-env-vars Preview A list of environment variable(s) for the build.
Space-separated values in 'key=value' format.

--connected-cluster-id Preview Resource ID of connected cluster. List using 'az connectedk8s list'.

--context-path Path in the repo from which to run the docker build. Defaults to "./". Dockerfile is
assumed to be named "Dockerfile" and in this directory.

--custom-location Preview Resource ID of custom location. List using 'az customlocation list'.

--env-vars A list of environment variable(s) for the container. Space-separated values in
'key=value' format. Empty string to clear existing values. Prefix value with 'secretref:' to
reference a secret.

--environment Name or resource ID of the container app's managed environment or connected
environment.

--image -i Container image, e.g. publisher/image-name:tag.

--ingress The ingress type.

Accepted values: external, internal --location -l Location. Values from: az account list-locations.
You can configure the default location using az configure --defaults location=<location>.

--logs-workspace-id Workspace ID of the Log Analytics workspace to send diagnostics logs to. You can
use "az monitor log-analytics workspace create" to create one. Extra billing may apply.

--logs-workspace-key Log Analytics workspace key to configure your Log Analytics workspace. You can
use "az monitor log-analytics workspace get-shared-keys" to retrieve the key.

--registry-identity A Managed Identity to authenticate with the registry server instead of
username/password. Use a resource ID or 'system' for user-defined and system-defined identities,
respectively. The registry must be an ACR. If possible, an 'acrpull' role assignemnt will be created
for the identity automatically.

--registry-password The password to log in to container registry. If stored as a secret, value must
start with 'secretref:' followed by the secret name.

--registry-server The container registry server hostname, e.g. myregistry.azurecr.io.

--registry-username The username to log in to container registry.

--repo Create an app via Github Actions. In the format: https://github.com/owner/repository-name or
owner/repository-name.

--resource-group -g Name of resource group. You can configure the default group using az configure
--defaults group=<name>.

--revisions-mode The active revisions mode for the container app.

Accepted values: labels, multiple, single --service-principal-client-id --sp-cid The service
principal client ID. Used by Github Actions to authenticate with Azure.

--service-principal-client-secret --sp-sec The service principal client secret. Used by Github
Actions to authenticate with Azure.

--service-principal-tenant-id --sp-tid The service principal tenant ID. Used by Github Actions to
authenticate with Azure.

--source Local directory path containing the application source and Dockerfile for building the
container image. Preview: If no Dockerfile is present, a container image is generated using
buildpacks. If Docker is not running or buildpacks cannot be used, Oryx will be used to generate the
image. See the supported Oryx runtimes here:
https://github.com/microsoft/Oryx/blob/main/doc/supportedRuntimeVersions.md.

--system-assigned Boolean indicating whether to assign system-assigned identity.

--target-label Preview The label to apply to new revisions. Required for revisions-mode 'labels'.

--target-port The application port used for ingress traffic.

--token A Personal Access Token with write access to the specified repository. For more information:
https://help.github.com/en/github/authenticating-to-github/creating-a-personal-access-token-for-the-command-line.
If not provided or not found in the cache (and using --repo), a browser page will be opened to
authenticate with Github.

--user-assigned Space-separated user identities to be assigned.

--workload-profile-name -w The friendly name for the workload profile.
