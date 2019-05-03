# Opus

This is an android app for playing music from local file or from youtube (while screen is off). You can as well edit metadata tag and manage playlist (local one or youtube one).
If you want to download the app, check on the Release tabs.

**This ReadMe is for developers only, if you want to see what the app can do, please take a look at this website: https://www.raccoon-sdg.fr/en**

Like every android app, every layouts are ine the Resources/layout folder.

The code of the app is devided in 4 differents folders:

 - The DataStructure folder which contains classes/objects that are used to represents basic objects of the app (like songs or playlists). This folder also contains ViewHolders (classes that are used to bind layouts with data)
 - The UI folder which contains itself 3 folders:
   - The Fragment folder, containing every tab of the app (the youtube search, the playlist tab...)
   - The Adaptet folder, containing every adapter of the app. An adapter is something that bind a list (of songs, of playlists...) to a listview so the user can scroll, click...
   - The View folder, containing other views that are a bit more compext than other tabs (like the player or the queue). It also contains custom views that need custom calculations for a random reason.
 - The API folder which contains statics methodes that are used everywhere in the app. Inside this folder, you have:
   - The LocalManager contains methods used to manage local songs. Inside this you have methodes like the shuffle all, a method to play a local song...
   - The YoutubeManager contains methods to manage youtube songs. It's the same as the LocalManager but for youtube songs.
   - The SongManager contains generics methods for a song (like play methods). It will check how to interpret the song and use the correct method (local or youtube one).
   - The PlaylistManager contains methods for playlists (both locals and youtubes one).
   - This folder also contains a subfolder named Services. This folder contains things that run in background without a UI implementation. These classes behave without the help of other classes. You have:
     - The MusicPlayer service, which is the most complicated but the most used. It's the one which manage the playback.
     - The Downloader, allowing you to download songs or playlists. This service is the one with the more UI callbacks.
     - The Sleeper which is the one managing the sleep timer.
  - The Other folder which contains classes that are not really usful in itself but are needed. Like images transformaters (things that remove black borders in image, make them in circles...). It also contains some callbacks (Chromecast one, some for the snackbars...).

If you find an issues or want to contribute to the project, open a request here. 
