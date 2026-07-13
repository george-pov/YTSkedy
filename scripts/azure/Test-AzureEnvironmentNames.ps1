[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('dev', 'prod')]
    [string]$Environment,

    [string]$ParameterFile
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-RepositoryRoot {
    [CmdletBinding()]
    param()

    $repositoryRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
    $mainBicepFile = Join-Path $repositoryRoot.ProviderPath 'bicep\main.bicep'

    if (-not (Test-Path -LiteralPath $mainBicepFile -PathType Leaf)) {
        throw "Could not resolve the repository root from '$PSScriptRoot'."
    }

    return $repositoryRoot.ProviderPath
}

function Get-TrackedResourceNames {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter(Mandatory)]
        [ValidateSet('dev', 'prod')]
        [string]$SelectedEnvironment
    )

    $expectedParameters = @(
        'environmentName'
        'resourceGroupName'
        'functionAppName'
        'functionPlanName'
        'functionStorageAccountName'
        'applicationStorageAccountName'
        'uiStorageAccountName'
        'applicationInsightsName'
        'logAnalyticsWorkspaceName'
        'deploymentIdentityName'
        'federatedCredentialName'
        'actionGroupName'
        'actionGroupShortName'
        'failureAnomalyAlertName'
        'deploymentStorageContainerName'
    )

    if ($SelectedEnvironment -eq 'prod') {
        $expectedParameters += 'resourceGroupLockName'
    }

    $definitions = @{}

    foreach ($line in Get-Content -LiteralPath $Path) {
        if ($line -notmatch '^\s*param\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\b(?<expression>.*)$') {
            continue
        }

        $parameterName = $Matches.name
        if ($parameterName -notin $expectedParameters) {
            continue
        }

        if (-not $definitions.ContainsKey($parameterName)) {
            $definitions[$parameterName] = @()
        }

        $definitions[$parameterName] += $Matches.expression.Trim()
    }

    $resourceNames = [ordered]@{}

    foreach ($parameterName in $expectedParameters) {
        if (-not $definitions.ContainsKey($parameterName)) {
            throw "Expected literal name parameter '$parameterName' is missing from '$Path'."
        }

        $parameterDefinitions = @($definitions[$parameterName])
        if ($parameterDefinitions.Count -ne 1) {
            throw "Expected name parameter '$parameterName' is duplicated in '$Path'."
        }

        $expression = $parameterDefinitions[0]
        if ($expression -notmatch "^=\s*'(?<value>[A-Za-z0-9-]+)'\s*(?://.*)?$") {
            throw "Expected name parameter '$parameterName' must use one literal string in '$Path'."
        }

        $value = $Matches.value
        if ($value.IndexOf($SelectedEnvironment, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw "Expected name parameter '$parameterName' does not contain environment '$SelectedEnvironment'."
        }

        $resourceNames[$parameterName] = $value
    }

    if ($resourceNames.environmentName -cne $SelectedEnvironment) {
        throw "Parameter 'environmentName' must equal '$SelectedEnvironment'."
    }

    $duplicateValues = @(
        $resourceNames.GetEnumerator() |
            Where-Object Key -ne 'environmentName' |
            Group-Object -Property Value |
            Where-Object Count -gt 1
    )

    if ($duplicateValues.Count -gt 0) {
        throw "Expected resource names must be unique in '$Path'."
    }

    return $resourceNames
}

function Invoke-AzureCli {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    $azureCli = Get-Command az -CommandType Application -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($null -eq $azureCli) {
        throw "Azure CLI executable 'az' was not found on PATH."
    }

    $output = & $azureCli.Source @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    $outputText = ($output | ForEach-Object { $_.ToString() }) -join [Environment]::NewLine

    if ($exitCode -ne 0) {
        if ([string]::IsNullOrWhiteSpace($outputText)) {
            $outputText = "Azure CLI exited with code $exitCode."
        }

        throw $outputText.Trim()
    }

    return $outputText.Trim()
}

function Test-StorageAccountName {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$ExpectedResourceGroupName
    )

    $response = Invoke-AzureCli -Arguments @(
        'storage'
        'account'
        'check-name'
        '--name'
        $Name
        '--output'
        'json'
        '--only-show-errors'
    )

    try {
        $availability = $response | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        throw "Azure returned an invalid storage name-availability response. $($_.Exception.Message)"
    }

    if ($null -eq $availability.PSObject.Properties['nameAvailable']) {
        throw "Azure omitted 'nameAvailable' from the storage name-availability response."
    }

    if ($availability.nameAvailable -eq $true) {
        return 'Available'
    }

    if ([string]$availability.reason -eq 'Invalid') {
        return 'Invalid'
    }

    $existingResourcesResponse = Invoke-AzureCli -Arguments @(
        'resource'
        'list'
        '--name'
        $Name
        '--resource-type'
        'Microsoft.Storage/storageAccounts'
        '--output'
        'json'
        '--only-show-errors'
    )

    try {
        $existingResources = @($existingResourcesResponse | ConvertFrom-Json -ErrorAction Stop)
    }
    catch {
        throw "Azure returned an invalid storage ownership response. $($_.Exception.Message)"
    }

    $expectedResource = $existingResources | Where-Object {
        $_.type -ieq 'Microsoft.Storage/storageAccounts' -and
        $_.resourceGroup -ieq $ExpectedResourceGroupName
    }

    if ($null -ne $expectedResource) {
        return 'ExistingExpected'
    }

    return 'Unavailable'
}

