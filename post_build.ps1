param([string]$version="NO_VERSION") 

$destination  = ".\Releases\$version"
$pluginDestination = "C:\Users\user\AppData\Roaming\r2modmanPlus-local\RiskOfRain2\profiles\Modding\BepInEx\plugins\$version"

if(Test-Path $destination)
{
    Remove-Item $destination -Recurse
}
if(Test-Path $pluginDestination)
{
    Remove-Item $pluginDestination -Recurse
}

New-Item -Path $destination -ItemType Directory

Copy-Item -Path ".\MiniMapMod\obj\Release\netstandard2.1\MiniMapMod.dll" -Destination $destination
Copy-Item -Path ".\MiniMapLibrary\obj\Release\netstandard2.1\MiniMapLibrary.dll" -Destination $destination
Copy-Item -Path ".\README.md" -Destination $destination
Copy-Item -Path ".\icon.png" -Destination $destination
Copy-Item -Path ".\manifest.json" -Destination $destination

Copy-Item -Path $destination $pluginDestination -Recurse

Compress-Archive -Path "$destination\*.*" -DestinationPath "$destination\$version.zip"
