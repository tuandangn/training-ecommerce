$directory = "d:\Learning\NamTraining\training-ecommerce\NamEcommerce\Domain\NamEcommerce.Domain.Shared\Exceptions"
$files = Get-ChildItem -Path $directory -Filter "*Exception*.cs" -Recurse

foreach ($file in $files) {
    if ($file.Name -eq "NamEcommerceDomainException.cs") { continue }
    
    $content = Get-Content -Path $file.FullName -Raw
    
    # Check if it's a primary constructor exception
    if ($content -match 'class\s+([A-Za-z0-9_]+Exception)') {
        $className = $matches[1]
        $errorCode = "`"Error.$className`""
        
        # Match the primary constructor parameters
        $argsList = @()
        if ($content -match "class\s+$className\((.*?)\)") {
            $paramsStr = $matches[1]
            if (-not [string]::IsNullOrWhiteSpace($paramsStr)) {
                $params = $paramsStr -split ','
                foreach ($param in $params) {
                    $parts = $param.Trim() -split ' '
                    if ($parts.Length -ge 2) {
                        # Add the arg name, removing any defaults like = null
                        $argName = $parts[-1].Replace("=null","").Replace("= null","").Trim()
                        $argsList += $argName
                    }
                }
            }
        }
        
        $baseCall = " : NamEcommerceDomainException($errorCode"
        if ($argsList.Count -gt 0) {
            $baseCall += ", " + ($argsList -join ", ")
        }
        $baseCall += ")"
        
        # Replace : Exception(...) with our new baseCall
        $newContent = [regex]::Replace($content, ':\s*Exception\s*\(.*?\)', $baseCall)
        
        # Replace : Exception without parens (if any)
        $newContent = [regex]::Replace($newContent, ':\s*Exception$', $baseCall, [System.Text.RegularExpressions.RegexOptions]::Multiline)

        if ($newContent -ne $content) {
            Set-Content -Path $file.FullName -Value $newContent -Encoding UTF8
            Write-Host "Refactored $($file.Name)"
        }
    }
}
