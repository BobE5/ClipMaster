Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap(32, 32)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::FromArgb(59, 130, 246))
$g.Dispose()
$bmp.Save("$PSScriptRoot\Resources\clipboard.ico", [System.Drawing.Imaging.ImageFormat]::Icon)
$bmp.Dispose()
Write-Output "Icon created"
