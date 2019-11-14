# <img src="https://github.com/rodriada000/SessionMapSwitcher/blob/master/docs/images/SessionMapsIcon.png?raw=true" width="10%"> Session Mod Manager
>... Previously known as Session Map Switcher

[![Contributors][contributors-shield]][contributors-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]

This is a Desktop application to easily mod Session and install custom maps and textures all in one tool. Easily switch between maps in-game without having to restart and download custom textures directly in the app.
![](https://github.com/rodriada000/SessionMapSwitcher/blob/master/docs/images/app_screenshot.png?raw=true "App Screenshot")

![](https://github.com/rodriada000/SessionMapSwitcher/blob/master/docs/images/app_import_online.png?raw=true "Asset Store Screenshot")

### **If you are having any issues please see [this FAQ Wiki provided here thanks to dga711 & others.](https://github.com/rodriada000/SessionMapSwitcher/wiki/Issues-FAQ)**

### Features
* Mod Manager can download the required patch (thanks to dga711) for Session to play custom modded maps and have custom textures within minutes.
* Easily load custom maps and switch between maps in-game without having to restart the game everytime you want to play a new map.
* Use the Asset Store easily download dozens of maps and custom textues like decks, wheels, shirts, decks, etc. Mod Manager installs the downloaded assets in minutes for you, No more fiddling with copying & deleting files from folders.
<img src="https://github.com/rodriada000/SessionMapSwitcher/blob/master/docs/images/asset_store_demo.gif?raw=true" width="90%">
* Easily replace textures with custom ones using the Texture Replacer. Mod Manager will automatically find the correct texture files to replace for you.
* Easily import your own custom maps from your Computer and be able to easily re-import the map after making changes to it. Just right click the map you're working on and click `Re-import Selected Map ...`.
* Unreal Engine 4 Project Watcher for map creators that automatically re-imports the map you are working on after cooking the content.


## Notes Before Using
* **NOTE:** You should start the game using Session Mod Manager's `Start Session` button. If Session is already running then close the game and start it from Session Mod Manager instead.
* **NOTE:** You will have to load the custom map from Mod Manager when you enter the apartment in-game otherwise the original default map will load after you leave the apartment to go skate.

## How To Use

### Patching The Game
_Skip this if you already patched/unpacked the game._
1. Download the [latest release here](https://github.com/rodriada000/SessionMapSwitcher/releases/latest).
2. Unzip the program anywhere you like.
3. Open SessionModManager.exe.
4. Set the `Path To Session` by clicking the `...` button or pasting the path and pressing `Enter` key. **The path should be the top level folder of the game directory e.g. `C:\Program Files (x86)\Steam\steamapps\common\Session`.**

> Note: The application will try to get the path to Session automatically if it has not been set.
5. Click `Patch With EzPz` and you will be prompted to download the required files to patch the game.

> Note: If the program is not running as Administrator then it will ask you to restart the program as administrator first to ensure the patching process completes succesfully.

6. Mod Manager will download and then open Session EzPz Mod. When this window opens click `Patch` and close the window. 
7. Mod Manager will output the status of the patching process so you know when it is complete. After that you can play custom maps and replace textures.
8. (Optional) Click `Apply` under Game Settings in Mod Manager to download the required files for modifying the object count. If unreal engine is already installed on your computer then it will only have to download the `crypto.json` file to unpack a specific file from the game


### Adding Custom Maps to Play
Mod Manager provides the ability to download custom maps and import them from online or to import them directly from your computer (either a folder or .zip file).

#### From Online
_This is the recommended way to import maps into Session._
1. Inside Mod Manager click `Asset Store` or  `Import Map > From Online ...`.
2. The Asset Store will open where you can choose what maps to download ![](https://github.com/rodriada000/SessionMapSwitcher/blob/master/docs/images/app_import_online.png?raw=true "Asset Store Window")
3. Select a map and click `Install Map` to start downloading it. There will be download progress in the bottom left corner.
> NOTE: You can browse other assets while a map is downloading/installing but you can only install one asset at a time.

#### From Computer
_Use this option when downloading custom maps from your web browser or for map creators that are working on maps._
1. Inside Mod Manager click `Import Map` and in the menu that opens click `From Computer ...` and this window will appear. ![](https://github.com/rodriada000/SessionMapSwitcher/blob/master/docs/images/app_import_computer.png?raw=true "Import Online Window")
2. If you are importing a map from a .zip file then check the `Import .zip File` checkbox.
3. Browse for the file or folder by clicking `...` or pasting the path directly.
4. click `Import Map` and the map will be imported in a few seconds depending on the amount of files to copy.
> NOTE: If you import a map from a folder then you can right-click on the map in the Available Maps list and click `Re-import Selected Map ...` to copy the latest files to Session. This is useful for map creators wanting to test their changes.

#### Manually
_This is the final way to add custom maps and is not recommended for beginners. Follow these steps to add custom maps to Session manually without using Mod Manager._
1. Download a map of your choosing.
2. Copy the map files into `[YourPathToSession]\SessionGame\Content` e.g. `C:\Program Files (x86)\Steam\steamapps\common\Session\SessionGame\Content`
3. If Mod Manager is already running then click the `Reload Available Maps` button and the new map should now be available in the list.


### Loading a Custom Map To Play
1. Select a modded map from the Available Maps list (_you can also double click the map in the list to load it._).
2. Before starting the game, change any game settings like gravity _(default value is -980)_ or number of objects you can place down.  
3. Click `Start Session` (_this will load the selected map and save your game settings before starting the game_).
> NOTE: When you go to the apartment in-game you will have to open Mod Manager and load the custom map again (even if you are not switching to a new map). This is to ensure the custom map is loaded instead of the original Brooklyn Banks map when you leave the apartment. 


### Switching Maps In-game
_This assumes you have already Session Mod Manager open and Session running. To load a new map follow the below steps._
1. Pause Session and return to your apartment.
2. `Alt + Tab` to switch back to Session Mod Manager.
3. Select a new map from the list of available maps (_or double click it to load._).
4. Click the `Load Map` button.
5. After the map has loaded `Alt + Tab` to switch back to Session. Then 'Go Skate'. The new map you selected should now load.


### Switch Back To Original Default Session Map
_This assumes you have already Session Mod Manager open and Session running. To restore the original NYC Brooklyn Banks level for Session follow these steps._
1. Pause Session and return to your apartment.
2. `Alt + Tab` to switch back to Session Mod Manager.
3. Select the first map in the list of Available Maps, 'Session Default Map - Brooklyn Banks'.
4. Click the `Load Map` button.
5. After the map has loaded `Alt + Tab` to switch back to Session. Then 'Go Skate'. The original map should now load.



### Replacing Textures
_This assumes you already patched/unpacked the game and have Session Mod Manager open. You can replace textures with the Texture Replacer or from the Asset Store._

1. Download the texture files you want to use. There is usually 2-3 files for a texture: the `.uexp`, `.uasset`, and sometimes the `.ubulk` file. All three files are needed to replace the texture.
> Note: The Texture Replacer also supports .zip and .rar files
2. In Mod Manager click the `...` button inside the `Texture Replacer` section. In the file browse window that opens select the `.uasset` or `.zip` file you downloaded.
> NOTE: You only need to select the one `.uasset` file. Mod Manager will know to copy the other required files.

> NOTE: You can also drag & drop .zip, .rar, or .uasset files into the Texture Replacer

3. Click `Replace` and the texture will be replaced.
<img src="https://github.com/rodriada000/SessionMapSwitcher/blob/master/docs/images/texture_replacer_demo.gif?raw=true" width="90%">

> NOTE: Mod Manager relies on the selected texture file to be cooked with the correct texture name or for the file to be named the same as the texture being replaced so Mod Manager can find the correct path for the texture.



### Using the Asset Store
1. Click `Asset Store` tab at the top to switch to the Asset Store window.
2. Click the checkboxes to select which categories you want to view e.g. Decks, Wheels, etc.
3. Select an asset in the list to see the description and preview image

> Note: Click `Force Refresh Assets` to get the latest list of assets.

4. Click `Install Asset` in bottom right corner to download/install the new texture _(depending on the asset selected the install button text will change e.g. `Install Deck`)_.
<img src="https://github.com/rodriada000/SessionMapSwitcher/blob/master/docs/images/asset_store_demo.gif?raw=true" width="90%">



## Credits
* Thanks to @dga711 for the [EzPz Mod](https://github.com/dga711/SessionEzPzMod)
* Thanks to @Gabisonfire for contributing to the [version updater](https://github.com/Gabisonfire/avantgarde-lib) and [asset store](https://github.com/Gabisonfire/SessionAssetStore) and also hosting the asset files.
* Thanks to [Session modding community](https://discord.gg/XBz5s7) & [Illusory Modding](https://discord.gg/3JAe2K)
