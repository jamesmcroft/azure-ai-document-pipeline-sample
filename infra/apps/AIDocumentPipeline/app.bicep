import { environmentVariableInfo } from '../../containers/container-app.bicep'

targetScope = 'resourceGroup'

@minLength(1)
@maxLength(64)
@description('Name of the workload which is used to generate a short unique hash used in all resources.')
param workloadName string

@minLength(1)
@description('Primary location for all resources.')
param location string

@description('Tags for all resources.')
param tags object = {
  WorkloadName: workloadName
  Environment: 'Dev'
  App: 'ai-document-pipeline'
}

@description('Name of the container image.')
param containerImageName string

@description('Environment variables for the container.')
param environmentVariables environmentVariableInfo[] = []

var abbrs = loadJsonContent('../../abbreviations.json')
var roles = loadJsonContent('../../roles.json')
var resourceToken = toLower(uniqueString(subscription().id, workloadName, location))
var appResourceToken = toLower(uniqueString(resourceToken, 'app-api'))

var storageAccountName = '${abbrs.storage.storageAccount}${resourceToken}'
var keyVaultName = '${abbrs.security.keyVault}${resourceToken}'
var applicationInsightsName = '${abbrs.managementGovernance.applicationInsights}${resourceToken}'
var containerRegistryName = '${abbrs.containers.containerRegistry}${resourceToken}'
var aiServicesName = '${abbrs.ai.aiServices}${resourceToken}'
var containerAppsEnvironmentName = '${abbrs.containers.containerAppsEnvironment}${resourceToken}'
var appIdentityName = '${abbrs.security.managedIdentity}${appResourceToken}'
var containerAppName = '${abbrs.containers.containerApp}${appResourceToken}'
var invoicesQueueName = 'invoices'

resource containerRegistryRef 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' existing = {
  name: containerRegistryName
}

resource applicationInsightsRef 'Microsoft.Insights/components@2020-02-02' existing = {
  name: applicationInsightsName
}

resource aiServicesRef 'Microsoft.CognitiveServices/accounts@2024-04-01-preview' existing = {
  name: aiServicesName
}

resource containerAppsEnvironmentRef 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  name: containerAppsEnvironmentName
}

resource storageAccountRef 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource keyVaultRef 'Microsoft.KeyVault/vaults@2024-04-01-preview' existing = {
  name: keyVaultName
}

module appIdentity '../../security/managed-identity.bicep' = {
  name: appIdentityName
  params: {
    name: appIdentityName
    location: location
    tags: union(tags, { IdentityFor: containerAppName })
  }
}

resource keyVaultSecretsOfficerRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: roles.security.keyVaultSecretsOfficer
}

resource acrPullRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: roles.containers.acrPull
}

resource cognitiveServicesUserRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: roles.ai.cognitiveServicesUser
}

resource cognitiveServicesOpenAIUserRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: roles.ai.cognitiveServicesOpenAIUser
}

// Required RBAC roles for Azure Functions to access the storage account
// https://learn.microsoft.com/en-us/azure/azure-functions/functions-reference?tabs=blob&pivots=programming-language-csharp#connecting-to-host-storage-with-an-identity
resource storageAccountContributorRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: roles.storage.storageAccountContributor
}

resource storageBlobDataContributorRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: roles.storage.storageBlobDataContributor
}

resource storageBlobDataOwnerRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: roles.storage.storageBlobDataOwner
}

resource storageQueueDataContributorRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: roles.storage.storageQueueDataContributor
}

resource storageTableDataContributorRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: roles.storage.storageTableDataContributor
}

module aiServicesRoleAssignment '../../security/resource-role-assignment.json' = {
  name: '${aiServicesName}-role-assignment'
  params: {
    resourceId: aiServicesRef.id
    roleAssignments: [
      {
        principalId: appIdentity.outputs.principalId
        roleDefinitionId: cognitiveServicesUserRole.id
        principalType: 'ServicePrincipal'
      }
      {
        principalId: appIdentity.outputs.principalId
        roleDefinitionId: cognitiveServicesOpenAIUserRole.id
        principalType: 'ServicePrincipal'
      }
    ]
  }
}

module keyVaultRoleAssignment '../../security/resource-role-assignment.json' = {
  name: '${keyVaultName}-role-assignment'
  params: {
    resourceId: keyVaultRef.id
    roleAssignments: [
      {
        principalId: appIdentity.outputs.principalId
        roleDefinitionId: keyVaultSecretsOfficerRole.id
        principalType: 'ServicePrincipal'
      }
    ]
  }
}

