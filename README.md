# MusicApp

Download link: https://drive.google.com/open?id=1XOPwa6Z4_HLRgNh4UQSTVR7jErj39XTs

This is an android app for playing music from local file or from youtube (while screen is off). You can as well edit metadata tag and manage playlist (local one or youtube one).

## Todo List:
 - Display forked playlist, watchlater and liked video on youtube playlist list
 - Add a loading bar when downloading metadata on youtube
 - Crop album art on edit metadata
 - Make auto updater
 - Display the app menu under the small player if the user selected this option
 - Handle back arrow click everywhere on the app
 - Allow user to keep synced a youtube playlist with a local playlist
 - Allow user to rotate is screen while using the app
 - Create a widget for the desktop
 - Make animations
 - Add splash art on empty views
 
### Minor change:
 - Create link between local file and youtube (like add a search on youtube button if user doesn't find a local file)
 - Add time indication on player view and when user is using the player's seekbar
 - Instantly start reorder if user click on the reorder button of a queue's item
 - Make more snackbar (error messages)
 - Hide toolbar and keep only tabs if the user scroll down on browse or playlist view
 - Add an animation for the current played sond on the queue
 - Change player's action button color according to the album art (make them more visible)
 
 
## Known bugs:
 - Download isn't working (make the app crash)
 - If the app is in standby for too long, the app crash
 - Updating an album art need a reboot to take effect
 - Going back to browse while browse's search bar isn't empty undo the search
 - Loading bar of youtube video can be canceled if user click on another song before the loading bar disapear
 - Should allow refresh on slide only when the first view of the list is completely visible (actually allowed when first view is visible)
 - When switching the focus of folder or playlist content, weird transition append
 - Preference toolbar name is highlighted on the dark theme
