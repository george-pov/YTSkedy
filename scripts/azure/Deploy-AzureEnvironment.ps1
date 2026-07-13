[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)]
    [ValidateSet('dev', 'prod')]
    [string]$Environment,

    [switch]$ValidateOnly,

    [switch]$Apply
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

function Assert-AzureCli {
    [CmdletBinding()]
    param()

    $azureCli = Get-Command az -CommandType Application -ErrorAction SilentlyContinue |
        Select-Object -First 1

    if ($null -eq $azureCli) {
        throw "Azure CLI executable 'az' was not found on PATH."
    }

    $null = Invoke-AzureCli -Arguments @(
        'version'
        '--output'
        'json'
        '--only-show-errors'
    )
}

function Assert-BicepCli {
    [CmdletBinding()]
    param()

    try {
        $null = Invoke-AzureCli -Arguments @(
            'bicep'
            'version'
            '--only-show-errors'
        )
    }
    catch {
        throw "Azure CLI-managed Bicep is unavailable. Run 'az bicep install' and retry. $($_.Exception.Message)"
    }
}

function Assert-RequiredEnvironmentVariables {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateSet('dev', 'prod')]
        [string]$SelectedEnvironment
    )

    $prefix = "YTSKEDY_$($SelectedEnvironment.ToUpperInvariant())"
    $requiredVariables = @(
        "${prefix}_AUTH_INSTANCE"
        "${prefix}_AUTH_TENANT_ID"
        "${prefix}_AUTH_CLIENT_ID"
        "${prefix}_AUTH_ISSUER"
        "${prefix}_ALERT_RECEIVER_NAME"
        "${prefix}_ALERT_RECEIVER_EMAIL_ADDRESS"
    )

    $missingVariables = @(
        $requiredVariables | Where-Object {
            [string]::IsNullOrWhiteSpace(
                [Environment]::GetEnvironmentVariable($_, 'Process')
            )
        }
    )

    if ($missingVariables.Count -gt 0) {
        throw "Missing required process environment variables: $($missingVariables -join ', ')."
    }
}

function Get-AzureSubscriptionContext {
    [CmdletBinding()]
    param()

    $response = Invoke-AzureCli -Arguments @(
        'account'
        'show'
        '--query'
        '{name:name,id:id,tenantId:tenantId}'
        '--output'
        'json'
        '--only-show-errors'
    )

    try {
        $context = $response | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        throw "Azure CLI returned an invalid subscription context. $($_.Exception.Message)"
    }

    if (
        [string]::IsNullOrWhiteSpace([string]$context.name) -or
        [string]::IsNullOrWhiteSpace([string]$context.id) -or
        [string]::IsNullOrWhiteSpace([string]$context.tenantId)
    ) {
        throw 'Azure CLI returned an incomplete subscription context.'
    }

    $parsedId = [Guid]::Empty
    if (-not [Guid]::TryParse([string]$context.id, [ref]$parsedId)) {
        throw 'Azure CLI returned an invalid subscription id.'
    }

    $parsedTenantId = [Guid]::Empty
    if (-not [Guid]::TryParse([string]$context.tenantId, [ref]$parsedTenantId)) {
        throw 'Azure CLI returned an invalid tenant id.'
    }

    return $context
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

function Invoke-BicepBuild {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$MainBicepFile
    )

    $null = Invoke-AzureCli -Arguments @(
        'bicep'
        'build'
        '--file'
        $MainBicepFile
        '--stdout'
        '--only-show-errors'
    )

    Write-Output 'BicepBuild Succeeded'
}

function Invoke-SubscriptionValidation {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ParameterFile,

        [Parameter(Mandatory)]
        [string]$Location,

        [Parameter(Mandatory)]
        [string]$DeploymentName
    )

    $provisioningState = Invoke-AzureCli -Arguments @(
        'deployment'
        'sub'
        'validate'
        '--name'
        $DeploymentName
        '--location'
        $Location
        '--parameters'
        $ParameterFile
        '--query'
        'properties.provisioningState'
        '--output'
        'tsv'
        '--only-show-errors'
    )

    if ($provisioningState -ne 'Succeeded') {
        throw "Subscription validation returned '$provisioningState'."
    }

    Write-Output 'SubscriptionValidation Succeeded'
}

function Invoke-SubscriptionWhatIf {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ParameterFile,

        [Parameter(Mandatory)]
        [string]$Location,

        [Parameter(Mandatory)]
        [string]$DeploymentName
    )

    $response = Invoke-AzureCli -Arguments @(
        'deployment'
        'sub'
        'what-if'
        '--name'
        $DeploymentName
        '--location'
        $Location
        '--parameters'
        $ParameterFile
        '--result-format'
        'ResourceIdOnly'
        '--no-pretty-print'
        '--output'
        'json'
        '--only-show-errors'
    )

    try {
        $whatIfResult = $response | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        throw "Azure CLI returned an invalid subscription what-if response. $($_.Exception.Message)"
    }

    if ($whatIfResult.status -ne 'Succeeded') {
        throw "Subscription what-if returned '$($whatIfResult.status)'."
    }

    foreach ($change in @($whatIfResult.changes)) {
        Write-Output "WhatIf $($change.changeType) $($change.resourceId)"
    }

    Write-Output 'SubscriptionWhatIf Succeeded'
}

