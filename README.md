# Session Map Switcher
This is a Desktop Application to make switching between Session maps in-game easier without having to restart the game.
![](https://github.com/rodriada000/SessionMapSwitcher/blob/master/docs/images/app_screenshot.png "App Screenshot")

## Notes Before Using
* Session should already be unpacked before using this program. You can learn how to unpack the game for modding by [watching this video](https://www.youtube.com/watch?v=UqmQeHYv8IQ).
* **NOTE** You should start the game using Session Map Switcher's `Start Session` button. If Session is already running then close the game and start it from Session Map Switcher instead.
* **NOTE:** Load a custom map in Session Map Switcher first before starting the game. There is a bug right now where you can not switch maps if you start the game with the default map (hoping to fix soon).
* Make sure you have a folder that contains a copy of all your modded map files. ![](https://github.com/rodriada000/SessionMapSwitcher/blob/master/docs/images/maps_folder_example.png "Example of Maps Folder"). The program will use this folder to search for maps to load.
* This tool will make a backup of the original game files for the Brooklyn Banks level so you can switch back to the original map. You will need ~1GB of free space to back the files up.

## How To Use

### Getting Started
1. Download the latest [release here](https://github.com/rodriada000/SessionMapSwitcher/releases).
2. Unzip the program anywhere you like.
3. Open SessionMapSwitcher.exe
4. Set the 'Path To Session' and path to your custom maps by clicking the `...` buttons. (These paths will be remembered when the program is re-opened.)
5. After setting 'Path to Maps', the original Session map files will be backed up (to a folder named `Original_Session_Map`). You can not load a map or start the game while the files are backing up.
6. Select a modded map from the list and click `Load Map`.
7. Click `Start Session` (if the game is not running).

### Switching Maps In-game
_This assumes you have already Session Map Switcher open and Session running. To load a new map follow the below steps._
1. Pause Session and return to your apartment.
2. `Alt + Tab` to switch back to Session Map Switcher.
3. Select a new map from the list of available maps.
4. Click the `Load Map` button.
5. After the map has loaded `Alt + Tab` to switch back to Session. Then 'Go Skate'. The new map you selected should now load.

### Switch BackTo Original Default Session Map
_This assumes you followed the Getting Started steps. To restore the original NYC Brooklyn Banks level for Session follow these steps._
1. Pause Session and return to your apartment.
2. `Alt + Tab` to switch back to Session Map Switcher.
3. Select the option 'Session Default Map (Brooklyn Banks)' from the list of available maps.
4. Click the `Load Map` button.
5. After the map has loaded `Alt + Tab` to switch back to Session. Then 'Go Skate'. The original map should now load.
