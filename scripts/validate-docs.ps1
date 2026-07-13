[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()

function Add-Failure {
    param([Parameter(Mandatory)][string]$Message)

    $failures.Add($Message)
}

$repoRoot = (& git rev-parse --show-toplevel).Trim()
if (-not $repoRoot) {
    throw 'Could not resolve the repository root.'
}

Push-Location $repoRoot
try {
    $stableFiles = @(
        'README.md'
        'CONTEXT.md'
        'src/ui/README.md'
        'docs/README.md'
        'docs/api/README.md'
        'docs/api/http/README.md'
        'docs/api/http/calendar-events.md'
        'docs/api/http/calendar-event-defaults.md'
        'docs/api/http/calendar-event-thumbnails.md'
        'docs/api/http/platforms.md'
        'docs/api/http/platform-publications.md'
        'docs/api/http/templates.md'
        'docs/ui/README.md'
        'docs/ui/routes.md'
        'docs/ui/operations/deployment.md'
        'docs/development/domain-vocabulary.md'
        'docs/development/naming-guidance.md'
    )

    foreach ($file in $stableFiles) {
        if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
            Add-Failure "Missing canonical documentation file: $file"
        }
    }

    $trackedContext = @(& git ls-files -- CONTEXT.md)
    if ($trackedContext -notcontains 'CONTEXT.md') {
        Add-Failure 'CONTEXT.md must be tracked by git.'
    }

    $trackedMarkdown = @(& git ls-files '*.md') |
        ForEach-Object { $_ -replace '\\', '/' } |
        Where-Object {
            $_ -eq 'README.md' -or
            $_ -eq 'CONTEXT.md' -or
            $_ -eq 'src/ui/README.md' -or
            $_ -like 'docs/*'
        }

    $durableMarkdown = @('README.md', 'CONTEXT.md', 'src/ui/README.md')
    $normalizedRoot = [System.IO.Path]::GetFullPath($repoRoot).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar) +
        [System.IO.Path]::DirectorySeparatorChar
    $durableMarkdown += @(Get-ChildItem -LiteralPath 'docs' -Recurse -File -Filter '*.md' |
        ForEach-Object {
            $_.FullName.Substring($normalizedRoot.Length) -replace '\\', '/'
        })
    $durableMarkdown = @($durableMarkdown | Sort-Object -Unique)

    $docsInventory = Get-Content -LiteralPath 'docs/README.md' -Raw
    foreach ($file in $durableMarkdown | Where-Object { $_ -like 'docs/*' }) {
        $relative = $file.Substring('docs/'.Length)
        if (-not $docsInventory.Contains("($relative)")) {
            Add-Failure "docs/README.md does not inventory $file"
        }
    }

    $rootInventoryTargets = @('../README.md', '../CONTEXT.md', '../src/ui/README.md')
    foreach ($target in $rootInventoryTargets) {
        if (-not $docsInventory.Contains("($target)")) {
            Add-Failure "docs/README.md does not list entrypoint $target"
        }
    }

    $areaIndexes = @(
        @{ Prefix = 'docs/api/'; Index = 'docs/api/README.md' }
        @{ Prefix = 'docs/ui/'; Index = 'docs/ui/README.md' }
    )

    foreach ($area in $areaIndexes) {
        $indexContent = Get-Content -LiteralPath $area.Index -Raw
        foreach ($file in $durableMarkdown | Where-Object {
            $_.StartsWith($area.Prefix) -and $_ -ne $area.Index
        }) {
            $relative = $file.Substring($area.Prefix.Length)
            if (-not $indexContent.Contains("($relative)")) {
                Add-Failure "$($area.Index) does not inventory $file"
            }
        }
    }

    $linkPattern = '(?<!\!)\[[^\]]+\]\((?<target>[^)]+)\)'
    foreach ($file in $durableMarkdown) {
        $content = Get-Content -LiteralPath $file -Raw
        foreach ($match in [regex]::Matches($content, $linkPattern)) {
            $target = $match.Groups['target'].Value.Trim().Trim('<', '>')
            if ($target -match '^(https?://|mailto:|#)') {
                continue
            }

            $pathPart = ($target -split '#', 2)[0]
            if ([string]::IsNullOrWhiteSpace($pathPart)) {
                continue
            }

            $sourcePath = Join-Path $repoRoot $file
            $sourceDirectory = Split-Path -Parent $sourcePath
            $candidate = [System.IO.Path]::GetFullPath(
                (Join-Path $sourceDirectory ([uri]::UnescapeDataString($pathPart))))

            if (-not (Test-Path -LiteralPath $candidate)) {
                Add-Failure "Broken internal link in ${file}: $target"
            }
        }
    }

    foreach ($file in $durableMarkdown | Where-Object { $_ -like 'docs/*' }) {
        $workReferences = @(Select-String -LiteralPath $file -Pattern '\.work/')
        foreach ($match in $workReferences) {
            Add-Failure "Durable docs reference .work/: ${file}:$($match.LineNumber)"
        }
    }

    foreach ($file in $durableMarkdown) {
        $trailing = @(Select-String -LiteralPath $file -Pattern '[ \t]+$')
        foreach ($match in $trailing) {
            Add-Failure "Trailing whitespace: ${file}:$($match.LineNumber)"
        }
    }

    $endpointDetails = @(Select-String `
        -LiteralPath 'docs/architecture/integration-contracts.md' `
        -Pattern '\b(GET|POST|PUT|PATCH|DELETE)\s+/api(?:/|\b)')
    foreach ($match in $endpointDetails) {
        Add-Failure "Integration ownership doc contains endpoint detail at line $($match.LineNumber)."
    }

    if ($failures.Count -gt 0) {
        foreach ($failure in $failures) {
            Write-Host "ERROR: $failure" -ForegroundColor Red
        }

        exit 1
    }

    Write-Host "Documentation validation passed for $($durableMarkdown.Count) durable Markdown files."
    Write-Host 'External URLs were not checked.'
}
finally {
    Pop-Location
}
