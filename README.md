# Session Map Switcher
This is a Desktop Application to make switching between Session maps in-game easier without having to restart the game.
![](https://github.com/rodriada000/SessionMapSwitcher/blob/master/docs/images/app_screenshot.png "App Screenshot")


## Notes Before Using
* **NOTE** You should start the game using Session Map Switcher's `Start Session` button. If Session is already running then close the game and start it from Session Map Switcher instead.
* This tool will make a backup of the original Session map so you can always switch back to playing the map that came with the game.

## How To Use

### Getting Started
1. Download the latest [release here](https://github.com/rodriada000/SessionMapSwitcher/releases/latest).
2. Unzip the program anywhere you like.
3. Open SessionMapSwitcher.exe
4. Set the 'Path To Session' by clicking the `...` button or pasting the path and pressing 'Enter' key. The path should be the top level folder of the game directory e.g. `C:\Program Files (x86)\Steam\steamapps\common\Session`.
5. If the path is valid then the available maps will be loaded from `[YourPathToSession]\SessionGame\Content`. Additionally the original Session map files will be backed up to a folder named `[YourPathToSession]\SessionGame\Content\Original_Session_Map`.
6. **NOTE: If you have not unpacked the game then when you click `Load Map` or `Start Session` it will prompt you to begin unpacking for you. This will take a few minutes but after the process completes you can play on modded maps.**
6. Select a modded map from the list (_you can also double click the map in the list to load it._).
7. Before sarting the game, change any game settings like gravity or number of objects you can place down.  
8. Click `Start Session` (_this will load the selected map and save your game settings before starting the game_).

### Switching Maps In-game
_This assumes you have already Session Map Switcher open and Session running. To load a new map follow the below steps._
1. Pause Session and return to your apartment.
2. `Alt + Tab` to switch back to Session Map Switcher.
3. Select a new map from the list of available maps (_or double click it to load._).
4. Click the `Load Map` button.
5. After the map has loaded `Alt + Tab` to switch back to Session. Then 'Go Skate'. The new map you selected should now load.

### Switch BackTo Original Default Session Map
_This assumes you followed the Getting Started steps. To restore the original NYC Brooklyn Banks level for Session follow these steps._
1. Pause Session and return to your apartment.
2. `Alt + Tab` to switch back to Session Map Switcher.
3. Select the option 'Session Default Map - Brooklyn Banks' from the list of available maps.
4. Click the `Load Map` button.
5. After the map has loaded `Alt + Tab` to switch back to Session. Then 'Go Skate'. The original map should now load.

### Adding Custom Maps to Play
_follow these steps to add custom maps to Session to be able to access them from the Map Switcher._
1. Download a map of your choosing ([like this one for example, thanks Tifo](https://www.youtube.com/watch?v=pIbT3NDE5H0&feature=youtu.be))
2. Copy the map files into `[YourPathToSession]\SessionGame\Content` e.g. `C:\Program Files (x86)\Steam\steamapps\common\Session\SessionGame\Content`
3. If Map Switcher is already running then click the `Reload Available Maps` button
