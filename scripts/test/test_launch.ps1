$exe = 'C:\Users\jeffr\Documents\Visual Studio Code Projects\VapourSynth GUI\src\gui\VapourSynthPortable\bin\Debug\net8.0-windows\VapourSynthPortable.exe'
$pinfo = New-Object System.Diagnostics.ProcessStartInfo
$pinfo.FileName = $exe
$pinfo.RedirectStandardError = $true
$pinfo.RedirectStandardOutput = $true
$pinfo.UseShellExecute = $false
$p = New-Object System.Diagnostics.Process
$p.StartInfo = $pinfo
$p.Start() | Out-Null
Start-Sleep 5
if (-not $p.HasExited) {
    Write-Host "Process running, ID: $($p.Id)"
    Write-Host "MainWindowHandle: $($p.MainWindowHandle)"
} else {
    Write-Host "Process exited with code: $($p.ExitCode)"
}
$stdout = $p.StandardOutput.ReadToEnd()
$stderr = $p.StandardError.ReadToEnd()
if ($stdout) { Write-Host "StdOut: $stdout" }
if ($stderr) { Write-Host "StdErr: $stderr" }