function Test-FunctionAppName {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$ExpectedResourceGroupName
    )

    $subscriptionId = Invoke-AzureCli -Arguments @(
        'account'
        'show'
        '--query'
        'id'
        '--output'
        'tsv'
        '--only-show-errors'
    )

    if ([string]::IsNullOrWhiteSpace($subscriptionId)) {
        throw 'Azure CLI returned an empty subscription id.'
    }

    $requestBody = @{
        name = $Name
        type = 'Microsoft.Web/sites'
    } | ConvertTo-Json -Compress

    if ($env:OS -eq 'Windows_NT') {
        $requestBody = $requestBody.Replace('"', '\"')
    }

    $response = Invoke-AzureCli -Arguments @(
        'rest'
        '--method'
        'post'
        '--url'
        "https://management.azure.com/subscriptions/$subscriptionId/providers/Microsoft.Web/checkNameAvailability?api-version=2024-04-01"
        '--body'
        $requestBody
        '--headers'
        'Content-Type=application/json'
        '--output'
        'json'
        '--only-show-errors'
    )

    try {
        $availability = $response | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        throw "Azure returned an invalid Function App name-availability response. $($_.Exception.Message)"
    }

    if ($null -eq $availability.PSObject.Properties['nameAvailable']) {
        throw "Azure omitted 'nameAvailable' from the Function App name-availability response."
    }

    if ($availability.nameAvailable -eq $true) {
        return 'Available'
    }

    if ([string]$availability.reason -eq 'Invalid') {
        return 'Invalid'
    }

    $existingResourcesResponse = Invoke-AzureCli -Arguments @(
        'resource'
        'list'
        '--name'
        $Name
        '--resource-type'
        'Microsoft.Web/sites'
        '--output'
        'json'
        '--only-show-errors'
    )

    try {
        $existingResources = @($existingResourcesResponse | ConvertFrom-Json -ErrorAction Stop)
    }
    catch {
        throw "Azure returned an invalid Function App ownership response. $($_.Exception.Message)"
    }

    $expectedResource = $existingResources | Where-Object {
        $_.type -ieq 'Microsoft.Web/sites' -and
        $_.resourceGroup -ieq $ExpectedResourceGroupName
    }

    if ($null -ne $expectedResource) {
        return 'ExistingExpected'
    }

    return 'Unavailable'
}

$repositoryRoot = Resolve-RepositoryRoot

if ([string]::IsNullOrWhiteSpace($ParameterFile)) {
    $ParameterFile = Join-Path $repositoryRoot "bicep\environments\$Environment.bicepparam"
}
elseif (-not [IO.Path]::IsPathRooted($ParameterFile)) {
    $ParameterFile = Join-Path $repositoryRoot $ParameterFile
}

if (-not (Test-Path -LiteralPath $ParameterFile -PathType Leaf)) {
    throw "Parameter file '$ParameterFile' does not exist."
}

$resolvedParameterFile = (Resolve-Path -LiteralPath $ParameterFile).ProviderPath
$resourceNames = Get-TrackedResourceNames -Path $resolvedParameterFile -SelectedEnvironment $Environment
$failedChecks = [Collections.Generic.List[string]]::new()

foreach ($storageAccountName in @(
    $resourceNames.functionStorageAccountName
    $resourceNames.applicationStorageAccountName
    $resourceNames.uiStorageAccountName
)) {
    try {
        $result = Test-StorageAccountName `
            -Name $storageAccountName `
            -ExpectedResourceGroupName $resourceNames.resourceGroupName
    }
    catch {
        Write-Output "StorageAccount $storageAccountName CheckFailed"
        throw
    }

    Write-Output "StorageAccount $storageAccountName $result"

    if ($result -in @('Invalid', 'Unavailable')) {
        $failedChecks.Add("StorageAccount/$storageAccountName")
    }
}

try {
    $functionAppResult = Test-FunctionAppName `
        -Name $resourceNames.functionAppName `
        -ExpectedResourceGroupName $resourceNames.resourceGroupName
}
catch {
    Write-Output "FunctionApp $($resourceNames.functionAppName) CheckFailed"
    throw
}

Write-Output "FunctionApp $($resourceNames.functionAppName) $functionAppResult"

if ($functionAppResult -in @('Invalid', 'Unavailable')) {
    $failedChecks.Add("FunctionApp/$($resourceNames.functionAppName)")
}

if ($failedChecks.Count -gt 0) {
    throw 'One or more global Azure resource names are invalid or unavailable.'
}
