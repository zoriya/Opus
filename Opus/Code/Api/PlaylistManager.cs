using Android.App;
using Android.Content;
using Android.Database;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Support.Design.Widget;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Opus.Adapter;
using Opus.Api.Services;
using Opus.DataStructure;
using Opus.Others;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Android.Provider.MediaStore.Audio;
using CursorLoader = Android.Support.V4.Content.CursorLoader;
using Environment = System.Environment;
using Path = System.IO.Path;
using Playlist = Google.Apis.YouTube.v3.Data.Playlist;
using PlaylistItem = Opus.DataStructure.PlaylistItem;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using Uri = Android.Net.Uri;

namespace Opus.Api
{
    public class PlaylistManager
    {
        /// <summary>
        /// Return a complete PlaylistItem object from the youtube id of the playlist
        /// </summary>
        /// <param name="playlistID"></param>
        /// <returns></returns>
        public static async Task<PlaylistItem> GetPlaylist(string playlistID)
        {
            PlaylistsResource.ListRequest request = YoutubeManager.YoutubeService.Playlists.List("snippet");
            request.Id = playlistID;

            PlaylistListResponse response = await request.ExecuteAsync();

            if (response.Items.Count > 0)
            {
                Playlist result = response.Items[0];
                return new PlaylistItem(result.Snippet.Title, -1, playlistID)
                {
                    HasWritePermission = false,
                    ImageURL = result.Snippet.Thumbnails.High.Url,
                    Owner = result.Snippet.ChannelTitle
                };
            }
            else
                return null;
        }

        #region PlayInOrder
        /// <summary>
        /// Play all tracks of a playlist in the default order. Handle both youtube and local playlists.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="startingPosition">The position where you want to start playing</param>
        public static void PlayInOrder(PlaylistItem item, int startingPosition = 0)
        {
            if (item.LocalID != -1 && item.LocalID != 0 || item.SyncState == SyncState.True)
                PlayInOrder(item.LocalID, startingPosition);
            else
                PlayInOrder(item.YoutubeID, startingPosition);
        }

        /// <summary>
        /// Play a local playlist in the default order.
        /// </summary>
        /// <param name="LocalID"></param>
        /// <param name="startingPosition">The position where you want to start playing</param>
        public static async void PlayInOrder(long LocalID, int startingPosition = 0)
        {
            List<Song> tracks = await GetTracksFromLocalPlaylist(LocalID);

            if (tracks.Count == 0)
                return;

            SongManager.Play(tracks[startingPosition]);
            tracks.RemoveAt(startingPosition);

            await Task.Delay(1000);

            while (MusicPlayer.instance == null)
                await Task.Delay(10);

            MusicPlayer.instance.InsertToQueue(0, tracks.GetRange(0, startingPosition).ToArray());
            MusicPlayer.currentID = startingPosition;
            Queue.instance?.RefreshCurrent();
            Player.instance?.RefreshPlayer();
            MusicPlayer.instance.AddToQueue(tracks.GetRange(startingPosition, tracks.Count - startingPosition));
        }

        /// <summary>
        /// Play a youtube playlist in the default order.
        /// </summary>
        /// <param name="YoutubeID"></param>
        /// <param name="startingPosition">The position where you want to start playing</param>
        public static async void PlayInOrder(string YoutubeID, int startingPosition = 0)
        {
            List<Song> tracks = await GetTracksFromYoutubePlaylist(YoutubeID, (song) => 
            {
                SongManager.Play(song);
            }, startingPosition);

            if (tracks.Count == 0)
                return;

            tracks.RemoveAt(startingPosition);

            await Task.Delay(1000);
            while (MusicPlayer.instance == null)
                await Task.Delay(10);

            MusicPlayer.instance.InsertToQueue(0, tracks.GetRange(0, startingPosition).ToArray());
            MusicPlayer.currentID = startingPosition;
            Queue.instance?.RefreshCurrent();
            Player.instance?.RefreshPlayer();
            MusicPlayer.instance.AddToQueue(tracks.GetRange(startingPosition, tracks.Count - startingPosition));
        }
        #endregion

        #region Shuffle
        /// <summary>
        /// Play all tracks of a playlist in a random order. Handle both youtube and local playlists.
        /// </summary>
        /// <param name="item"></param>
        public static void Shuffle(PlaylistItem item)
        {
            if (item.LocalID != -1 && item.LocalID != 0 || item.SyncState == SyncState.True)
                Shuffle(item.LocalID);
            else
                Shuffle(item.YoutubeID);
        }

        /// <summary>
        /// Play all tracks of a local playlist in a random order.
        /// </summary>
        /// <param name="LocalID"></param>
        public async static void Shuffle(long LocalID)
        {
            List<Song> tracks = await GetTracksFromLocalPlaylist(LocalID);
            if (tracks.Count == 0)
                return;

            Random r = new Random();
            tracks = tracks.OrderBy(x => r.Next()).ToList();

            SongManager.Play(tracks[0]);
            tracks.RemoveAt(0);

            await Task.Delay(1000);
            while (MusicPlayer.instance == null)
                await Task.Delay(10);

            MusicPlayer.instance.AddToQueue(tracks);
        }

        /// <summary>
        /// Play all tracks of a youtube playlist in a random order.
        /// </summary>
        /// <param name="YoutubeID"></param>
        public static async void Shuffle(string YoutubeID)
        {
            Random r = new Random();
            int playPos = r.Next(50);
            List<Song> tracks = await GetTracksFromYoutubePlaylist(YoutubeID, (song) => 
            {
                MusicPlayer.currentID = -1;
                SongManager.Play(song);
            }, playPos);

            if (tracks.Count == 0)
                return;

            tracks.RemoveAt(playPos);
            tracks = tracks.OrderBy(x => r.Next()).ToList();
            MusicPlayer.instance.AddToQueue(tracks);
        }
        #endregion

