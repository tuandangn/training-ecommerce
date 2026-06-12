param(
    [string[]]$Urls = @("/design"),
    [string]$BaseUrl = "http://localhost:5146",
    [string]$OutputDir = "artifacts/ui-screenshots",
    [int]$ReadyTimeoutSeconds = 60,
    [string]$ViewportSize = "1440,1200",
    [switch]$FullPage
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")
$outputPath = Join-Path $repoRoot $OutputDir
$logsDir = Join-Path $outputPath "logs"
New-Item -ItemType Directory -Force -Path $outputPath, $logsDir | Out-Null

$localPlaywright = Join-Path $repoRoot "node_modules/.bin/playwright.cmd"
$globalPlaywright = Get-Command "playwright.cmd" -ErrorAction SilentlyContinue

if (Test-Path -LiteralPath $localPlaywright) {
    $playwrightCommand = $localPlaywright
}
elseif ($globalPlaywright) {
    $playwrightCommand = $globalPlaywright.Source
}
else {
    Write-Error "Playwright CLI was not found. Install it before running screenshots: npm install --save-dev @playwright/test && npx playwright install chromium"
}

$projectPath = Join-Path $repoRoot "NamEcommerce/Presentation/NamEcommerce.Web/NamEcommerce.Web.csproj"
$stdoutPath = Join-Path $logsDir "web.out.log"
$stderrPath = Join-Path $logsDir "web.err.log"

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:SeedData__RunOnStartup = "false"
$env:HostedServices__RunOnStartup = "false"
$env:Logging__EventLog__LogLevel__Default = "None"
$env:ConnectionStrings__NamEcommerceEfDbContext = "Data Source=.\SQLEXPRESS;Database=NamEcommerceDb;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=True;TrustServerCertificate=True;Encrypt=False;"

$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = "dotnet"
$startInfo.WorkingDirectory = $repoRoot.Path
$startInfo.UseShellExecute = $false
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError = $true
$startInfo.Arguments = "run --project `"$projectPath`" --no-build --no-launch-profile --urls $BaseUrl"

$webProcess = [System.Diagnostics.Process]::Start($startInfo)

try {
    $deadline = [DateTimeOffset]::UtcNow.AddSeconds($ReadyTimeoutSeconds)
    $ready = $false
    $probeUrl = "$BaseUrl$($Urls[0])"

    while ([DateTimeOffset]::UtcNow -lt $deadline) {
        if ($webProcess.HasExited) {
            break
        }

        try {
            $response = Invoke-WebRequest -Uri $probeUrl -UseBasicParsing -TimeoutSec 5
            if ([int]$response.StatusCode -lt 500) {
                $ready = $true
                break
            }
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }

    if (-not $ready) {
        Write-Error "Web app did not become ready at $probeUrl."
    }

    foreach ($url in $Urls) {
        $absoluteUrl = if ($url.StartsWith("http", [StringComparison]::OrdinalIgnoreCase)) { $url } else { "$BaseUrl$url" }
        $name = ($url.Trim("/") -replace "[^a-zA-Z0-9_-]+", "-")
        if ([string]::IsNullOrWhiteSpace($name)) {
            $name = "home"
        }

        $screenshotPath = Join-Path $outputPath "$name.png"
        $arguments = @(
            "screenshot",
            "--browser=chromium",
            "--viewport-size=$ViewportSize"
        )

        if ($FullPage) {
            $arguments += "--full-page"
        }

        $arguments += $absoluteUrl
        $arguments += $screenshotPath

        & $playwrightCommand @arguments

        if (-not (Test-Path -LiteralPath $screenshotPath)) {
            Write-Error "Screenshot was not created: $screenshotPath"
        }

        Write-Host "Captured $screenshotPath"
    }
}
finally {
    if ($webProcess -and -not $webProcess.HasExited) {
        $webProcess.Kill()
        $webProcess.WaitForExit()
    }

    if ($webProcess) {
        $webProcess.StandardOutput.ReadToEnd() | Set-Content -LiteralPath $stdoutPath -Encoding UTF8
        $webProcess.StandardError.ReadToEnd() | Set-Content -LiteralPath $stderrPath -Encoding UTF8
    }
}
