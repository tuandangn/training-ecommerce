param(
    [switch]$UpdateBaseline,
    [string]$ViewsRoot = "NamEcommerce/Presentation/NamEcommerce.Web/Views",
    [string]$BaselinePath = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")
$viewsPath = Join-Path $repoRoot $ViewsRoot
if ([string]::IsNullOrWhiteSpace($BaselinePath)) {
    $BaselinePath = Join-Path $PSScriptRoot "ui-lint-baseline.json"
}

if (-not (Test-Path -LiteralPath $viewsPath)) {
    Write-Error "Views root not found: $viewsPath"
}

$files = Get-ChildItem -LiteralPath $viewsPath -Recurse -Filter "*.cshtml" -File
$styleBlockCount = 0
$inlineStyleCount = 0
$filesWithStyleBlocks = New-Object System.Collections.Generic.List[string]
$filesWithInlineStyles = New-Object System.Collections.Generic.List[string]

foreach ($file in $files) {
    $text = Get-Content -LiteralPath $file.FullName -Raw
    $styleBlocks = ([regex]::Matches($text, "<style\b")).Count
    $inlineStyles = ([regex]::Matches($text, "style\s*=")).Count

    if ($styleBlocks -gt 0) {
        $filesWithStyleBlocks.Add($file.FullName.Replace($repoRoot.Path + [IO.Path]::DirectorySeparatorChar, ""))
    }

    if ($inlineStyles -gt 0) {
        $filesWithInlineStyles.Add($file.FullName.Replace($repoRoot.Path + [IO.Path]::DirectorySeparatorChar, ""))
    }

    $styleBlockCount += $styleBlocks
    $inlineStyleCount += $inlineStyles
}

$result = [pscustomobject]@{
    viewsRoot = $ViewsRoot
    fileCount = $files.Count
    styleBlockCount = $styleBlockCount
    inlineStyleCount = $inlineStyleCount
    filesWithStyleBlocks = @($filesWithStyleBlocks)
    filesWithInlineStyles = @($filesWithInlineStyles)
}

if ($UpdateBaseline) {
    $baseline = [pscustomobject]@{
        viewsRoot = $ViewsRoot
        generatedAt = (Get-Date).ToString("yyyy-MM-dd")
        styleBlockCount = $styleBlockCount
        inlineStyleCount = $inlineStyleCount
    }
    $baseline | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $BaselinePath -Encoding UTF8
    Write-Host "Updated UI lint baseline: $BaselinePath"
    Write-Host "styleBlockCount=$styleBlockCount inlineStyleCount=$inlineStyleCount"
    exit 0
}

if (-not (Test-Path -LiteralPath $BaselinePath)) {
    Write-Error "Baseline not found: $BaselinePath. Run tools/ui-lint.ps1 -UpdateBaseline first."
}

$expected = Get-Content -LiteralPath $BaselinePath -Raw | ConvertFrom-Json
$failed = $false

if ($styleBlockCount -gt [int]$expected.styleBlockCount) {
    Write-Error "styleBlockCount increased: $styleBlockCount > $($expected.styleBlockCount)"
    $failed = $true
}

if ($inlineStyleCount -gt [int]$expected.inlineStyleCount) {
    Write-Error "inlineStyleCount increased: $inlineStyleCount > $($expected.inlineStyleCount)"
    $failed = $true
}

Write-Host "UI lint counts: styleBlockCount=$styleBlockCount/$($expected.styleBlockCount) inlineStyleCount=$inlineStyleCount/$($expected.inlineStyleCount)"
if ($failed) {
    exit 1
}