        #region AddToQueue
        /// <summary>
        /// Add every song of a playlist in the queue. (Using default order). Handle both local and youtube playlists.
        /// </summary>
        /// <param name="item"></param>
        public static void AddToQueue(PlaylistItem item)
        {
            if (item.LocalID != -1 && item.LocalID != 0 || item.SyncState == SyncState.True)
                AddToQueue(item.LocalID);
            else
                AddToQueue(item.YoutubeID);
        }

        /// <summary>
        /// Add every song of a local playlist in the queue. (Using default order).
        /// </summary>
        /// <param name="LocalID"></param>
        public async static void AddToQueue(long LocalID)
        {
            if (MusicPlayer.instance == null || MusicPlayer.queue == null || MusicPlayer.queue?.Count == 0)
            {
                PlayInOrder(LocalID);
                return;
            }

            List<Song> tracks = await GetTracksFromLocalPlaylist(LocalID);
            MusicPlayer.instance.AddToQueue(tracks);
        }

        /// <summary>
        /// Add every song of a youtube playlist in the queue. (Using default order).
        /// </summary>
        /// <param name="YoutubeID"></param>
        public static async void AddToQueue(string YoutubeID)
        {
            if (MusicPlayer.instance == null || MusicPlayer.queue == null || MusicPlayer.queue?.Count == 0)
            {
                PlayInOrder(YoutubeID);
                return;
            }

            MusicPlayer.instance.AddToQueue((await GetTracksFromYoutubePlaylist(YoutubeID)));
        }
        #endregion

        #region UI
        /// <summary>
        /// Display the create playlist dialog (where the user can choose a location and a name). After completing the creation, the songs array will be added to this playlist.
        /// </summary>
        /// <param name="songs"></param>
        public static void CreatePlalistDialog(Song[] songs)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(MainActivity.instance, MainActivity.dialogTheme);
            builder.SetTitle(Resource.String.new_playlist);
            View view = MainActivity.instance.LayoutInflater.Inflate(Resource.Layout.CreatePlaylistDialog, null);
            builder.SetView(view);
            PlaylistLocationAdapter adapter = new PlaylistLocationAdapter(MainActivity.instance, Android.Resource.Layout.SimpleSpinnerItem, new string[] { MainActivity.instance.GetString(Resource.String.create_local), MainActivity.instance.GetString(Resource.String.create_youtube), MainActivity.instance.GetString(Resource.String.create_synced) })
            {
                YoutubeWorkflow = songs.Length == 1 && songs[0].YoutubeID == null ? false : true
            };
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            view.FindViewById<Spinner>(Resource.Id.playlistLocation).Adapter = adapter;
            builder.SetNegativeButton(Resource.String.cancel, (senderAlert, args) => { });
            builder.SetPositiveButton(Resource.String.ok, (senderAlert, args) =>
            {
                switch (view.FindViewById<Spinner>(Resource.Id.playlistLocation).SelectedItemPosition)
                {
                    case 0:
                        CreateLocalPlaylist(view.FindViewById<EditText>(Resource.Id.playlistName).Text, songs);
                        break;
                    case 1:
                        CreateYoutubePlaylist(view.FindViewById<EditText>(Resource.Id.playlistName).Text, songs);
                        break;
                    case 2:
                        CreateLocalPlaylist(view.FindViewById<EditText>(Resource.Id.playlistName).Text, songs, true);
                        CreateYoutubePlaylist(view.FindViewById<EditText>(Resource.Id.playlistName).Text, songs);
                        break;
                }
            });
            builder.Show();
        }

