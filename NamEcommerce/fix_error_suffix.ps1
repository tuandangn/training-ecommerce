$directory = "d:\Learning\NamTraining\training-ecommerce\NamEcommerce\Domain\NamEcommerce.Domain.Shared\Exceptions"
$files = Get-ChildItem -Path $directory -Filter "*Exception*.cs" -Recurse

foreach ($file in $files) {
    $content = Get-Content -Path $file.FullName -Raw
    
    # Replace "Error.SomethingException" with "Error.Something"
    $newContent = [regex]::Replace($content, '"Error\.([A-Za-z0-9_]+)Exception"', '"Error.$1"')
    
    if ($newContent -ne $content) {
        Set-Content -Path $file.FullName -Value $newContent -Encoding UTF8
        Write-Host "Fixed suffix in $($file.Name)"
    }
}
