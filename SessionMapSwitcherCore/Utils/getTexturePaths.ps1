cls

## CHANGE PATH TO MATCH WHERE .pak file is
## Make sure to have UnrealPak.exe in same folder as .pak file
## outputs C# code to add textures to a dictionary where Key is texture name and Value is relative path to texture

cd "C:\Program Files (x86)\Steam\steamapps\common\Session\SessionGame\Content\Paks"

.\UnrealPak.exe -cryptokeys="Crypto.json" -List .\SessionGame-WindowsNoEditor.pak > FileList.txt


$allFiles = Get-Content .\FileList.txt

$allFiles | Select-String '"SessionGame\/Content\/Customization.*.uasset.*"' -AllMatches `
          | Foreach-Object {
     
                $rawPath = $_.Matches.Value.Trim('"')

                $index = $rawPath.LastIndexOf('/')

                $relativePath = $rawPath.Substring(0, $index).Replace("SessionGame/Content/", "")
                $name = $rawPath.Substring($index + 1)

                '_texturePaths.Add(new TexturePathInfo(){ TextureName = "' + $name.Replace(".uasset", "") + '", RelativePath = "' + $relativePath.Replace("/", "\\") + '" });'
            }