# Play Economy Inventory service
Play economy game items inventory service.

## About
This service implements items inventory service REST api.
It is built only for playground and this code should not be used in production.

### Endpoints

### Architecture

## Run

## Contribute
### Prerequisites
* Install [winget](https://learn.microsoft.com/en-us/windows/package-manager/winget/)
* Install git: `winget install --id Git.Git --source winget`
* Install dotnet 6 (or greater) SDK: `winget install --d Microsoft.DotNet.SDK.6`
* Install docker[^wsl]: `winget install --id Docker.DockerDesktop`
* Install visual studio code: `winget install --id VisualStudioCode --source winget`

### Build development infrastructure.
This service needs some infrastructure services like MongoDB to run. The infrastructure stack is already configured at [Play.Infrastructure](https://github.com/1384microservices/Play.Infrastructure) repository.

### Install Play.Common
This services needs Play.Common nuget package. To make it available on your machine run first instructions from [Play.Common](https://github.com/1384microservices/Play.Common) repository.

### Clone source
Create a project folder on your box. **D:\Projects\Play Economy** is a good idea but you can choose whatever fits your needs. For Windows boxes you have to issue this command in a powershell window: `New-Item -ItemType Directory -Path 'D:\Projects\Play Economy'`. Switch to this directory: `Set-Locatin -Path 'D:\Projects\Play Economy'`. 

Clone this repository to your box: `git clone https://github.com/1384microservices/Play.Inventory.git`.

### Run service
Within your service repository root folder (ie `D:\Projects\Play Economy`) start the service by issuing `dotnet run --Project .\src\Play.Inventory.Service\Play.Inventory.Service.csproj`

### Pack and publish contracts
```powershell
# Change with your package version
$version="1.0.2"
$owner="1384microservices"
# Change with your GitHub Personal Access Token
$gh_pat="[Type here your GitHub PAT here]"
$repositoryUrl="https://github.com/$owner/Play.Inventory"
# Build package
dotnet pack .\src\Play.Inventory.Contracts\ --configuration Release -p:PackageVersion=$version -p:RepositoryUrl=$repositoryUrl -o ..\packages\
# Publish package
dotnet nuget push ..\packages\Play.Inventory.Contracts.$version.nupkg --api-key $gh_pat --source "github"
```

### Publish service container image
```powershell
# Create docker image
docker-compose build

$imageVersion="1.0.7"
docker tag "play.inventory:latest" "play.inventory:${imageVersion}"

$appName="playeconomy1384"
$repositoryUrl="${appName}.azurecr.io"

docker tag "play.inventory:latest" "${repositoryUrl}/play.inventory:${imageVersion}"

# Optional, run it if you're not authenticated
az acr login --name $appName

docker push "${repositoryUrl}/play.inventory:${imageVersion}"
```

### Create K8S service pod
```powershell
$k8sNS="inventory"

# Create namespace
kubectl create namespace  $k8sNS

# Create pod
kubectl apply -f kubernetes\inventory.yaml -n $k8sNS

# Query pod status
kubectl get pods -n $k8sNS

# Query logs
kubectl logs '[pod-name]' -n $k8sNS
kubectl describe '[pod-name]' -n $k8sNS

# Query service
kubectl get svc -n $k8sNS
```

### Create pod managed identity and grabt Key Vault access
```powershell
# Create azure Identity
$appName="playeconomy1384"
$k8sNS="inventory"

az identity create --resource-group $appName --name $k8sNS

# Create pod managed identity
$identityResourceId = az identity show --resource-group $appName --name $k8sNS --query id -otsv
az aks pod-identity add --resource-group $appName --cluster-name $appName --namespace $k8sNS --name $k8sNS --identity-resource-id $identityResourceId

# Grant pod Key Vault access
$identityClientId = az identity show --resource-group $appName --name $k8sNS --query clientId -otsv
az keyvault set-policy -n $appName --secret-permissions get list --spn $identityClientId

# Set federated identity credentials
$aksOIDIssuer=az aks show -n $appName -g $appName --query "oidcIssuerProfile.issuerUrl" -otsv
az identity federated-credential create --name $k8sNS --identity-name $k8sNS --resource-group $appName --issuer $aksOIDIssuer --subject "system:serviceaccount:${k8sNS}:${k8sNS}-serviceaccount"
```

### Install the helm chart
```powershell
$helmUser=[guid]::Empty.Guid

$appname="playeconomy1384"
$registry="${appname}.azurecr.io"
$helmPassword=az acr login --name $appname --expose-token --output tsv --query accessToken
helm registry login $registry --username $helmUser --password $helmPassword

$k8sNS="inventory"
$chartVersion="0.1.0"
helm upgrade inventory-service oci://$registry/helm/microservice --version $chartVersion -f ./helm/values.yaml -n $k8sNS --install
```



[^wsl]:[You need to have WSL upfront](https://learn.microsoft.com/en-us/windows/wsl/)