        /// <summary>
        /// Display the dialog listing all the playlist where you can add a song. If the song is already contained in one of the playlist, it will be displayed.
        /// </summary>
        /// <param name="item"></param>
        public static async void AddSongToPlaylistDialog(Song item)
        {
            if (item.YoutubeID == null)
                item = LocalManager.CompleteItem(item);

            List<PlaylistItem> playlists = new List<PlaylistItem>();
            List<PlaylistItem> synced = new List<PlaylistItem>();

            (List<PlaylistItem> localPlaylists, string error) = await GetLocalPlaylists();
            if (localPlaylists != null)
            {
                foreach (PlaylistItem playlist in localPlaylists)
                    playlist.SongContained = await LocalManager.SongIsContained(item.Id, playlist.LocalID);

                (playlists, synced) = await ProcessSyncedPlaylists(localPlaylists);
                playlists.AddRange(synced);
            }

            PlaylistItem Loading = new PlaylistItem("Loading", null);
            playlists.Add(Loading);

            View Layout = MainActivity.instance.LayoutInflater.Inflate(Resource.Layout.AddToPlaylistLayout, null);
            if (MainActivity.Theme == 1)
            {
                Layout.FindViewById<ImageView>(Resource.Id.leftIcon).SetColorFilter(Color.White);
                Layout.FindViewById<View>(Resource.Id.divider).SetBackgroundColor(Color.White);
            }
            AlertDialog.Builder builder = new AlertDialog.Builder(MainActivity.instance, MainActivity.dialogTheme);
            builder.SetTitle(Resource.String.save_playlist);
            builder.SetView(Layout);
            RecyclerView ListView = Layout.FindViewById<RecyclerView>(Resource.Id.recycler);
            ListView.SetLayoutManager(new LinearLayoutManager(MainActivity.instance));
            AddToPlaylistAdapter adapter = new AddToPlaylistAdapter(playlists);
            ListView.SetAdapter(adapter);
            adapter.ItemClick += async (sender, position) =>
            {
                AddToPlaylistHolder holder = (AddToPlaylistHolder)ListView.GetChildViewHolder(ListView.GetChildAt(position));
                bool add = !holder.Added.Checked;
                holder.Added.Checked = add;

                PlaylistItem playlist = playlists[position];
                if (add)
                {
                    if (playlist.LocalID != 0)
                    {
                        if (item.Id == 0 || item.Id == -1)
                            YoutubeManager.Download(new[] { item }, playlist.Name);
                        else
                            LocalManager.AddToPlaylist(new[] { item }, playlist.Name, playlist.LocalID);
                    }
                    if (playlist.YoutubeID != null)
                        YoutubeManager.AddToPlaylist(new[] { item }, playlist.YoutubeID);
                }
                else
                {
                    if (playlist.YoutubeID != null)
                        item = await CompleteItem(item, playlist.YoutubeID);


                    if (item.TrackID != null)
                        RemoveFromYoutubePlaylist(item.TrackID);

                    if (playlist.LocalID != 0)
                    {
                        ContentResolver resolver = MainActivity.instance.ContentResolver;
                        Uri plUri = Playlists.Members.GetContentUri("external", playlist.LocalID);
                        resolver.Delete(plUri, Playlists.Members.AudioId + "=?", new string[] { item.Id.ToString() });
                    }
                }
            };
            builder.SetPositiveButton(Resource.String.ok, (sender, e) => { });
            AlertDialog dialog = builder.Create();
            Layout.FindViewById<LinearLayout>(Resource.Id.CreatePlaylist).Click += (sender, e) => { dialog.Dismiss(); CreatePlalistDialog(new[] { item }); };
            dialog.Show();

            if (item.YoutubeID == null)
            {
                if (item.YoutubeID == null)
                {
                    Toast.MakeText(MainActivity.instance, Resource.String.playlist_add_song_not_found, ToastLength.Long).Show();
                    playlists.Remove(Loading);
                    adapter.NotifyItemRemoved(playlists.Count);
                    return;
                }
            }

            if (!await MainActivity.instance.WaitForYoutube())
            {
                Toast.MakeText(MainActivity.instance, Resource.String.youtube_loading_error, ToastLength.Long).Show();
                playlists.Remove(Loading);
                adapter.NotifyItemRemoved(playlists.Count);
                return;
            }

            (List<PlaylistItem> youtube, string er) = await GetOwnedYoutubePlaylists(synced, (playlist, position) => 
            {
                AddToPlaylistHolder holder = (AddToPlaylistHolder)ListView.GetChildViewHolder(ListView.GetChildAt(position));
                holder.SyncLoading.Visibility = ViewStates.Gone;
                holder.Status.Visibility = ViewStates.Visible;
                holder.Status.SetImageResource(Resource.Drawable.Sync);
            });

            if(youtube != null)
            {
                foreach (PlaylistItem playlist in youtube)
                    playlist.SongContained = await YoutubeManager.SongIsContained(item.YoutubeID, playlist.YoutubeID);

                int positionStart = playlists.IndexOf(Loading);
                playlists.Remove(Loading);
                playlists.AddRange(youtube);
                adapter.NotifyItemRangeInserted(positionStart, youtube.Count - 1);
                adapter.NotifyItemChanged(playlists.Count);
            }
            else
            {
                playlists.Remove(Loading);
                adapter.NotifyItemRemoved(playlists.Count);
            }
        }

