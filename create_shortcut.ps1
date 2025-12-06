$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("$env:USERPROFILE\Desktop\ClipMaster.lnk")
$Shortcut.TargetPath = "$env:USERPROFILE\ClipMaster\bin\Release\net8.0-windows\ClipMaster.exe"
$Shortcut.WorkingDirectory = "$env:USERPROFILE\ClipMaster\bin\Release\net8.0-windows"
$Shortcut.Description = "ClipMaster Clipboard Manager"
$Shortcut.Save()
Write-Host "Shortcut created on Desktop!"