module containerRegistryRoleAssignment '../../security/resource-role-assignment.json' = {
  name: '${containerRegistryName}-role-assignment'
  params: {
    resourceId: containerRegistryRef.id
    roleAssignments: [
      {
        principalId: appIdentity.outputs.principalId
        roleDefinitionId: acrPullRole.id
        principalType: 'ServicePrincipal'
      }
    ]
  }
}

module storageAccountRoleAssignment '../../security/resource-role-assignment.json' = {
  name: '${storageAccountName}-role-assignment'
  params: {
    resourceId: storageAccountRef.id
    roleAssignments: [
      {
        principalId: appIdentity.outputs.principalId
        roleDefinitionId: storageAccountContributorRole.id
        principalType: 'ServicePrincipal'
      }
      {
        principalId: appIdentity.outputs.principalId
        roleDefinitionId: storageBlobDataContributorRole.id
        principalType: 'ServicePrincipal'
      }
      {
        principalId: appIdentity.outputs.principalId
        roleDefinitionId: storageBlobDataOwnerRole.id
        principalType: 'ServicePrincipal'
      }
      {
        principalId: appIdentity.outputs.principalId
        roleDefinitionId: storageQueueDataContributorRole.id
        principalType: 'ServicePrincipal'
      }
      {
        principalId: appIdentity.outputs.principalId
        roleDefinitionId: storageTableDataContributorRole.id
        principalType: 'ServicePrincipal'
      }
    ]
  }
}

var functionsWebJobStorageVariableName = 'AzureWebJobsStorage'
var invoicesConnectionStringVariableName = 'INVOICES_QUEUE_CONNECTION'
var applicationInsightsConnectionStringSecretName = 'applicationinsightsconnectionstring'

module invoicesQueue '../../storage/storage-queue.bicep' = {
  name: '${abbrs.storage.storageAccount}-${invoicesQueueName}'
  params: {
    name: invoicesQueueName
    storageAccountName: storageAccountRef.name
  }
}

module containerApp '../../containers/container-app.bicep' = {
  name: containerAppName
  params: {
    name: containerAppName
    location: location
    tags: union(tags, { App: 'ai-document-pipeline' })
    containerAppsEnvironmentId: containerAppsEnvironmentRef.id
    containerAppIdentityId: appIdentity.outputs.id
    imageInContainerRegistry: true
    containerRegistryName: containerRegistryRef.name
    containerImageName: containerImageName
    containerIngress: {
      external: true
      targetPort: 80
      transport: 'auto'
      allowInsecure: false
    }
    containerScale: {
      minReplicas: 1
      maxReplicas: 3
      rules: [
        {
          name: 'http'
          http: {
            metadata: {
              concurrentRequests: '20'
            }
          }
        }
      ]
    }
    secrets: [
      {
        name: applicationInsightsConnectionStringSecretName
        value: applicationInsightsRef.properties.ConnectionString
      }
    ]
    environmentVariables: concat(
      [
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          secretRef: applicationInsightsConnectionStringSecretName
        }
        {
          name: '${functionsWebJobStorageVariableName}__accountName'
          value: storageAccountRef.name
        }
        {
          name: '${functionsWebJobStorageVariableName}__credential'
          value: 'managedidentity'
        }
        {
          name: '${functionsWebJobStorageVariableName}__clientId'
          value: appIdentity.outputs.clientId
        }
        {
          name: 'MANAGED_IDENTITY_CLIENT_ID'
          value: appIdentity.outputs.clientId
        }
        {
          name: 'INVOICES_STORAGE_ACCOUNT_NAME'
          value: storageAccountRef.name
        }
        {
          name: '${invoicesConnectionStringVariableName}__accountName'
          value: storageAccountRef.name
        }
        {
          name: '${invoicesConnectionStringVariableName}__credential'
          value: 'managedidentity'
        }
        {
          name: '${invoicesConnectionStringVariableName}__clientId'
          value: appIdentity.outputs.clientId
        }
        {
          name: 'WEBSITE_HOSTNAME'
          value: 'localhost'
        }
      ],
      environmentVariables
    )
  }
}

output containerAppInfo object = {
  id: containerApp.outputs.id
  name: containerApp.outputs.name
  fqdn: containerApp.outputs.fqdn
  url: containerApp.outputs.url
  latestRevisionFqdn: containerApp.outputs.latestRevisionFqdn
  latestRevisionUrl: containerApp.outputs.latestRevisionUrl
}