        /// <summary>
        /// Display a dialog that will allow the user to rename the playlist.
        /// </summary>
        /// <param name="item">The playlist you want to rename</param>
        /// <param name="UiCallback">A callback called after the rename</param>
        public static void Rename(PlaylistItem item, Action UiCallback = null)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(MainActivity.instance, MainActivity.dialogTheme);
            builder.SetTitle(Resource.String.rename_playlist);
            View view = MainActivity.instance.LayoutInflater.Inflate(Resource.Layout.CreatePlaylistDialog, null);
            view.FindViewById(Resource.Id.playlistLocation).Visibility = ViewStates.Gone;
            builder.SetView(view);
            builder.SetNegativeButton(Resource.String.cancel, (senderAlert, args) => { });
            builder.SetPositiveButton(Resource.String.rename, async (senderAlert, args) =>
            {
                if (item.LocalID != 0 && !await MainActivity.instance.GetWritePermission())
                    return;

                string newName = view.FindViewById<EditText>(Resource.Id.playlistName).Text;

                if (item.YoutubeID != null)
                {
                    try
                    {
                        Playlist playlist = new Playlist
                        {
                            Snippet = new PlaylistSnippet()
                        };
                        playlist.Snippet.Title = newName;
                        playlist.Id = item.YoutubeID;

                        await YoutubeManager.YoutubeService.Playlists.Update(playlist, "snippet").ExecuteAsync();
                    }
                    catch (System.Net.Http.HttpRequestException)
                    {
                        MainActivity.instance.Timout();
                    }
                }
                if (item.LocalID != 0)
                {
                    ContentValues value = new ContentValues();
                    value.Put(Playlists.InterfaceConsts.Name, newName);
                    MainActivity.instance.ContentResolver.Update(Playlists.ExternalContentUri, value, Playlists.InterfaceConsts.Id + "=?", new string[] { item.LocalID.ToString() });
                }

                item.Name = newName;
                UiCallback?.Invoke();
            });
            builder.Show();
        }

        /// <summary>
        /// Display a dialog that allow the user to delete the playlist (from youtube, from the local storage or both).
        /// </summary>
        /// <param name="item"></param>
        /// <param name="UiCallback">Called when the user deleted the playlist</param>
        public static void Delete(PlaylistItem item, Action UiCallback)
        {
            AlertDialog dialog = new AlertDialog.Builder(MainActivity.instance, MainActivity.dialogTheme)
                .SetTitle(MainActivity.instance.GetString(Resource.String.delete_playlist, item.Name))
                .SetPositiveButton(Resource.String.yes, async (sender, e) =>
                {
                    if (item.LocalID != 0 && !await MainActivity.instance.GetWritePermission())
                        return;

                    if (item.SyncState != SyncState.False)
                        StopSyncing(item);

                    if (item.YoutubeID != null)
                    {
                        if (item.HasWritePermission)
                        {
                            try
                            {
                                PlaylistsResource.DeleteRequest deleteRequest = YoutubeManager.YoutubeService.Playlists.Delete(item.YoutubeID);
                                await deleteRequest.ExecuteAsync();
                            }
                            catch (System.Net.Http.HttpRequestException)
                            {
                                MainActivity.instance.Timout();
                            }
                        }
                        else
                        {
                            //try
                            //{
                            //    ChannelSectionsResource.ListRequest forkedRequest = YoutubeEngine.youtubeService.ChannelSections.List("snippet,contentDetails");
                            //    forkedRequest.Mine = true;
                            //    ChannelSectionListResponse forkedResponse = await forkedRequest.ExecuteAsync();

                            //    foreach (ChannelSection section in forkedResponse.Items)
                            //    {
                            //        if (section.Snippet.Title == "Saved Playlists")
                            //        {
                            //            section.ContentDetails.Playlists.Remove(YoutubeID);
                            //            ChannelSectionsResource.UpdateRequest request = YoutubeEngine.youtubeService.ChannelSections.Update(section, "snippet,contentDetails");
                            //            ChannelSection response = await request.ExecuteAsync();
                            //        }
                            //    }
                            //}
                            //catch (System.Net.Http.HttpRequestException)
                            //{
                            //    MainActivity.instance.Timout();
                            //}
                        }
                    }
                    if (item.LocalID != 0)
                    {
                        ContentResolver resolver = MainActivity.instance.ContentResolver;
                        Uri uri = Playlists.ExternalContentUri;
                        resolver.Delete(Playlists.ExternalContentUri, Playlists.InterfaceConsts.Id + "=?", new string[] { item.LocalID.ToString() });
                    }

                    UiCallback?.Invoke();
                })
                .SetNegativeButton(Resource.String.no, (sender, e) => { })
                .Create();
            dialog.Show();
        }

        /// <summary>
        /// Display the stop syncing dialog.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="UiCallback">Callback called when the playlist is not synced anymore.</param>
        public static void StopSyncingDialog(PlaylistItem item, Action UiCallback)
        {
            AlertDialog dialog = new AlertDialog.Builder(MainActivity.instance, MainActivity.dialogTheme)
                .SetTitle(Resource.String.stop_syncing)
                .SetPositiveButton(Resource.String.yes, (sender, e) => { StopSyncing(item); UiCallback?.Invoke(); })
                .SetNegativeButton(Resource.String.no, (sender, e) => { })
                .Create();
            dialog.Show();
        }

        /// <summary>
        /// Remove a playlist from the synced database.
        /// </summary>
        /// <param name="item"></param>
        public async static void StopSyncing(PlaylistItem item)
        {
            await Task.Run(() =>
            {
                SQLiteConnection db = new SQLiteConnection(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "SyncedPlaylists.sqlite"));
                db.CreateTable<PlaylistItem>();

                db.Delete(db.Table<PlaylistItem>().ToList().Find(x => x.LocalID == item.LocalID));
            });
        }

        /// <summary>
        /// Display the dialog to remove a track from the playlist. This can be undo by clicking on the "undo" button of the snackbar that will apear.
        /// </summary>
        /// <param name="item">The playlist item</param>
        /// <param name="song">The song item</param>
        /// <param name="RemovedCallback">A callback called when the user click ok in the dialog. You just need to remove the track from the UI, everything else is already handled.</param>
        /// <param name="CancelledCallback">A callback called when the user click cancel in the dialog</param>
        /// <param name="UndoCallback">A callback called when the user click undo in the snackbar</param>
        public static void RemoveTrackFromPlaylistDialog(PlaylistItem item, Song song, Action RemovedCallback, Action CancelledCallback, Action UndoCallback)
        {
            AlertDialog dialog = new AlertDialog.Builder(MainActivity.instance, MainActivity.dialogTheme)
                .SetTitle(MainActivity.instance.GetString(Resource.String.remove_from_playlist, song.Title))
                .SetPositiveButton(Resource.String.yes, async (sender, e) =>
                {
                    RemovedCallback?.Invoke();

                    if (item.YoutubeID != null)
                    {
                        if (song.TrackID == null)
                            song = await CompleteItem(song, item.YoutubeID);
                    }
                    RemoveTrackFromPlaylistCallback callback = new RemoveTrackFromPlaylistCallback(song, item.LocalID);

                    Snackbar snackBar = Snackbar.Make(MainActivity.instance.FindViewById(Resource.Id.snackBar), (song.Title.Length > 20 ? song.Title.Substring(0, 17) + "..." : song.Title) + MainActivity.instance.GetString(Resource.String.removed_from_playlist), Snackbar.LengthLong)
                        .SetAction(MainActivity.instance.GetString(Resource.String.undo), (v) =>
                        {
                            callback.canceled = true;
                            UndoCallback?.Invoke();
                        });
                    snackBar.AddCallback(callback);
                    snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
                    snackBar.Show();
                })
                .SetNegativeButton(Resource.String.no, (sender, e) => { CancelledCallback?.Invoke(); })
                .Create();
            dialog.Show();
        }
        #endregion

        #region GetPlaylists
        /// <summary>
        /// This method will return all playlists available on the local storage in the array "playlists".
        /// If there is an error, the Task will return an error message to display to the user.
        /// </summary>
        /// <param name="askForPermission">Should we ask for the read perission or simple return nothing if the user has not given the read permission.</param>
        /// <returns>The List<PlaylistItem> contains all the local playlists or is null if there is an erorr. The string is the error message. If there is no error, this string is null.</returns>
        public static async Task<(List<PlaylistItem>, string)> GetLocalPlaylists(bool askForPermission = true)
        {
            if (askForPermission)
            {
                if (!await MainActivity.instance.GetReadPermission())
                    return (null, Application.Context.GetString(Resource.String.localpl_noperm));
            }
            else
            {
                if (!MainActivity.instance.HasReadPermission())
                    return (null, Application.Context.GetString(Resource.String.localpl_noperm));
            }

            List<PlaylistItem> playlists = new List<PlaylistItem>();

            Uri uri = Playlists.ExternalContentUri;
            await Task.Run(() => 
            {
                if (Looper.MyLooper() == null)
                    Looper.Prepare();

                CursorLoader loader = new CursorLoader(Application.Context, uri, null, null, null, null);
                ICursor cursor = (ICursor)loader.LoadInBackground();

                if (cursor != null && cursor.MoveToFirst())
                {
                    int nameID = cursor.GetColumnIndex(Playlists.InterfaceConsts.Name);
                    int listID = cursor.GetColumnIndex(Playlists.InterfaceConsts.Id);
                    do
                    {
                        string name = cursor.GetString(nameID);
                        long id = cursor.GetLong(listID);

                        Uri musicUri = Playlists.Members.GetContentUri("external", id);
                        CursorLoader cursorLoader = new CursorLoader(Application.Context, musicUri, null, null, null, null);
                        ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();

                        playlists.Add(new PlaylistItem(name, id, musicCursor.Count));
                    }
                    while (cursor.MoveToNext());
                    cursor.Close();
                }
            });

            if (playlists.Count == 0)
                return (null, "EMPTY");
            else
                return (playlists, null);
        }

        /// <summary>
        /// This method will proceed the local playlists and split synced one from the local one. 
        /// The outputed youtube playlists will already have the right sync state set (loading, synced...)
        /// </summary>
        /// <param name="localPlaylists">The array of local playlists (can be obtaines with the GetLocalPlaylists method).</param>
        /// <returns>The first list contains all the local only playlists and the second one contains all the synced one. Every playlists has the right sync state set.</returns>
        public static async Task<(List<PlaylistItem>, List<PlaylistItem>)> ProcessSyncedPlaylists(List<PlaylistItem> localPlaylists)
        {
            List<PlaylistItem> syncedPlaylists = new List<PlaylistItem>();
            await Task.Run(() =>
            {
                SQLiteConnection db = new SQLiteConnection(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "SyncedPlaylists.sqlite"));
                db.CreateTable<PlaylistItem>();

                syncedPlaylists = db.Table<PlaylistItem>().ToList();
            });

            foreach (PlaylistItem synced in syncedPlaylists)
            {
                PlaylistItem local = localPlaylists?.Find(x => x.LocalID == synced.LocalID);
                if (local != null)
                {
                    synced.Count = local.Count;
                    localPlaylists.Remove(local); //This playlist is a synced one, we don't want to display it in the "local" collumn but in the youtube one.

                    //Set sync state of the playlist (SyncState can't be false since we take the playlist in the synced database)
                    if (synced.YoutubeID == null)
                        synced.SyncState = SyncState.Loading;
                    else
                        synced.SyncState = SyncState.True;
                }
                else
                {
                    //If local is null, we had an error loading local playlists or another thing (can be read permission denied for example) 
                    synced.SyncState = SyncState.Error;
                }
            }

            return (localPlaylists, syncedPlaylists);
        }

        /// <summary>
        /// This method return all youtube playlists owned by the user and process synced playlist if you give an array of know youtube synced playlists  
        /// The YoutubePlaylists array should contains the synced playlist availables. Warning, this will return your initial array(proceded if there is synced playlist) + owned 
        /// The second outputed var(the string) is the error message that should be displayed to the user(the list will be null if there is an error)
        /// </summary>
        /// <param name="SyncedPlaylists"></param>
        /// <param name="UiCallback">A callback that will tell the ui to update a synced playlist with the new data got from the item.</param>
        /// <returns></returns>
        public static async Task<(List<PlaylistItem>, string)> GetOwnedYoutubePlaylists(List<PlaylistItem> SyncedPlaylists, Action<PlaylistItem, int> UiCallback)
        {
            if (!await MainActivity.instance.WaitForYoutube() || YoutubeManager.IsUsingAPI)
                return (null, "Error"); //Should have a better error handling

            List<PlaylistItem> YoutubePlaylists = new List<PlaylistItem>();

            try
            {
                YouTubeService youtube = YoutubeManager.YoutubeService;

                PlaylistsResource.ListRequest request = youtube.Playlists.List("snippet,contentDetails");
                request.Mine = true;
                request.MaxResults = 25;
                PlaylistListResponse response = await request.ExecuteAsync();

                for (int i = 0; i < response.Items.Count; i++)
                {
                    Playlist playlist = response.Items[i];
                    PlaylistItem item = new PlaylistItem(playlist.Snippet.Title, playlist.Id, playlist, (int)playlist.ContentDetails.ItemCount)
                    {
                        Owner = playlist.Snippet.ChannelTitle,
                        ImageURL = playlist.Snippet.Thumbnails.High.Url,
                        HasWritePermission = true
                    };

                    ProcessPlaylistSyncState(item, SyncedPlaylists, YoutubePlaylists, UiCallback);
                }

                return (YoutubePlaylists, null);
            }
            catch (System.Net.Http.HttpRequestException)
            {
                return (null, "Error"); //Should handle precise error here
            }
        }

        /// <summary>
        /// This method return all youtube playlists saved by the user and process synced playlist if you give an array of know youtube synced playlists 
        /// The YoutubePlaylists array should contains the synced playlist availables.Warning, this will return your initial array(proceded if there is synced playlist) + saved playlists
        /// The second outputed var(the string) is the error message that should be displayed to the user(the list will be null if there is an error)
        /// </summary>
        /// <param name="SyncedPlaylists"></param>
        /// <param name="UiCallback">A callback that will tell the ui to update a synced playlist with the new data got from the item.</param>
        /// <returns></returns>
        public static async Task<List<PlaylistItem>> GetSavedYoutubePlaylists(List<PlaylistItem> SyncedPlaylists, Action<PlaylistItem, int> UiCallback)
        {
            List<PlaylistItem> SavedPlaylists = new List<PlaylistItem>();
            await Task.Run(() =>
            {
                SQLiteConnection db = new SQLiteConnection(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "SavedPlaylists.sqlite"));
                db.CreateTable<SavedPlaylist>();

                SavedPlaylists = db.Table<SavedPlaylist>().ToList().ConvertAll(x => { PlaylistItem y = x; y.YoutubeID = x.YoutubeID; y.LocalID = -1; return y; });
            });

            List<PlaylistItem> YoutubePlaylists = new List<PlaylistItem>();

            foreach (PlaylistItem item in SavedPlaylists)
                ProcessPlaylistSyncState(item, SyncedPlaylists, YoutubePlaylists, UiCallback);

            return YoutubePlaylists;
        }

        /// <summary>
        /// Complete the synced playlist array and the youtube playlists array with a new youtube playlist. The method will add the item to the right array or if the playlist is contained in one of the array, it will complete this item.
        /// </summary>
        /// <param name="item">The playlist you want to process</param>
        /// <param name="SyncedPlaylists">An array of known synced playlists (Will be updated)</param>
        /// <param name="YoutubePlaylists">An array of youtube playlists. (Will be updated)</param>
        /// <param name="UiCallback">A callback that will tell the ui to update a synced playlist with the new data got from the item.</param>
        public static void ProcessPlaylistSyncState(PlaylistItem item, List<PlaylistItem> SyncedPlaylists, List<PlaylistItem> YoutubePlaylists, Action<PlaylistItem, int> UiCallback)
        {
            PlaylistItem syncedItem = SyncedPlaylists?.Find(x => x.YoutubeID == item.YoutubeID);
            if (syncedItem != null)
            {
                syncedItem.Snippet = item.Snippet;
                syncedItem.Count = item.Count;

                if(syncedItem.ImageURL == null)
                {
                    syncedItem.ImageURL = item.ImageURL;
                    Task.Run(() =>
                    {
                        SQLiteConnection db = new SQLiteConnection(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "SyncedPlaylists.sqlite"));
                        db.CreateTable<PlaylistItem>();
                        db.InsertOrReplace(syncedItem);
                    });
                }
            }
            else if (SyncedPlaylists?.Find(x => x.Name == item.Name) != null)
            {
                /*We couldn't find a match of a synced playlist with the exact youtube id but we found a synced playlist with the exact same name as this one (item). 
                * We bind them and complete the database for future calls. */
                syncedItem = SyncedPlaylists.Find(x => x.Name == item.Name);
                int syncIndex = SyncedPlaylists.IndexOf(syncedItem);
                item.LocalID = syncedItem.LocalID;
                item.SyncState = SyncState.True;

                //If the URL is the youtube "no thumb", we don't want to save this in the database. We'll wait for the next real thumb.
                if (item.Count == 0)
                    item.ImageURL = null;

                SyncedPlaylists[syncIndex] = item;

                if (UiCallback != null)
                    MainActivity.instance.RunOnUiThread(() => { UiCallback.Invoke(item, syncIndex); });

                Task.Run(() =>
                {
                    SQLiteConnection db = new SQLiteConnection(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "SyncedPlaylists.sqlite"));
                    db.CreateTable<PlaylistItem>();
                    db.InsertOrReplace(item);
                });
            }
            else
                YoutubePlaylists.Add(item);
        }
        #endregion

        #region LocalPlaylists
        /// <summary>
        /// Return a list containing all the songs in the local playlist.
        /// </summary>
        /// <param name="LocalID"></param>
        /// <returns></returns>
        public async static Task<List<Song>> GetTracksFromLocalPlaylist(long LocalID)
        {
            if (Looper.MyLooper() == null)
                Looper.Prepare();

            List<Song> songs = new List<Song>();
            Uri musicUri = Playlists.Members.GetContentUri("external", LocalID);
            await Task.Run(() => 
            {
                if (Looper.MyLooper() == null)
                    Looper.Prepare();

                CursorLoader cursorLoader = new CursorLoader(Application.Context, musicUri, null, null, null, null);
                ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();

                if (musicCursor != null && musicCursor.MoveToFirst())
                {
                    int titleID = musicCursor.GetColumnIndex(Media.InterfaceConsts.Title);
                    int artistID = musicCursor.GetColumnIndex(Media.InterfaceConsts.Artist);
                    int albumID = musicCursor.GetColumnIndex(Media.InterfaceConsts.Album);
                    int thisID = musicCursor.GetColumnIndex(Media.InterfaceConsts.Id);
                    int pathID = musicCursor.GetColumnIndex(Media.InterfaceConsts.Data);
                    do
                    {
                        string Artist = musicCursor.GetString(artistID);
                        string Title = musicCursor.GetString(titleID);
                        string Album = musicCursor.GetString(albumID);
                        long AlbumArt = musicCursor.GetLong(musicCursor.GetColumnIndex(Albums.InterfaceConsts.AlbumId));
                        long id = musicCursor.GetLong(thisID);
                        string path = musicCursor.GetString(pathID);

                        if (Title == null)
                            Title = "Unknown Title";
                        if (Artist == null)
                            Artist = "Unknow Artist";
                        if (Album == null)
                            Album = "Unknow Album";

                        songs.Add(new Song(Title, Artist, Album, null, AlbumArt, id, path));
                    }
                    while (musicCursor.MoveToNext());
                    musicCursor.Close();
                }
            });

            return songs;
        }

        /// <summary>
        /// Will create a local playlist and add an array of songs in this playlist
        /// </summary>
        /// <param name="name">The name of the playlist</param>
        /// <param name="items">The array of songs you want to add. Can be local one or youtube one, it will download them and add them after.</param>
        /// <param name="syncedPlaylist">True if you want the playlist to be created and synced on youtube too</param>
        public async static void CreateLocalPlaylist(string name, Song[] items, bool syncedPlaylist = false)
        {
            if (!await MainActivity.instance.GetWritePermission())
                return;

            //Create the playlist in the local storage db.
            ContentValues value = new ContentValues();
            value.Put(Playlists.InterfaceConsts.Name, name);
            MainActivity.instance.ContentResolver.Insert(Playlists.ExternalContentUri, value);

            long playlistID = await GetPlaylistID(name);

            if (items != null && items.Length > 0)
            {
                LocalManager.AddToPlaylist(items, name, playlistID); //Will only add files already downloaded
                YoutubeManager.Download(items, name); //Will download missing files and add them (if there was youtube songs in the items array.
            }

            if (syncedPlaylist)
            {
                await Task.Run(() =>
                {
                    SQLiteConnection db = new SQLiteConnection(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "SyncedPlaylists.sqlite"));
                    db.CreateTable<PlaylistItem>();
                    db.InsertOrReplace(new PlaylistItem(name, playlistID, null));
                });
            }
        }

        /// <summary>
        /// Return the id of a local playlist by using it's name. Return -1 if the name is not found.
        /// </summary>
        /// <param name="playlistName"></param>
        /// <returns></returns>
        public async static Task<long> GetPlaylistID(string playlistName)
        {
            Uri uri = Playlists.ExternalContentUri;
            return await Task.Run(() =>
            {
                if (Looper.MyLooper() == null)
                    Looper.Prepare();

                CursorLoader loader = new CursorLoader(Application.Context, uri, null, null, null, null);
                ICursor cursor = (ICursor)loader.LoadInBackground();

                if (cursor != null && cursor.MoveToFirst())
                {
                    int nameID = cursor.GetColumnIndex(MediaStore.Audio.Playlists.InterfaceConsts.Name);
                    int plID = cursor.GetColumnIndex(MediaStore.Audio.Playlists.InterfaceConsts.Id);
                    do
                    {
                        string name = cursor.GetString(nameID);

                        if (name != playlistName)
                            continue;

                        return cursor.GetLong(plID);
                    }
                    while (cursor.MoveToNext());
                    cursor.Close();
                }
                return -1;
            });
        }

        /// <summary>
        /// Remove a track from a local playlist
        /// </summary>
        /// <param name="song"></param>
        /// <param name="LocalPlaylistID"></param>
        public async static void RemoveFromLocalPlaylist(Song song, long LocalPlaylistID)
        {
            await Task.Run(() => 
            {
                ContentResolver resolver = MainActivity.instance.ContentResolver;
                Uri uri = MediaStore.Audio.Playlists.Members.GetContentUri("external", LocalPlaylistID);
                resolver.Delete(uri, MediaStore.Audio.Playlists.Members.AudioId + "=?", new string[] { song.Id.ToString() });
            });
        }
        #endregion

        #region YoutubePlaylists
        /// <summary>
        /// This will list all the tracks contained in the playlist.
        /// </summary>
        /// <param name="YoutubeID">The id of the youtube playlist</param>
        /// <param name="action">An action to exectute before the end of the listing, allowing the app to start playback before the end of the execution of this method.</param
        /// <param name="positionListening">The position of the item that will be used for the action.</param>
        /// <returns></returns>
        public async static Task<List<Song>> GetTracksFromYoutubePlaylist(string YoutubeID, Action<Song> action = null, int positionListening = -1)
        {
            List<Song> tracks = new List<Song>();

            if (!await MainActivity.instance.WaitForYoutube())
            {
                Toast.MakeText(Application.Context, Resource.String.youtube_loading_error, ToastLength.Long).Show();
                return null;
            }

            try
            {
                string nextPageToken = "";
                while (nextPageToken != null)
                {
                    var ytPlaylistRequest = YoutubeManager.YoutubeService.PlaylistItems.List("snippet, contentDetails");
                    ytPlaylistRequest.PlaylistId = YoutubeID;
                    ytPlaylistRequest.MaxResults = 50;
                    ytPlaylistRequest.PageToken = nextPageToken;

                    var ytPlaylist = await ytPlaylistRequest.ExecuteAsync();

                    foreach (var item in ytPlaylist.Items)
                    {
                        if (item.Snippet.Title != "[Deleted video]" && item.Snippet.Title != "Private video" && item.Snippet.Title != "Deleted video")
                        {
                            Song song = new Song(item.Snippet.Title, item.Snippet.ChannelTitle, item.Snippet.Thumbnails.Default__.Url, item.ContentDetails.VideoId, -1, -1, item.ContentDetails.VideoId, true, false);
                            tracks.Add(song);

                            if (tracks.Count == positionListening + 1)
                                action?.Invoke(tracks.Last());
                        }
                    }

                    nextPageToken = ytPlaylist.NextPageToken;
                }

                return tracks;
            }
            catch (System.Net.Http.HttpRequestException)
            {
                MainActivity.instance.Timout();
                return tracks;
            }
        }


        /// <summary>
        /// Will create a youtube playlist and add an array of songs in this playlist
        /// </summary>
        /// <param name="name">The name of the playlist</param>
        /// <param name="items">The array of songs you want to add. Can be local one or youtube one, it will download them and add them after.</param>
        public async static void CreateYoutubePlaylist(string playlistName, Song[] items)
        {
            try
            {
                Playlist playlist = new Playlist();
                PlaylistSnippet snippet = new PlaylistSnippet();
                PlaylistStatus status = new PlaylistStatus();
                snippet.Title = playlistName;
                playlist.Snippet = snippet;
                playlist.Status = status;

                var createRequest = YoutubeManager.YoutubeService.Playlists.Insert(playlist, "snippet, status");
                Playlist response = await createRequest.ExecuteAsync();

                YoutubeManager.AddToPlaylist(items, response.Id);
            }
            catch (System.Net.Http.HttpRequestException)
            {
                MainActivity.instance.Timout();
            }
        }

        /// <summary>
        /// Will set the TrackID of the song using the playlist given with the YoutubeID var. (The trackID represent a track in a youtube playlist).
        /// </summary>
        /// <param name="song"></param>
        /// <param name="PlaylistYoutubeID"></param>
        /// <returns></returns>
        public static async Task<Song> CompleteItem(Song song, string PlaylistYoutubeID)
        {
            song.TrackID = null;
            if (await MainActivity.instance.WaitForYoutube())
            {
                try
                {
                    var request = YoutubeManager.YoutubeService.PlaylistItems.List("snippet");
                    request.PlaylistId = PlaylistYoutubeID;
                    request.VideoId = song.YoutubeID;
                    request.MaxResults = 1;

                    var result = await request.ExecuteAsync();
                    if (result.Items.Count > 0)
                        song.TrackID = result.Items[0].Id;
                    else
                        song.Title = null;
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    MainActivity.instance.Timout();
                }
            }
            else
                MainActivity.instance.Timout();

            return song;
        }

        /// <summary>
        /// Remove a song from a youtube playlist. Identify the song and the playlist directly with the TrackID.
        /// </summary>
        /// <param name="TrackID"></param>
        public static async void RemoveFromYoutubePlaylist(string TrackID)
        {
            if (TrackID == null)
                return;

            try
            {
                await YoutubeManager.YoutubeService.PlaylistItems.Delete(TrackID).ExecuteAsync();
            }
            catch (System.Net.Http.HttpRequestException)
            {
                MainActivity.instance.Timout();
            }
        }
        #endregion

        #region YoutubeFork
        /// <summary>
        /// Return true if the playlist is already forked.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public async static Task<bool> IsForked(PlaylistItem item)
        {
            List<PlaylistItem> SavedPlaylistt = await GetSavedYoutubePlaylists(null, null);
            if (SavedPlaylistt.Count(x => x.YoutubeID == item.YoutubeID) > 0)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Save a playlist in the user library.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static async void ForkPlaylist(PlaylistItem item)
        {
            SavedPlaylist pl = new SavedPlaylist(item);
            await Task.Run(() =>
            {
                SQLiteConnection db = new SQLiteConnection(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "SavedPlaylists.sqlite"));
                db.CreateTable<SavedPlaylist>();
                db.InsertOrReplace(pl);
            });

            Snackbar snackBar = Snackbar.Make(MainActivity.instance.FindViewById(Resource.Id.snackBar), MainActivity.instance.GetString(Resource.String.playlist_saved), Snackbar.LengthLong);
            snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
            snackBar.Show();
        }

        /// <summary>
        /// Remove a playlist from the user library.
        /// </summary>
        /// <param name="item"></param>
        public static async void Unfork(PlaylistItem item)
        {
            SavedPlaylist pl = new SavedPlaylist(item);
            await Task.Run(() =>
            {
                SQLiteConnection db = new SQLiteConnection(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "SavedPlaylists.sqlite"));
                db.CreateTable<SavedPlaylist>();
                db.Delete(pl);
            });

            Snackbar snackBar = Snackbar.Make(MainActivity.instance.FindViewById(Resource.Id.snackBar), MainActivity.instance.GetString(Resource.String.playlist_unsaved), Snackbar.LengthLong);
            snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
            snackBar.Show();
        }
        #endregion
    }
}