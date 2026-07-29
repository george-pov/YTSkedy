[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-RepositoryRoot {
    [CmdletBinding()]
    param()

    $repositoryRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
    $iconSourcePath = Join-Path $repositoryRoot.ProviderPath `
        'src\ui\src\app\shared\components\icon\icon.ts'

    if (-not (Test-Path -LiteralPath $iconSourcePath -PathType Leaf)) {
        throw "Could not resolve the repository root from '$PSScriptRoot'."
    }

    return $repositoryRoot.ProviderPath
}

function Get-SupportedIconNames {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    $source = Get-Content -LiteralPath $Path -Raw
    $arrayMatch = [regex]::Match(
        $source,
        'export const supportedIconNames = \[(?<names>.*?)\] as const;',
        [Text.RegularExpressions.RegexOptions]::Singleline
    )

    if (-not $arrayMatch.Success) {
        throw "Could not find the supportedIconNames array in '$Path'."
    }

    $nameBlock = $arrayMatch.Groups['names'].Value
    $nameMatches = [regex]::Matches($nameBlock, "'(?<name>[a-z0-9_]+)'")
    $iconNames = @($nameMatches | ForEach-Object { $_.Groups['name'].Value })

    if ($iconNames.Count -eq 0) {
        throw "The supportedIconNames array in '$Path' is empty."
    }

    $unparsedContent = [regex]::Replace($nameBlock, "'[a-z0-9_]+',?", '')
    if (-not [string]::IsNullOrWhiteSpace($unparsedContent)) {
        throw "The supportedIconNames array in '$Path' contains unsupported syntax."
    }

    $sortedUniqueNames = @($iconNames | Sort-Object -Unique)
    if (($iconNames -join ',') -cne ($sortedUniqueNames -join ',')) {
        throw 'supportedIconNames must be alphabetically sorted and contain unique names.'
    }

    return $iconNames
}

function Test-Woff2File {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    $bytes = [IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -lt 4) {
        return $false
    }

    $signature = [Text.Encoding]::ASCII.GetString($bytes, 0, 4)
    return $signature -ceq 'wOF2'
}

$repositoryRoot = Resolve-RepositoryRoot
$iconSourcePath = Join-Path $repositoryRoot `
    'src\ui\src\app\shared\components\icon\icon.ts'
$fontDirectory = Join-Path $repositoryRoot `
    'src\ui\public\fonts\material-symbols-outlined'
$fontPath = Join-Path $fontDirectory 'material-symbols-outlined.woff2'
$sourcePath = Join-Path $fontDirectory 'SOURCE.md'
$iconNames = @(Get-SupportedIconNames -Path $iconSourcePath)
$iconNamesQuery = $iconNames -join ','

$opticalSize = 24
$weight = 400
$fill = 1
$grade = 0
$cssRequest = 'https://fonts.googleapis.com/css2?' +
    "family=Material+Symbols+Outlined:opsz,wght,FILL,GRAD@$opticalSize,$weight,$fill,$grade" +
    "&icon_names=$iconNamesQuery&display=block"
$browserUserAgent = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) ' +
    'AppleWebKit/537.36 (KHTML, like Gecko) ' +
    'Chrome/138.0.0.0 Safari/537.36'
$requestHeaders = @{ 'User-Agent' = $browserUserAgent }

$cssResponse = Invoke-WebRequest -Uri $cssRequest -Headers $requestHeaders
$fontUrlMatch = [regex]::Match($cssResponse.Content, 'url\((?<url>https://[^)]+)\)')

if (-not $fontUrlMatch.Success) {
    throw 'The Google Fonts CSS response did not contain a font URL.'
}

if ($cssResponse.Content -notmatch "format\('woff2'\)") {
    throw 'The Google Fonts CSS response did not provide a WOFF2 font.'
}

New-Item -ItemType Directory -Path $fontDirectory -Force | Out-Null
$temporaryFontPath = Join-Path ([IO.Path]::GetTempPath()) `
    "ytskedy-material-symbols-$([guid]::NewGuid()).woff2"

try {
    Invoke-WebRequest `
        -Uri $fontUrlMatch.Groups['url'].Value `
        -Headers $requestHeaders `
        -OutFile $temporaryFontPath

    if (-not (Test-Woff2File -Path $temporaryFontPath)) {
        throw 'The downloaded file is not a valid WOFF2 font.'
    }

    Move-Item -LiteralPath $temporaryFontPath -Destination $fontPath -Force
}
finally {
    if (Test-Path -LiteralPath $temporaryFontPath -PathType Leaf) {
        Remove-Item -LiteralPath $temporaryFontPath -Force
    }
}

$fontFile = Get-Item -LiteralPath $fontPath
$fontHash = Get-FileHash -LiteralPath $fontPath -Algorithm SHA256
$downloadDate = Get-Date -Format 'yyyy-MM-dd'
$sourceLines = @(
    '# Material Symbols Outlined Font Source'
    ''
    'This folder contains an optimized subset of the official Google Material'
    'Symbols Outlined font for use through Angular Material `mat-icon`.'
    ''
    '- Generator: `scripts/ui/Update-MaterialSymbolsSubset.ps1`'
    '- Family: Material Symbols Outlined'
    ('- Icons: `{0}`' -f $iconNamesQuery)
    ('- Axes: optical size {0}, weight {1}, fill {2}, grade {3}' -f `
        $opticalSize, $weight, $fill, $grade)
    ('- CSS request: `{0}`' -f $cssRequest)
    ('- Downloaded: {0}' -f $downloadDate)
    ('- SHA-256: `{0}`' -f $fontHash.Hash)
    ''
    'Material Symbols are licensed under the Apache License 2.0. The repository''s'
    'complete copy of that license is stored at'
    '`src/ui/src/app/shared/components/icon/LICENSE.txt`.'
)

[IO.File]::WriteAllLines(
    $sourcePath,
    $sourceLines,
    [Text.UTF8Encoding]::new($false)
)

Write-Output "Generated: $fontPath"
Write-Output "Icons: $iconNamesQuery"
Write-Output "Size: $($fontFile.Length) bytes"
Write-Output "SHA-256: $($fontHash.Hash)"