function Invoke-SubscriptionDeployment {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ParameterFile,

        [Parameter(Mandatory)]
        [string]$Location,

        [Parameter(Mandatory)]
        [string]$DeploymentName
    )

    $response = Invoke-AzureCli -Arguments @(
        'deployment'
        'sub'
        'create'
        '--name'
        $DeploymentName
        '--location'
        $Location
        '--parameters'
        $ParameterFile
        '--query'
        'properties.outputs'
        '--output'
        'json'
        '--only-show-errors'
    )

    try {
        $outputs = $response | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        throw "Azure CLI returned invalid deployment outputs. $($_.Exception.Message)"
    }

    $approvedOutputs = @(
        'resourceGroupName'
        'functionAppName'
        'functionAppUrl'
        'uiStorageAccountName'
        'uiStaticWebsiteUrl'
        'deploymentIdentityClientId'
        'deploymentIdentityPrincipalId'
        'applicationInsightsName'
        'logAnalyticsWorkspaceName'
    )

    foreach ($outputName in $approvedOutputs) {
        $outputProperty = $outputs.PSObject.Properties[$outputName]
        if ($null -eq $outputProperty) {
            throw "Deployment output '$outputName' is missing."
        }

        Write-Output "$outputName=$($outputProperty.Value.value)"
    }
}

$whatIfMode = $PSBoundParameters.ContainsKey('WhatIf') -and
    [bool]$PSBoundParameters.WhatIf
$selectedModeCount = @($ValidateOnly, $whatIfMode, $Apply).Where({ [bool]$_ }).Count

if ($selectedModeCount -ne 1) {
    throw 'Select exactly one mode: -ValidateOnly, -WhatIf, or -Apply.'
}

if (
    $Apply -and
    $PSBoundParameters.ContainsKey('Confirm') -and
    -not [bool]$PSBoundParameters.Confirm
) {
    throw 'Apply cannot suppress confirmation. Remove -Confirm:$false and confirm the deployment interactively.'
}

$repositoryRoot = Resolve-RepositoryRoot
$mainBicepFile = Join-Path $repositoryRoot 'bicep\main.bicep'
$parameterFile = Join-Path $repositoryRoot "bicep\environments\$Environment.bicepparam"
$nameCheckScript = Join-Path $repositoryRoot 'scripts\azure\Test-AzureEnvironmentNames.ps1'

foreach ($requiredFile in @($mainBicepFile, $parameterFile, $nameCheckScript)) {
    if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
        throw "Required file '$requiredFile' does not exist."
    }
}

$targetParameters = @{
    location = @()
    resourceGroupName = @()
}

foreach ($line in Get-Content -LiteralPath $parameterFile) {
    if ($line -notmatch "^\s*param\s+(?<name>location|resourceGroupName)\s*=\s*'(?<value>[A-Za-z0-9-]+)'\s*(?://.*)?$") {
        continue
    }

    $targetParameters[$Matches.name] += $Matches.value
}

foreach ($parameterName in $targetParameters.Keys) {
    if ($targetParameters[$parameterName].Count -ne 1) {
        throw "Parameter '$parameterName' must have one literal value in '$parameterFile'."
    }
}

$location = $targetParameters.location[0]
$targetResourceGroup = $targetParameters.resourceGroupName[0]
$deploymentName = "ytskedy-$Environment-$([DateTime]::UtcNow.ToString('yyyyMMddHHmmss', [Globalization.CultureInfo]::InvariantCulture))"

Assert-AzureCli
Assert-BicepCli
Assert-RequiredEnvironmentVariables -SelectedEnvironment $Environment
$subscriptionContext = Get-AzureSubscriptionContext

Write-Output "Environment $Environment"
Write-Output "Location $location"
Write-Output "ResourceGroup $targetResourceGroup"
Write-Output "Subscription $($subscriptionContext.name) $($subscriptionContext.id)"
Write-Output "Tenant $($subscriptionContext.tenantId)"
Write-Output "Deployment $deploymentName"

& $nameCheckScript -Environment $Environment -ParameterFile $parameterFile
Invoke-BicepBuild -MainBicepFile $mainBicepFile

if ($ValidateOnly) {
    Invoke-SubscriptionValidation `
        -ParameterFile $parameterFile `
        -Location $location `
        -DeploymentName $deploymentName

    return
}

if ($whatIfMode) {
    Invoke-SubscriptionWhatIf `
        -ParameterFile $parameterFile `
        -Location $location `
        -DeploymentName $deploymentName

    return
}

Invoke-SubscriptionValidation `
    -ParameterFile $parameterFile `
    -Location $location `
    -DeploymentName $deploymentName

Invoke-SubscriptionWhatIf `
    -ParameterFile $parameterFile `
    -Location $location `
    -DeploymentName $deploymentName

$previousConfirmPreference = $ConfirmPreference
$ConfirmPreference = 'High'

try {
    $deploymentConfirmed = $PSCmdlet.ShouldProcess(
        $targetResourceGroup,
        "Deploy '$Environment' Azure environment using '$deploymentName'"
    )
}
finally {
    $ConfirmPreference = $previousConfirmPreference
}

if (-not $deploymentConfirmed) {
    throw 'Azure deployment was not confirmed. No Azure resources were changed.'
}

Invoke-SubscriptionDeployment `
    -ParameterFile $parameterFile `
    -Location $location `
    -DeploymentName $deploymentName
