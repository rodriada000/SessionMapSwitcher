# <img src="https://github.com/rodriada000/SessionMapSwitcher/blob/master/docs/images/SessionMapsIcon.png?raw=true" width="10%"> Session Mod Manager [![GitHub release](https://img.shields.io/github/release/rodriada000/SessionMapSwitcher)](https://GitHub.com/rodriada000/SessionMapSwitcher/releases/)
>... Previously known as Session Map Switcher

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![GitHub contributors](https://img.shields.io/github/contributors/rodriada000/SessionMapSwitcher)](https://GitHub.com/rodriada000/SessionMapSwitcher/graphs/contributors/)
[![GitHub issues](https://img.shields.io/github/issues/rodriada000/SessionMapSwitcher)](https://GitHub.com/rodriada000/SessionMapSwitcher/issues/)

This is a Desktop application to easily mod Session and install custom maps and textures all in one tool. Easily switch between maps in-game without having to restart and download custom textures directly in the app.
<img src="https://github.com/rodriada000/SessionMapSwitcher/blob/master/docs/images/app_screenshot.png?raw=true" width="75%"> 
<img src="https://github.com/rodriada000/SessionMapSwitcher/blob/master/docs/images/app_import_online.png?raw=true" width="75%"> 

### **If you are having any issues please see [this FAQ Wiki provided here thanks to dga711 & others](https://github.com/rodriada000/SessionMapSwitcher/wiki/Issues-FAQ) or [view this tutorial video by Redgouf.](https://www.youtube.com/watch?v=EjqcErdS0jg)**

### Features
* Mod Manager can download the required patch (thanks to dga711/GHFear) for Session to play custom modded maps and have custom textures within minutes. (`Patch With Illusory Mod Unlocker` button)
* Easily load custom maps and switch between maps in-game without having to restart the game everytime you want to play a new map.
* Use the Asset Store easily download dozens of maps and custom textues like decks, wheels, shirts, decks, etc. Mod Manager installs the downloaded assets in minutes for you, No more fiddling with copying & deleting files from folders.
* Easily replace textures with custom ones using the Texture Replacer. Mod Manager will automatically find the correct texture files to replace for you.
* Easily import your own custom maps from your Computer and be able to easily re-import the map after making changes to it. Just right click the map you're working on and click `Re-import Selected Map ...`.
* Unreal Engine 4 Project Watcher for map creators that automatically re-imports the map you are working on after cooking the content.


## How To Use

### Patching The Game
_Skip this if you already patched/unpacked the game._
1. Download the [latest release here](https://github.com/rodriada000/SessionMapSwitcher/releases/latest).
2. Unzip the program anywhere you like.
3. Open SessionModManager.exe.
4. Set the `Path To Session` by clicking the `...` button or pasting the path and pressing `Enter` key. **The path should be the top level folder of the game directory e.g. `C:\Program Files (x86)\Steam\steamapps\common\Session`.**

> Note: The application will try to get the path to Session automatically if it has not been set.
5. Click `Patch With Illusory Mod Unlocker` and you will be prompted to download the required file to patch the game.
> Note: If you already have the mod unlocker program installed then SMM will open it instead.

6. Mod Manager will open a web browser so you can download the Illusory Universal Mod Unlocker ([or you can download it from here](https://illusory.dev/#))
7. After it finishes downloading, run the setup.exe and then the mod unlocker program will open.
8. Set the path to be `...\Session\SessionGame\Binaries\Win64` and then click `patch`
9. The game should now be patched and you can close the mod unlocker program.
> Note: This path is slightly different from the one set in SMM. 

### Modify Object Placement Count
_This feature allows you to change how many objects you can place in-game_

In the settings tab you can modify game settings and the object count (how many objects you place down in the object dropper).  You could set it anywhere from 1 to 65,000.


### Adding Custom Maps to Play
Mod Manager provides the ability to download custom maps and import them from online or to import them directly from your computer (either a folder or .zip file).

#### From Online
_This is the recommended way to import maps into Session._
1. Inside Mod Manager click `Asset Store` or  `Import Map > From Online ...`.
2. The Asset Store will open where you can choose what maps to download ![](https://github.com/rodriada000/SessionMapSwitcher/blob/master/docs/images/app_import_online.png?raw=true "Asset Store Window")
3. Select a map and click `Install Map` to start downloading it. There will be download progress in the bottom left corner.
> NOTE: You can queue asset downloads but it will only download/install one at a time.

#### From Computer
_Use this option when downloading custom maps from your web browser or for map creators that are working on maps._
1. Inside Mod Manager click `Import Map` and this window will appear. ![](https://github.com/rodriada000/SessionMapSwitcher/blob/master/docs/images/app_import_computer.png?raw=true "Import Online Window")
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
2. Before starting the game, change any game settings like number of objects you can place down.  
3. _(Optional)_ Check the `Load Second Map After Start` option so your selected map will be loaded if you go back to the apartment
4. Click `Start Session` (_this will load the selected map and save your game settings before starting the game_).
> **NOTE: When you go to the apartment in-game you will have to open Mod Manager and load the custom map again (even if you are not switching to a new map). This is to ensure the custom map is loaded instead of the original Brooklyn Banks map when you leave the apartment.** 


### Switching Maps In-game
_This assumes you have already Session Mod Manager open and Session running. To load a new map follow the below steps._
1. Pause Session.
2. `Alt + Tab` to switch back to Session Mod Manager.
3. Select a new map from the list of available maps (_or double click it to load._).
4. Click the `Load Map` button.
5. After the map has loaded `Alt + Tab` to switch back to Session. Then select a new map and choose Chatham Towers(The modded map will replace Chatham towers in the map selection screen). The new map you selected should now load.


### Switch Back To Original Default Session Map
_This assumes you have already Session Mod Manager open and Session running. To restore the original NYC Brooklyn Banks level for Session follow these steps._
1. Pause Session.
2. `Alt + Tab` to switch back to Session Mod Manager.
3. Select the first map in the list of Available Maps, 'Session Default Map - Brooklyn Banks'.
4. Click the `Load Map` button.
5. After the map has loaded `Alt + Tab` to switch back to Session. Then select the Chatham Towers map. The original map should now load.



### Replacing Textures
_This assumes you already patched/unpacked the game and have Session Mod Manager open. You can replace textures with the Texture Replacer or from the Asset Store._

1. Download the texture files you want to use. There is usually 2-3 files for a texture: the `.uexp`, `.uasset`, and sometimes the `.ubulk` file. All three files are needed to replace the texture.
> Note: The Texture Replacer also supports .zip and .rar files
2. In Mod Manager click the button with the folder icon on the `Texture Replacer` tab. In the file browse window that opens select the `.uasset` or `.zip` file you downloaded.
> NOTE: You only need to select the one `.uasset` file. Mod Manager will know to copy the other required files.

> NOTE: You can also drag & drop .zip, .rar, or .uasset files into the Texture Replacer

3. Click `Replace` and the texture will be replaced.
<img src="https://github.com/rodriada000/SessionMapSwitcher/blob/master/docs/images/texture_replacer_demo.gif?raw=true" width="90%">


> NOTE: Mod Manager relies on the selected texture file to be cooked with the correct texture name or for the file to be named the same as the texture being replaced so Mod Manager can find the correct path for the texture.



### Using the Asset Store
_The asset store is curated by the community. You can create a catalog that lists all your available mods to download and can share that catalog with others._
1. Click `Asset Store` tab at the top to switch to the Asset Store window.
2. Click the checkboxes to select which categories you want to view e.g. Decks, Wheels, etc.
3. Select an asset in the list to see the description and preview image

> Note: Click `Force Refresh Assets` to get the latest list of assets.

4. Click `Install Asset` in bottom right corner to download/install the new texture _(depending on the asset selected the install button text will change e.g. `Install Deck`)_.
<img src="https://github.com/rodriada000/SessionMapSwitcher/blob/master/docs/images/asset_store_demo.gif?raw=true" width="90%">

#### Adding a Catalog
_Use the `Manage Catalogs` button on the asset store tab to add and remove catalogs from the community._
1. Click `Asset Store` tab on the left to switch to the Asset Store window.
2. Click `Manage Catalogs` button to open the manage catalog window.
3. Click `Add Catalog` and then paste the catalog url you found into the textbox that appears. Then click `Confirm`
> NOTE: Here is an example of a catalog url: https://raw.githubusercontent.com/rodriada000/SessionCustomMapReleases/master/DefaultSMMCatalog.json 


### Tutorial Video On How to Use All Features And Troubleshoot Session Mod Manager
_shows older version, but still applies to newer versions_ 
[![Video Preview](https://img.youtube.com/vi/EjqcErdS0jg/0.jpg)](https://www.youtube.com/watch?v=EjqcErdS0jg)

## Credits
* Thanks to [Illusory Modding](https://discord.gg/3JAe2K) for the [Universal Mod Unlocker](https://illusory.dev/)
* Thanks to @dga711 for the [EzPz Mod](https://github.com/dga711/SessionEzPzMod)
* Thanks to Redgouf for creating an in-depth tutorial video on the tool
* @Gabisonfire for contributing to the [app version updater](https://github.com/Gabisonfire/avantgarde-lib)