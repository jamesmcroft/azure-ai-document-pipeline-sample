import { raiPolicyInfo, modelDeploymentInfo } from './ai_ml/ai-services.bicep'

targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the workload which is used to generate a short unique hash used in all resources.')
param workloadName string

@minLength(1)
@description('Primary location for all resources.')
param location string

@description('Name of the resource group. If empty, a unique name will be generated.')
param resourceGroupName string = ''

@description('Tags for all resources.')
param tags object = {
  WorkloadName: workloadName
  Environment: 'Dev'
}

var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, workloadName, location))

var storageAccountName = '${abbrs.storage.storageAccount}${resourceToken}'
var keyVaultName = '${abbrs.security.keyVault}${resourceToken}'
var logAnalyticsWorkspaceName = '${abbrs.managementGovernance.logAnalyticsWorkspace}${resourceToken}'
var applicationInsightsName = '${abbrs.managementGovernance.applicationInsights}${resourceToken}'
var containerRegistryName = '${abbrs.containers.containerRegistry}${resourceToken}'
var aiServicesName = '${abbrs.ai.aiServices}${resourceToken}'
var containerAppsEnvironmentName = '${abbrs.containers.containerAppsEnvironment}${resourceToken}'

var raiPolicy = {
  name: workloadName
  mode: 'Blocking'
  prompt: {}
  completion: {}
}

var gpt4oDeployment = {
  name: 'gpt-4o'
  model: {
    format: 'OpenAI'
    name: 'gpt-4o'
    version: '2024-08-06'
  }
  sku: {
    name: 'GlobalStandard'
    capacity: 10
  }
  raiPolicyName: workloadName
  versionUpgradeOption: 'OnceCurrentVersionExpired'
}

resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: !empty(resourceGroupName) ? resourceGroupName : '${abbrs.managementGovernance.resourceGroup}${workloadName}'
  location: location
  tags: union(tags, {})
}

module storageAccount './storage/storage-account.bicep' = {
  name: storageAccountName
  scope: resourceGroup
  params: {
    name: storageAccountName
    location: location
    tags: union(tags, {})
    sku: {
      name: 'Standard_LRS'
    }
  }
}

module keyVault './security/key-vault.bicep' = {
  name: keyVaultName
  scope: resourceGroup
  params: {
    name: keyVaultName
    location: location
    tags: union(tags, {})
  }
}

module logAnalyticsWorkspace './management_governance/log-analytics-workspace.bicep' = {
  name: logAnalyticsWorkspaceName
  scope: resourceGroup
  params: {
    name: logAnalyticsWorkspaceName
    location: location
    tags: union(tags, {})
  }
}

module applicationInsights './management_governance/application-insights.bicep' = {
  name: applicationInsightsName
  scope: resourceGroup
  params: {
    name: applicationInsightsName
    location: location
    tags: union(tags, {})
    logAnalyticsWorkspaceName: logAnalyticsWorkspace.outputs.name
  }
}

module containerRegistry './containers/container-registry.bicep' = {
  name: containerRegistryName
  scope: resourceGroup
  params: {
    name: containerRegistryName
    location: location
    tags: union(tags, {})
    sku: {
      name: 'Basic'
    }
    adminUserEnabled: true
  }
}

module aiServices './ai_ml/ai-services.bicep' = {
  name: aiServicesName
  scope: resourceGroup
  params: {
    name: aiServicesName
    location: location
    tags: union(tags, {})
    raiPolicies: [raiPolicy]
    deployments: [gpt4oDeployment]
  }
}

module containerAppsEnvironment './containers/container-apps-environment.bicep' = {
  name: containerAppsEnvironmentName
  scope: resourceGroup
  params: {
    name: containerAppsEnvironmentName
    location: location
    tags: union(tags, {})
    logAnalyticsWorkspaceName: logAnalyticsWorkspace.outputs.name
  }
}

output outputs object = {
  location: location
  resourceGroupName: resourceGroup.name
  workloadName: workloadName
  containerRegistryName: containerRegistryName
  openAIEndpoint: aiServices.outputs.openAIEndpoint
  gpt4oDeploymentName: gpt4oDeployment.name
  storageAccountName: storageAccountName
}
