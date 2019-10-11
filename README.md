# <img src="https://github.com/rodriada000/SessionMapSwitcher/blob/master/docs/images/SessionMapsIcon.png?raw=true" width="10%"> Session Map Switcher

This is a Desktop Application to make switching between Session maps in-game easier without having to restart the game.
![](https://github.com/rodriada000/SessionMapSwitcher/blob/master/docs/images/app_screenshot.png?raw=true "App Screenshot")

### Features
* Map Switcher can download the required patch (thanks to dga711) for Session to play custom modded maps and have custom textures within minutes.
* Easily load custom maps and switch between maps in-game without having to restart the game everytime you want to play a new map.
* Import custom maps from the online community using the `Import Map > From Online ...` feature.
* Easily replace textures with custom ones. Map Switcher will automatically find the correct texture files to replace for you.
* Easily import your own custom maps from your Computer and be able to easily re-import the map after making changes to it. Just right click the map you're working on and click `Re-import Selected Map ...`.
* Unreal Engine 4 Project Watcher for map creators that automatically re-imports the map you are working on after cooking the content.


## Notes Before Using
* **NOTE:** You should start the game using Session Map Switcher's `Start Session` button. If Session is already running then close the game and start it from Session Map Switcher instead.
* **NOTE:** You will have to load the custom map from map switcher when you enter the apartment in-game otherwise the original default map will load after you leave the apartment to go skate.

## How To Use

### Patching The Game
_Skip this if you already patched the game._
1. Download the [latest release here](https://github.com/rodriada000/SessionMapSwitcher/releases/latest).
2. Unzip the program anywhere you like.
3. Open SessionMapSwitcher.exe.
4. Set the `Path To Session` by clicking the `...` button or pasting the path and pressing `Enter` key. **The path should be the top level folder of the game directory e.g. `C:\Program Files (x86)\Steam\steamapps\common\Session`.** ![](https://github.com/rodriada000/SessionMapSwitcher/blob/master/docs/images/app_after_path_select.png?raw=true "Set Session Path")
5. Click `Patch With EzPz` and you will be prompted to download the required files to patch the game. ![](https://github.com/rodriada000/SessionMapSwitcher/blob/master/docs/images/app_pack_detected.png?raw=true "Game Not Unpacked Detected Screen")
> Note: If the program is not running as Administrator then it will ask you to restart the program as administrator first to ensure the unpacking process completes succesfully.

> Note: The download can fail sometimes because the file host is not good. Retry a couple of times if the download fails until it succeeds.  
6. Map Switcher will extract a few files from the game and then open Session EzPz Mod. When this window opens click `Patch` and close the window. 
7. Map Switcher will output the status of the patching process so you know when it is complete. After that you can play custom maps and replace textures.


### Adding Custom Maps to Play
Map Switcher provides the ability to download custom maps and import them from online or to import them directly from your computer (either a folder or .zip file).

#### From Online
_This is the recommended way to import maps into Session._
1. Inside Map Switcher click `Import Map` and in the menu that opens click `From Online ...`.
2. The Online Repository window will open where you can choose what maps to download ![](https://github.com/rodriada000/SessionMapSwitcher/blob/master/docs/images/app_import_online.png?raw=true "Import Online Window")
3. Select a map and click `Import Selected Map` to start downloading it. You will see download progress and will see the message "Map Imported!" when it is complete.
> NOTE: some maps in the list are not available for direct download due to file size or the map creators request. You will have to download these maps from the page that opens in your browser.

#### From Computer
_Use this option when downloading custom maps from your web browser or for map creators that are working on maps._
1. Inside Map Switcher click `Import Map` and in the menu that opens click `From Computer ...` and this window will appear. ![](https://github.com/rodriada000/SessionMapSwitcher/blob/master/docs/images/app_import_computer.png?raw=true "Import Online Window")
2. If you are importing a map from a .zip file then check the `Import .zip File` checkbox.
3. Browse for the file or folder by clicking `...` or pasting the path directly.
4. click `Import Map` and the map will be imported in a few seconds depending on the amount of files to copy.
> NOTE: If you import a map from a folder then you can right-click on the map in the Available Maps list and click `Re-import Selected Map ...` to copy the latest files to Session. This is useful for map creators wanting to test their changes.

#### Manually
_This is the final way to add custom maps and is not recommended for beginners. Follow these steps to add custom maps to Session manually without using Map Switcher._
1. Download a map of your choosing ([like this one for example, thanks Tifo](https://www.youtube.com/watch?v=pIbT3NDE5H0&feature=youtu.be))
2. Copy the map files into `[YourPathToSession]\SessionGame\Content` e.g. `C:\Program Files (x86)\Steam\steamapps\common\Session\SessionGame\Content`
3. If Map Switcher is already running then click the `Reload Available Maps` button and the new map should now be available in the list.


### Loading a Custom Map To Play
1. Select a modded map from the Available Maps list (_you can also double click the map in the list to load it._).
2. Before starting the game, change any game settings like gravity _(default value is -980)_ or number of objects you can place down.  
3. Click `Start Session` (_this will load the selected map and save your game settings before starting the game_).
> NOTE: When you go to the apartment in-game you will have to open Map Switcher and load the custom map again (even if you are not switching to a new map). This is to ensure the custom map is loaded instead of the original Brooklyn Banks map when you leave the apartment. 


### Switching Maps In-game
_This assumes you have already Session Map Switcher open and Session running. To load a new map follow the below steps._
1. Pause Session and return to your apartment.
2. `Alt + Tab` to switch back to Session Map Switcher.
3. Select a new map from the list of available maps (_or double click it to load._).
4. Click the `Load Map` button.
5. After the map has loaded `Alt + Tab` to switch back to Session. Then 'Go Skate'. The new map you selected should now load.


### Switch Back To Original Default Session Map
_This assumes you have already Session Map Switcher open and Session running. To restore the original NYC Brooklyn Banks level for Session follow these steps._
1. Pause Session and return to your apartment.
2. `Alt + Tab` to switch back to Session Map Switcher.
3. Select the first map in the list of Available Maps, 'Session Default Map - Brooklyn Banks'.
4. Click the `Load Map` button.
5. After the map has loaded `Alt + Tab` to switch back to Session. Then 'Go Skate'. The original map should now load.



### Replacing Textures
_This assumes you already unpacked the game and have Session Map Switcher open._
1. Download the texture files you want to use. There is usually 2-3 files for a texture: the `.uexp`, `.uasset`, and sometimes the `.ubulk` file. All three files are needed to replace the texture.
> Note: The Texture Replacer also supports .zip files
2. In Map Switcher click the `...` button inside the `Texture Replacer` section. In the file browse window that opens select the `.uasset` or `.zip` file you downloaded. ![](https://github.com/rodriada000/SessionMapSwitcher/blob/master/docs/images/app_replace_texture.png?raw=true "Replace Textures Example")
> NOTE: You only need to select the one `.uasset` file. Map Switcher will know to copy the other required files.
3. Click `Replace` and the texture will be replaced.
> NOTE: Map Switcher relies on the texture files to be cooked with the correct folder path to find the correct texture to replace. If the texture is not cooked with the correct path then the texture may be copied to the incorrect location.
