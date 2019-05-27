# Opus

This is an android app for playing music from local file or from youtube (while screen is off). You can as well edit metadata tag and manage playlist (local one or youtube one).
If you want to download the app, check on the Release tabs.

**This ReadMe is for developers only, if you want to see what the app can do, please take a look at this website: https://www.raccoon-sdg.fr/en**

## How to find the part of the code that interest you

Like every android app, every layouts are in the [Resources/layout](https://github.com/AnonymusRaccoon/Opus/tree/master/Opus/Resources/layout) folder.

The first thing that the app will load is the [MainActivity](https://github.com/AnonymusRaccoon/Opus/blob/master/Opus/Code/MainActivity.cs) file. It will also display the [Main Layout](https://github.com/AnonymusRaccoon/Opus/blob/master/Opus/Resources/layout/Main.xml) that is used everywhere in the app. For code of a specifc section of the app, check the list bellow.

The code of the app is devided in 4 differents folders:

 - The [DataStructure](https://github.com/AnonymusRaccoon/Opus/tree/master/Opus/Code/DataStructure) folder which contains classes/objects that are used to represents basic objects of the app (like songs or playlists). This folder also contains ViewHolders (classes that are used to bind layouts with data)
 - The [UI](https://github.com/AnonymusRaccoon/Opus/tree/master/Opus/Code/UI) folder which contains itself 3 folders:
   - The [Fragment](https://github.com/AnonymusRaccoon/Opus/tree/master/Opus/Code/UI/Fragments) folder, containing every tab of the app (the youtube search, the playlist tab...)
   - The [Adapter](https://github.com/AnonymusRaccoon/Opus/tree/master/Opus/Code/UI/Adapter) folder, containing every adapter of the app. An adapter is something that bind a list (of songs, of playlists...) to a listview so the user can scroll, click...
   - The [Views](https://github.com/AnonymusRaccoon/Opus/tree/master/Opus/Code/UI/Views) folder, containing other views that are a bit more compext than other tabs (like the player or the queue). It also contains custom views that need custom calculations for a random reason.
 - The [API](https://github.com/AnonymusRaccoon/Opus/tree/master/Opus/Code/Api) folder which contains statics methodes that are used everywhere in the app. Inside this folder, you have:
   - The [LocalManager](https://github.com/AnonymusRaccoon/Opus/blob/master/Opus/Code/Api/LocalManager.cs) contains methods used to manage local songs. Inside this you have methodes like the shuffle all, a method to play a local song...
   - The [YoutubeManager](https://github.com/AnonymusRaccoon/Opus/blob/master/Opus/Code/Api/YoutubeManager.cs) contains methods to manage youtube songs. It's the same as the LocalManager but for youtube songs.
   - The [SongManager](https://github.com/AnonymusRaccoon/Opus/blob/master/Opus/Code/Api/SongManager.cs) contains generics methods for a song (like play methods). It will check how to interpret the song and use the correct method (local or youtube one).
   - The [PlaylistManager](https://github.com/AnonymusRaccoon/Opus/blob/master/Opus/Code/Api/PlaylistManager.cs) contains methods for playlists (both locals and youtubes one).
   - This folder also contains a subfolder named [Services](https://github.com/AnonymusRaccoon/Opus/tree/master/Opus/Code/Api/Services). This folder contains things that run in background without a UI implementation. These classes behave without the help of other classes. You have:
     - The [MusicPlayer](https://github.com/AnonymusRaccoon/Opus/blob/master/Opus/Code/Api/Services/MusicPlayer.cs) service, which is the most complicated but the most used. It's the one which manage the playback.
     - The [Downloader](https://github.com/AnonymusRaccoon/Opus/blob/master/Opus/Code/Api/Services/Downloader.cs), allowing you to download songs or playlists. This service is the one with the more UI callbacks.
     - The [Sleeper](https://github.com/AnonymusRaccoon/Opus/blob/master/Opus/Code/Api/Services/Sleeper.cs) which is the one managing the sleep timer.
  - The [Other](https://github.com/AnonymusRaccoon/Opus/tree/master/Opus/Code/Others) folder which contains classes that are not really usful in itself but are needed. Like images transformaters (things that remove black borders in image, make them in circles...). It also contains some callbacks (Chromecast one, some for the snackbars...).
  
## How to build the app

### Installation of the environement
To build the app, you'll need the following elements:
  - [Visual Studio](https://visualstudio.microsoft.com/) (2017 or 2019)
  - The xamarin addon of visual studio
  - The android SDK of the target android version (visual studio will ask you to download the right version when you open the project)
    
With all of these components, you then need to download the packages that the app need. For that, simply right click in the solution tab of visual studio and click "Restore NuGet packages". With that, you'll be able to build the app. For precise details on how to build for debugging, emulators or release builds please refer to the xamarin doc.

### Youtube API Key
After that, you'll need an API key to use the youtube API.
#### Creating an API Key
To create an API KEY, go to https://console.developers.google.com and sign in with your google account. Then find and click on the "Create a Project" button. One your project has been created, go to the "Library" tab and select "Youtube Data API v3". On this tab, click on "Enable". You now have allowed your project to use the youtube api.
Then, you'll need to really create the key that will allow you to communicate with the google servers. On the same website, select the "Credentials" tab. Then click on "Create credentials" and select API key. You'll have a popup with your new API key. Copy this, you'll need it in the next step. *Please note that you can restrict your API key to one app, one website or one API. Since I'm using xamarin, the API key can't be restricted to the app only but you can allow this api key to work for youtube only.*
 
#### Using the API Key inside Opus
To use your newly created API key, create an xml file in the Resources/values folder. You can name it how you want, that doesn't change a thing. Inside this file, paste this:
```xml
<?xml version="1.0" encoding="utf-8" ?>
<resources>
  <string name="yt_api_key">REPLACE-THIS-WITH-YOUR-API-KEY</string>
</resources>
```
Please remember that your api key shouldn't be public. I recommend you to add this file in your git-ignore.

### Allow app installation
You'll also need to change the package name and the package signin-key (or simply disable it). It's because android will only allow new versions of the app if the newer version is signed with the same key as the older version so the easiest way to have your custom version is to change the package name, like that android won't even know that the two apps (your version and mine) are related.
    

**If you find an issues or want to contribute to the project, open a request here.** 
