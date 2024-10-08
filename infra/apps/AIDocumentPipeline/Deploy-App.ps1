<#
.SYNOPSIS
    Builds and deploys the AI Document Pipeline Azure Functions app to a Container App in an existing Azure environment.
.DESCRIPTION
    This script initiates the deployment of the app.bicep template to the current default Azure subscription,
    determined by the Azure CLI. The infra/Deploy-Infrastructure.ps1 script must be run first to deploy the
    core infrastructure to the Azure subscription required by this script.

	Follow the instructions in the DeploymentGuide.md file at the root of this project to understand what this
    script will deploy to your Azure subscription, and the step-by-step on how to run it.
.PARAMETER InfrastructureOutputsPath
    The path to the deployments outputs file from the infra/Deploy-Infrastructure.ps1 script.
.EXAMPLE
    .\Deploy-App.ps1 -InfrastructureOutputsPath "../../InfrastructureOutputs.json"
.NOTES
    Author: James Croft
#>

param
(
    [Parameter(Mandatory = $true)]
    [string]$InfrastructureOutputsPath
)

function Get-JsonBicepParameterObject($hashTable) {
    $hashTableJson = $hashTable | ConvertTo-Json -Compress
    $hashTableJson = $hashTableJson -replace '"', '\"'
    $hashTableJson = "`"$hashTableJson`""
    return $hashTableJson
}

$InfrastructureOutputs = Get-Content -Path $InfrastructureOutputsPath -Raw | ConvertFrom-Json

$Location = $InfrastructureOutputs.outputs.value.location
$ResourceGroupName = $InfrastructureOutputs.outputs.value.resourceGroupName
$WorkloadName = $InfrastructureOutputs.outputs.value.workloadName
$ContainerRegistryName = $InfrastructureOutputs.outputs.value.containerRegistryName
$Gpt4oModelEndpoint = $InfrastructureOutputs.outputs.value.openAIEndpoint
$Gpt4oModelDeploymentName = $InfrastructureOutputs.outputs.value.gpt4oDeploymentName

$ContainerName = "ai-document-pipeline"
$ContainerVersion = (Get-Date -Format "yyMMddHHmm")
$ContainerImageName = "${ContainerName}:${ContainerVersion}"
$AzureContainerImageName = "${ContainerRegistryName}.azurecr.io/${ContainerImageName}"

Push-Location -Path $PSScriptRoot

Write-Host "Starting ${ContainerName} deployment..."

az --version

Write-Host "Building ${ContainerImageName} image..."

az acr login --name $ContainerRegistryName

docker build -t $ContainerImageName -f ../../../src/AIDocumentPipeline/Dockerfile ../../../src/.

Write-Host "Pushing ${ContainerImageName} image to Azure..."

docker tag $ContainerImageName $AzureContainerImageName
docker push $AzureContainerImageName

Write-Host "Deploying Azure Container Apps for ${ContainerName}..."

$EnvironmentVariables = @(
    @{ name = "OPENAI_ENDPOINT"; value = $Gpt4oModelEndpoint }
    @{ name = "OPENAI_VISION_COMPLETION_DEPLOYMENT"; value = $Gpt4oModelDeploymentName }
    @{ name = "OPENAI_API_VERSION"; value = "2024-08-01-preview" }
)

$DeploymentOutputs = (az deployment group create --name $ContainerName --resource-group $ResourceGroupName --template-file './app.bicep' `
        --parameters '../../main.parameters.json' `
        --parameters workloadName=$WorkloadName `
        --parameters location=$Location `
        --parameters containerImageName=$ContainerImageName `
        --parameters environmentVariables=$(Get-JsonBicepParameterObject $EnvironmentVariables) `
        --query properties.outputs -o json) | ConvertFrom-Json

$DeploymentOutputs | ConvertTo-Json | Out-File -FilePath './AppOutputs.json' -Encoding utf8

Write-Host "Cleaning up old ${ContainerName} images in Azure Container Registry..."

az acr run --cmd "acr purge --filter '${ContainerName}:.*' --untagged --ago 1h" --registry $ContainerRegistryName /dev/null

Pop-Location

return $DeploymentOutputs
