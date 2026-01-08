$tokens = $null
$errors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseFile(
    "$PSScriptRoot\Build-Portable.ps1",
    [ref]$tokens,
    [ref]$errors
)

if ($errors.Count -gt 0) {
    Write-Host "Parse errors found:" -ForegroundColor Red
    foreach ($err in $errors) {
        Write-Host "  Line $($err.Extent.StartLineNumber): $($err.Message)" -ForegroundColor Yellow
    }
} else {
    Write-Host "No parse errors found!" -ForegroundColor Green
}
