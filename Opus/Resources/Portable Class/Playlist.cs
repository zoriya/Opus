using Android.Content;
using Android.Content.Res;
using Android.Database;
using Android.Graphics;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Opus.Resources.values;
using SQLite;
using Square.Picasso;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Android.Provider.MediaStore.Audio;
using CursorLoader = Android.Support.V4.Content.CursorLoader;
using Application = Android.App.Application;

namespace Opus.Resources.Portable_Class
{
    public class Playlist : Fragment
    {
        public static Playlist instance;
        public RecyclerView ListView;
        private PlaylistAdapter adapter;
        private bool populating = false;

        private List<PlaylistItem> LocalPlaylists = new List<PlaylistItem>();
        private List<PlaylistItem> YoutubePlaylists = new List<PlaylistItem>();


        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            MainActivity.instance.contentRefresh.Refresh += OnRefresh;
        }

        public override void OnDestroy()
        {
            MainActivity.instance.contentRefresh.Refresh -= OnRefresh;
            base.OnDestroy();
            instance = null;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.RecyclerFragment, container, false);
            ListView = view.FindViewById<RecyclerView>(Resource.Id.recycler);
            ListView.SetLayoutManager(new LinearLayoutManager(Android.App.Application.Context));
            instance = this;

#pragma warning disable CS4014
            populating = false;
            PopulateView();
            return view;
        }

        public async Task PopulateView()
        {
            if (!populating)
            {
                populating = true;

                //Initialisation
                LocalPlaylists.Clear();
                YoutubePlaylists.Clear();
                LocalPlaylists.Add(new PlaylistItem("Header", -1));
                YoutubePlaylists.Add(new PlaylistItem("Header", null));
                PlaylistItem Loading = new PlaylistItem("Loading", null);
                
                //Get all local playlist and display an error message if we have an error.
                (List<PlaylistItem> locPlaylists, string error) = await GetLocalPlaylists();

                if (instance == null)
                    return;

                if (locPlaylists == null) //an error has occured
                    LocalPlaylists.Add(new PlaylistItem("EMPTY", -1) { Owner = error });


                //Handle synced playlist from the local playlist array we had before.
                (List<PlaylistItem> loc, List<PlaylistItem> SyncedPlaylists) = await ProcessSyncedPlaylists(locPlaylists);

                if (instance == null)
                    return;

                LocalPlaylists.AddRange(loc);
                YoutubePlaylists.AddRange(SyncedPlaylists);


                //Display this for now, we'll load non synced youtube playlist in the background.
                YoutubePlaylists.Add(Loading);
                adapter = new PlaylistAdapter(LocalPlaylists, YoutubePlaylists);
                ListView.SetAdapter(adapter);
                adapter.ItemClick += ListView_ItemClick;
                adapter.ItemLongCLick += ListView_ItemLongClick;
                ListView.SetItemAnimator(new DefaultItemAnimator());

                //Youtube owned playlists
                (List<PlaylistItem> yt, string err) = await GetOwnedYoutubePlaylists(SyncedPlaylists);

                if (instance == null)
                    return;

                if (yt == null)
                {
                    YoutubePlaylists.Remove(Loading);
                    adapter.NotifyItemRemoved(LocalPlaylists.Count + YoutubePlaylists.Count);
                    YoutubePlaylists.Add(new PlaylistItem("Error", null)); //Should use the "err" var here
                    adapter.NotifyItemInserted(LocalPlaylists.Count + YoutubePlaylists.Count);
                    populating = false;
                    SyncError();
                    return;
                }
                else
                {
                    int startPos = YoutubePlaylists.Count - 1;
                    YoutubePlaylists.InsertRange(startPos, yt);
                    adapter.NotifyItemRangeInserted(LocalPlaylists.Count + startPos, yt.Count);
                }

                //Youtube saved playlists
                (yt, error) = await GetSavedYoutubePlaylists(SyncedPlaylists);

                if (instance == null)
                    return;

                if (yt == null)
                {
                    YoutubePlaylists.Remove(Loading);
                    adapter.NotifyItemRemoved(LocalPlaylists.Count + YoutubePlaylists.Count);
                    YoutubePlaylists.Add(new PlaylistItem("Error", null)); //Should use the "error" var here
                    adapter.NotifyItemInserted(LocalPlaylists.Count + YoutubePlaylists.Count);
                    populating = false;
                    SyncError();
                    return;
                }
                else
                {
                    YoutubePlaylists.Remove(Loading);
                    int loadPos = LocalPlaylists.Count + YoutubePlaylists.Count;
                    YoutubePlaylists.AddRange(yt);
                    adapter.NotifyItemChanged(loadPos);
                    adapter.NotifyItemRangeInserted(loadPos + 1, yt.Count - 1);
                    adapter.forkSaved = true;
                }

                if (SyncedPlaylists.Count > 0)
                {
                    List<PlaylistItem> BadSync = SyncedPlaylists.FindAll(x => x.SyncState == SyncState.Loading);
                    for (int i = 0; i < SyncedPlaylists.Count; i++)
                        SyncedPlaylists[i].SyncState = SyncState.Error;

                    LocalPlaylists.AddRange(BadSync);

                    if (BadSync.Count > 0)
                    {
                        if (LocalPlaylists[1].Name == "EMPTY")
                        {
                            LocalPlaylists.RemoveAt(1);
                            adapter.NotifyItemRemoved(1);
                        }
                        adapter.NotifyItemRangeInserted(LocalPlaylists.Count - SyncedPlaylists.Count, BadSync.Count);
                    }
                }

                populating = false;
            }
        }

        private void SyncError()
        {
            for (int i = 1; i < YoutubePlaylists.Count; i++)
            {
                if(YoutubePlaylists[i].SyncState == SyncState.Loading)
                {
                    YoutubePlaylists[i].SyncState = SyncState.Error;
                    PlaylistHolder holder = (PlaylistHolder)ListView.GetChildViewHolder(ListView.GetChildAt(LocalPlaylists.Count + i));
                    holder.sync.SetImageResource(Resource.Drawable.SyncError);
                    holder.sync.Visibility = ViewStates.Visible;
                    holder.SyncLoading.Visibility = ViewStates.Gone;
                    if (MainActivity.Theme == 1)
                        holder.sync.SetColorFilter(Color.White);
                }
            }
        }

        /* This method will return all playlists available on the local storage in the array "playlists".
         * If there is an error, the Task will return an error message to display to the user. */
        public static async Task<(List<PlaylistItem>, string)> GetLocalPlaylists()
        {
            if (!await MainActivity.instance.GetReadPermission())
                return (null, Application.Context.GetString(Resource.String.no_permission));

            List<PlaylistItem> playlists = new List<PlaylistItem>();

            Android.Net.Uri uri = Playlists.ExternalContentUri;
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

                    Android.Net.Uri musicUri = Playlists.Members.GetContentUri("external", id);
                    CursorLoader cursorLoader = new CursorLoader(Application.Context, musicUri, null, null, null, null);
                    ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();

                    playlists.Add(new PlaylistItem(name, id, musicCursor.Count));
                }
                while (cursor.MoveToNext());
                cursor.Close();
            }

            if (playlists.Count == 1)
                return (null, Application.Context.GetString(Resource.String.local_playlist_empty));
            else
                return (playlists, null);
        }

        /* This method will proceed the local playlists and remove synced one from the imput.
         * It will return as first ouput an array containing the true local playlist and as second output the synced youtube playlists.
         * The outputed youtube playlists will already have the right sync state set (loading, synced...) */
        public static async Task<(List<PlaylistItem>, List<PlaylistItem>)> ProcessSyncedPlaylists(List<PlaylistItem> localPlaylists)
        {
            List<PlaylistItem> syncedPlaylists = new List<PlaylistItem>();
            await Task.Run(() =>
            {
                SQLiteConnection db = new SQLiteConnection(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "SyncedPlaylists.sqlite"));
                db.CreateTable<PlaylistItem>();

                syncedPlaylists = db.Table<PlaylistItem>().ToList();
            });

            foreach (PlaylistItem synced in syncedPlaylists)
            {
                PlaylistItem local = localPlaylists?.Find(x => x.LocalID == synced.LocalID);
                if (local != null)
                {
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


        /* This method return all youtube playlists owned by the user and process synced playlist if you give an array of know youtube synced playlists 
         * The YoutubePlaylists array should contains the synced playlist availables. Warning, this will return your initial array (proceded if there is synced playlist) + owned playlists
         * The second outputed var (the string) is the error message that should be displayed to the user (the list will be null if there is an error)*/
        public static async Task<(List<PlaylistItem>, string)> GetOwnedYoutubePlaylists(List<PlaylistItem> SyncedPlaylists)
        {
            if (!await MainActivity.instance.WaitForYoutube())
                return (null, "Error"); //Should have a better error handling

            List<PlaylistItem> YoutubePlaylists = new List<PlaylistItem>();

            try
            {
                YouTubeService youtube = YoutubeEngine.youtubeService;

                PlaylistsResource.ListRequest request = youtube.Playlists.List("snippet,contentDetails");
                request.Mine = true;
                request.MaxResults = 25;
                PlaylistListResponse response = await request.ExecuteAsync();

                for (int i = 0; i < response.Items.Count; i++)
                {
                    Google.Apis.YouTube.v3.Data.Playlist playlist = response.Items[i];
                    PlaylistItem item = new PlaylistItem(playlist.Snippet.Title, playlist.Id, playlist, (int)playlist.ContentDetails.ItemCount)
                    {
                        Owner = playlist.Snippet.ChannelTitle,
                        ImageURL = playlist.Snippet.Thumbnails.High.Url,
                        HasWritePermission = true
                    };

                    AddItemWithSyncCheck(item, SyncedPlaylists, YoutubePlaylists);
                }

                return (YoutubePlaylists, null);
            }
            catch (System.Net.Http.HttpRequestException)
            {
                return (null, "Error"); //Should handle precise error here
            }
        }

        /* This method return all youtube playlists saved by the user and process synced playlist if you give an array of know youtube synced playlists 
         * The YoutubePlaylists array should contains the synced playlist availables. Warning, this will return your initial array (proceded if there is synced playlist) + saved playlists
         * The second outputed var (the string) is the error message that should be displayed to the user (the list will be null if there is an error)*/
        public static async Task<(List<PlaylistItem>, string)> GetSavedYoutubePlaylists(List<PlaylistItem> SyncedPlaylists)
        {
            if (!await MainActivity.instance.WaitForYoutube())
                return (null, "Error"); //Should have a better error handling

            List<PlaylistItem> YoutubePlaylists = new List<PlaylistItem>();

            try
            {
                YouTubeService youtube = YoutubeEngine.youtubeService;

                ChannelSectionsResource.ListRequest forkedRequest = youtube.ChannelSections.List("snippet,contentDetails");
                forkedRequest.Mine = true;
                ChannelSectionListResponse forkedResponse = await forkedRequest.ExecuteAsync();

                foreach (ChannelSection section in forkedResponse.Items)
                {
                    if (section.Snippet.Title == "Saved Playlists")
                    {
                        for (int i = 0; i < section.ContentDetails.Playlists.Count; i++)
                        {
                            PlaylistsResource.ListRequest plRequest = youtube.Playlists.List("snippet, contentDetails");
                            plRequest.Id = section.ContentDetails.Playlists[i];
                            PlaylistListResponse plResponse = await plRequest.ExecuteAsync();

                            Google.Apis.YouTube.v3.Data.Playlist playlist = plResponse.Items[i];
                            playlist.Kind = "youtube#saved";
                            PlaylistItem item = new PlaylistItem(playlist.Snippet.Title, playlist.Id, playlist, (int)playlist.ContentDetails.ItemCount)
                            {
                                Owner = playlist.Snippet.ChannelTitle,
                                ImageURL = playlist.Snippet.Thumbnails.High.Url,
                                HasWritePermission = false
                            };

                            AddItemWithSyncCheck(item, SyncedPlaylists, YoutubePlaylists);
                        }
                    }
                }

                return (YoutubePlaylists, null);
            }
            catch (System.Net.Http.HttpRequestException)
            {
                return (null, "Error"); //Should handle precise error here
            }
        }

        //All change are made here because PlaylistItem and List are classes witch mean that they will be updated automatically on the others methods
        public static void AddItemWithSyncCheck(PlaylistItem item, List<PlaylistItem> SyncedPlaylists, List<PlaylistItem> YoutubePlaylists)
        {
            PlaylistItem syncedItem = SyncedPlaylists?.Find(x => x.YoutubeID == item.YoutubeID);
            if (syncedItem != null)
            {
                syncedItem.Snippet = item.Snippet;
                syncedItem.Count = item.Count;
            }
            else if (SyncedPlaylists?.Find(x => x.Name == item.Name) != null)
            {
                /*We couldn't find a match of a synced playlist with the exact youtube id but we found a synced playlist with the exact same name as this one (item). 
                * We bind them and complete the database for future calls. */
                syncedItem = SyncedPlaylists.Find(x => x.Name == item.Name);
                int syncIndex = SyncedPlaylists.IndexOf(syncedItem);
                item.LocalID = syncedItem.LocalID;
                item.SyncState = SyncState.True;

                SyncedPlaylists[syncIndex] = item;

                if(instance != null)
                    MainActivity.instance.RunOnUiThread(() => { instance.YoutubeItemSynced(item, syncIndex); });

                Task.Run(() =>
                {
                    SQLiteConnection db = new SQLiteConnection(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "SyncedPlaylists.sqlite"));
                    db.CreateTable<PlaylistItem>();
                    db.InsertOrReplace(item);
                });
            }
            else
                YoutubePlaylists.Add(item);
        }

        //We give the item and the index since the public "YoutubePlaylists" array is not updated yet.
        private void YoutubeItemSynced(PlaylistItem item, int syncedPlaylistIndex)
        {
            /*The display order is 
             *  - Local Header
             *  - Local Playlists
             *  - Youtube Header
             *  - Synced Playlists
             *  Since local header and local playlists are both contained in the "LocalPlaylists" array, to get the position of the syncedPlaylist,
             *  we need to sum the LocalPlaylists count (this sum get the position of the youtube header) and then we add the syncedPlaylistIndex.
             *  We need to add one for the youtube header (witch is not in the syncedplaylists array)*/
            PlaylistHolder holder = (PlaylistHolder)ListView.FindViewHolderForAdapterPosition(LocalPlaylists.Count + syncedPlaylistIndex + 1);
            holder.Owner.Text = item.Owner;
            Picasso.With(Application.Context).Load(item.ImageURL).Placeholder(Resource.Color.background_material_dark).Resize(400, 400).CenterCrop().Into(holder.AlbumArt);

            if(item.HasWritePermission)
            {
                holder.edit.Visibility = ViewStates.Visible;
                if (MainActivity.Theme == 1)
                    holder.edit.SetColorFilter(Color.White);
            }

            holder.sync.SetImageResource(Resource.Drawable.Sync);
            holder.sync.Visibility = ViewStates.Visible;
            holder.SyncLoading.Visibility = ViewStates.Gone;
            if (MainActivity.Theme == 1)
                holder.sync.SetColorFilter(Color.White);
        }

        public static Fragment NewInstance()
        {
            if(instance == null)
                instance = new Playlist { Arguments = new Bundle() };
            return instance;
        }

        private async void OnRefresh(object sender, System.EventArgs e)
        {
            await Refresh();
            MainActivity.instance.contentRefresh.Refreshing = false;
        }

        public async Task Refresh()
        {
            await PopulateView();
        }

        private void ListView_ItemClick(object sender, int Position)
        {
            if(Position == LocalPlaylists.Count + YoutubePlaylists.Count)
            {
                View view = LayoutInflater.Inflate(Resource.Layout.SaveAPlaylist, null);
                AlertDialog dialog = new AlertDialog.Builder(Activity, MainActivity.dialogTheme)
                    .SetTitle(Resource.String.add_playlist_msg)
                    .SetView(view)
                    .SetNegativeButton(Resource.String.cancel, (s, eventArgs) => { })
                    .SetPositiveButton(Resource.String.add, async (s, eventArgs) => 
                    {
                        string url = view.FindViewById<EditText>(Resource.Id.playlistURL).Text;
                        string shrinkedURL = url.Substring(url.IndexOf('=') + 1);
                        string playlistID = shrinkedURL;
                        if (shrinkedURL.Contains("&"))
                        {
                            playlistID = shrinkedURL.Substring(0, shrinkedURL.IndexOf("&"));
                        }
                        await YoutubeEngine.ForkPlaylist(playlistID);

                        try
                        {
                            ChannelSectionsResource.ListRequest forkedRequest = YoutubeEngine.youtubeService.ChannelSections.List("snippet,contentDetails");
                            forkedRequest.Mine = true;
                            ChannelSectionListResponse forkedResponse = await forkedRequest.ExecuteAsync();
                            if (instance == null)
                                return;

                            foreach (ChannelSection section in forkedResponse.Items)
                            {
                                if (section.Snippet.Title == "Saved Playlists")
                                {
                                    PlaylistsResource.ListRequest plRequest = YoutubeEngine.youtubeService.Playlists.List("snippet, contentDetails");
                                    plRequest.Id = section.ContentDetails.Playlists[section.ContentDetails.Playlists.Count - 1];
                                    PlaylistListResponse plResponse = await plRequest.ExecuteAsync();

                                    if (instance == null)
                                        return;

                                    Google.Apis.YouTube.v3.Data.Playlist ytPlaylist = plResponse.Items[0];
                                    ytPlaylist.Kind = "youtube#saved";
                                    PlaylistItem item = new PlaylistItem(ytPlaylist.Snippet.Title, ytPlaylist.Id, ytPlaylist, (int)ytPlaylist.ContentDetails.ItemCount)
                                    {
                                        Owner = ytPlaylist.Snippet.ChannelTitle,
                                        ImageURL = ytPlaylist.Snippet.Thumbnails.High.Url,
                                        HasWritePermission = false
                                    };
                                    YoutubePlaylists.Add(item);
                                }
                            }
                        }
                        catch (System.Net.Http.HttpRequestException)
                        {
                            MainActivity.instance.Timout();
                        }
                        catch (Google.GoogleApiException)
                        {
                            Snackbar snackBar = Snackbar.Make(MainActivity.instance.FindViewById(Resource.Id.snackBar), Resource.String.playlist_not_found, Snackbar.LengthLong);
                            snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
                            snackBar.Show();
                        }

                        if (YoutubePlaylists.Count == 3 && YoutubePlaylists[1].Name == "EMPTY")
                        {
                            YoutubePlaylists.RemoveAt(1);
                            adapter.NotifyItemChanged(LocalPlaylists.Count + YoutubePlaylists.Count - 1);
                        }
                        else
                            adapter.NotifyItemInserted(LocalPlaylists.Count + YoutubePlaylists.Count);
                    })
                    .Show();
                return;
            }

            bool local = Position <= LocalPlaylists.Count;
            PlaylistItem playlist = local ?
                LocalPlaylists[Position] :
                YoutubePlaylists[Position - LocalPlaylists.Count];

            if(playlist.SyncState == SyncState.Error && local)
            {
                //Handle sync errors
                /*Shouldn't do this but for now, will do this.*/MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, PlaylistTracks.NewInstance(playlist.LocalID, playlist.Name)).AddToBackStack(null).Commit();
                return;
            }

            instance = null;
            MainActivity.instance.contentRefresh.Refresh -= OnRefresh;
            MainActivity.instance.contentRefresh.Refresh -= OnRefresh;


            if (playlist.SyncState == SyncState.True || playlist.SyncState == SyncState.Loading)
                MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, PlaylistTracks.NewInstance(playlist.YoutubeID, playlist.LocalID, playlist.Name, playlist.HasWritePermission, true, playlist.Owner, playlist.Count, playlist.ImageURL)).AddToBackStack("Playlist Track").Commit();
            else if (local || (playlist.SyncState == SyncState.Error && playlist.LocalID != 0 && playlist.LocalID != -1))
                MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, PlaylistTracks.NewInstance(playlist.LocalID, playlist.Name)).AddToBackStack("Playlist Track").Commit();
            else
                MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, PlaylistTracks.NewInstance(playlist.YoutubeID, playlist.Name, playlist.HasWritePermission, true, playlist.Owner, playlist.Count, playlist.ImageURL)).AddToBackStack("Playlist Track").Commit();
        }

        private void ListView_ItemLongClick(object sender, int position)
        {
            More(position);
        }

        public void More(int Position)
        {
            bool local = Position <= LocalPlaylists.Count;
            PlaylistItem item = local ?
                LocalPlaylists[Position] :
                YoutubePlaylists[Position - LocalPlaylists.Count];

            BottomSheetDialog bottomSheet = new BottomSheetDialog(MainActivity.instance);
            View bottomView = LayoutInflater.Inflate(Resource.Layout.BottomSheet, null);
            bottomView.FindViewById<TextView>(Resource.Id.bsTitle).Text = item.Name;
            if (!local || item.SyncState != SyncState.False)
            {
                bottomView.FindViewById<TextView>(Resource.Id.bsArtist).Text = item.Owner;
                Picasso.With(MainActivity.instance).Load(item.ImageURL).Placeholder(Resource.Color.background_material_dark).Transform(new RemoveBlackBorder(true)).Into(bottomView.FindViewById<ImageView>(Resource.Id.bsArt));
            }
            else
            {
                bottomView.FindViewById<TextView>(Resource.Id.bsArtist).Text = item.Count + " element" + (item.Count == 1 ? "" : "s");
                bottomView.FindViewById<ImageView>(Resource.Id.bsArt).Visibility = ViewStates.Gone;
            }
            bottomSheet.SetContentView(bottomView);

            List<BottomSheetAction> actions = new List<BottomSheetAction>
            {
                new BottomSheetAction(Resource.Drawable.Play, Resources.GetString(Resource.String.play_in_order), (sender, eventArg) =>
                {
                    if (local || item.SyncState == SyncState.True)
                        PlayInOrder(item.LocalID);
                    else
                        PlayInOrder(item.YoutubeID);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.Shuffle, Resources.GetString(Resource.String.random_play), (sender, eventArg) =>
                {
                    if (local || item.SyncState == SyncState.True)
                        RandomPlay(item.LocalID, Activity);
                    else
                        YoutubeEngine.RandomPlay(item.YoutubeID);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.Queue, Resources.GetString(Resource.String.add_to_queue), (sender, eventArg) =>
                {
                    if (local || item.SyncState == SyncState.True)
                        AddToQueue(item.LocalID);
                    else
                        AddToQueue(item.YoutubeID);
                    bottomSheet.Dismiss();
                })
            };

            if (local || item.HasWritePermission)
            {
                actions.AddRange(new BottomSheetAction[]{ new BottomSheetAction(Resource.Drawable.Edit, Resources.GetString(Resource.String.rename), (sender, eventArg) =>
                {
                    if (local)
                        Rename(Position, item);
                    else
                        RenameYoutubePlaylist(Position, item.YoutubeID, item.LocalID);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.Delete, Resources.GetString(Resource.String.delete), (sender, eventArg) =>
                {
                    if(item.SyncState == SyncState.True || item.SyncState == SyncState.Loading)
                    {
                        AlertDialog dialog = new AlertDialog.Builder(MainActivity.instance, MainActivity.dialogTheme)
                            .SetTitle(GetString(Resource.String.delete_playlist, item.Name))
                            .SetPositiveButton(Resource.String.yes, async (s, e) =>
                            {
                                try
                                {
                                    PlaylistsResource.DeleteRequest deleteRequest = YoutubeEngine.youtubeService.Playlists.Delete(item.YoutubeID);
                                    await deleteRequest.ExecuteAsync();

                                    YoutubePlaylists.RemoveAt(Position - LocalPlaylists.Count);
                                    adapter.NotifyItemRemoved(Position);

                                    if (YoutubePlaylists.Count == 1)
                                    {
                                        YoutubePlaylists.Add(new PlaylistItem("EMPTY", null) { Owner = Resources.GetString(Resource.String.youtube_playlist_empty) });
                                        adapter.NotifyItemInserted(LocalPlaylists.Count + YoutubePlaylists.Count);
                                    }
                                }
                                catch (System.Net.Http.HttpRequestException)
                                {
                                    MainActivity.instance.Timout();
                                    return;
                                }

                                await Task.Run(() =>
                                {
                                    SQLiteConnection db = new SQLiteConnection(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "SyncedPlaylists.sqlite"));
                                    db.CreateTable<PlaylistItem>();

                                    db.Delete(db.Table<PlaylistItem>().ToList().Find(x => x.LocalID == item.LocalID));
                                });

                                ContentResolver resolver = Activity.ContentResolver;
                                Android.Net.Uri uri = Playlists.ExternalContentUri;
                                resolver.Delete(Playlists.ExternalContentUri, Playlists.InterfaceConsts.Id + "=?", new string[] { item.LocalID.ToString() });
                            })
                            .SetNegativeButton(Resource.String.no, (s, e) => { })
                            .Create();
                        dialog.Show();
                    }
                    else
                    {
                        if (local)
                            RemovePlaylist(Position, item.LocalID);
                        else
                            DeleteYoutubePlaylist(Position, item.YoutubeID);
                    }
                    
                    bottomSheet.Dismiss();
                })});
            }
            

            if(item.SyncState == SyncState.True)
            {
                actions.AddRange(new BottomSheetAction[]{ new BottomSheetAction(Resource.Drawable.Sync, Resources.GetString(Resource.String.sync_now), (sender, eventArg) =>
                {
                    YoutubeEngine.DownloadPlaylist(item.Name, item.YoutubeID);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.SyncDisabled, Resources.GetString(Resource.String.stop_sync), (sender, eventArg) =>
                {
                    StopSyncing(Position, item.LocalID);
                    bottomSheet.Dismiss();
                })});
            }
            else if (!local && item.HasWritePermission)
            {
                actions.Add(new BottomSheetAction(Resource.Drawable.Sync, Resources.GetString(Resource.String.sync), (sender, eventArg) => 
                {
                    YoutubeEngine.DownloadPlaylist(item.Name, item.YoutubeID);
                    bottomSheet.Dismiss();
                }));
            }
            else if(!local)
            {
                actions.AddRange(new BottomSheetAction[]{ new BottomSheetAction(Resource.Drawable.Sync, Resources.GetString(Resource.String.sync), (sender, eventArg) =>
                {
                    YoutubeEngine.DownloadPlaylist(item.Name, item.YoutubeID);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.Delete, Resources.GetString(Resource.String.unfork), (sender, eventArg) =>
                {
                    Unfork(Position, item.YoutubeID);
                    bottomSheet.Dismiss();
                })});
            }

            bottomSheet.FindViewById<ListView>(Resource.Id.bsItems).Adapter = new BottomSheetAdapter(MainActivity.instance, Resource.Layout.BottomSheetText, actions);
            bottomSheet.Show();
        }

        public static async void PlayInOrder(long playlistID)
        {
            Android.Net.Uri musicUri = Playlists.Members.GetContentUri("external", playlistID);
            List<Song> songs = new List<Song>();
            CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
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

                MusicPlayer.queue.Clear();
                Browse.Play(songs[0]);
                songs.RemoveAt(0);

                while (MusicPlayer.instance == null)
                    await Task.Delay(10);

                MusicPlayer.instance.AddToQueue(songs.ToArray());
            }
        }

        public static async void PlayInOrder(string playlistID)
        {
            List<Song> songs = new List<Song>();

            if (!await MainActivity.instance.WaitForYoutube())
            {
                Toast.MakeText(Android.App.Application.Context, Resource.String.youtube_loading_error, ToastLength.Long).Show();
                return;
            }


            try
            {
                string nextPageToken = "";
                while (nextPageToken != null)
                {
                    var ytPlaylistRequest = YoutubeEngine.youtubeService.PlaylistItems.List("snippet, contentDetails");
                    ytPlaylistRequest.PlaylistId = playlistID;
                    ytPlaylistRequest.MaxResults = 50;
                    ytPlaylistRequest.PageToken = nextPageToken;

                    var ytPlaylist = await ytPlaylistRequest.ExecuteAsync();

                    foreach (var item in ytPlaylist.Items)
                    {
                        if (item.Snippet.Title != "[Deleted video]" && item.Snippet.Title != "Private video" && item.Snippet.Title != "Deleted video")
                        {
                            Song song = new Song(item.Snippet.Title, item.Snippet.ChannelTitle, item.Snippet.Thumbnails.Default__.Url, item.ContentDetails.VideoId, -1, -1, item.ContentDetails.VideoId, true, false);
                            songs.Add(song);
                        }
                    }

                    nextPageToken = ytPlaylist.NextPageToken;
                }

                if (songs.Count == 0)
                    return;

                if (MusicPlayer.isRunning)
                    MusicPlayer.queue?.Clear();

                YoutubeEngine.Play(songs[0].YoutubeID, songs[0].Title, songs[0].Artist, songs[0].Album);
                songs.RemoveAt(0);
                songs.Reverse();

                while (MusicPlayer.instance == null)
                    await Task.Delay(10);

                foreach (Song song in songs)
                {
                    MusicPlayer.instance.AddToQueue(song);
                }
            }
            catch (System.Net.Http.HttpRequestException)
            {
                MainActivity.instance.Timout();
            }
        }

        public static void RandomPlay(long playlistID, Context context)
        {
            List<string> tracksPath = new List<string>();
            Android.Net.Uri musicUri = Playlists.Members.GetContentUri("external", playlistID);

            CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
            ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();

            if (musicCursor != null && musicCursor.MoveToFirst())
            {
                int pathID = musicCursor.GetColumnIndex(Media.InterfaceConsts.Data);
                do
                {
                    tracksPath.Add(musicCursor.GetString(pathID));
                }
                while (musicCursor.MoveToNext());
                musicCursor.Close();
            }

            if (tracksPath.Count == 0)
                return;

            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.PutStringArrayListExtra("files", tracksPath);
            intent.SetAction("RandomPlay");
            context.StartService(intent);

            MainActivity.instance.ShowSmallPlayer();
            MainActivity.instance.ShowPlayer();
        }

        public static void AddToQueue(long playlistID)
        {
            if (MusicPlayer.instance == null || MusicPlayer.queue == null || MusicPlayer.queue?.Count == 0)
            {
                PlayInOrder(playlistID);
                return;
            }

            List<string> tracksPath = new List<string>();
            Android.Net.Uri musicUri = Playlists.Members.GetContentUri("external", playlistID);

            CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
            ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();

            if (musicCursor != null && musicCursor.MoveToFirst())
            {
                int pathID = musicCursor.GetColumnIndex(Media.InterfaceConsts.Data);
                do
                {
                    tracksPath.Add(musicCursor.GetString(pathID));
                }
                while (musicCursor.MoveToNext());
                musicCursor.Close();
            }

            tracksPath.Reverse();

            foreach(string path in tracksPath)
                MusicPlayer.instance.AddToQueue(path);
        }

        public static async void AddToQueue(string playlistID)
        {
            if (MusicPlayer.instance == null || MusicPlayer.queue == null || MusicPlayer.queue?.Count == 0)
            {
                PlayInOrder(playlistID);
                return;
            }

            List<Song> songs = new List<Song>();

            if (!await MainActivity.instance.WaitForYoutube())
            {
                Toast.MakeText(Android.App.Application.Context, Resource.String.youtube_loading_error, ToastLength.Long).Show();
                return;
            }


            try
            {
                string nextPageToken = "";
                while (nextPageToken != null)
                {
                    var ytPlaylistRequest = YoutubeEngine.youtubeService.PlaylistItems.List("snippet, contentDetails");
                    ytPlaylistRequest.PlaylistId = playlistID;
                    ytPlaylistRequest.MaxResults = 50;
                    ytPlaylistRequest.PageToken = nextPageToken;

                    var ytPlaylist = await ytPlaylistRequest.ExecuteAsync();

                    foreach (var item in ytPlaylist.Items)
                    {
                        if (item.Snippet.Title != "[Deleted video]" && item.Snippet.Title != "Private video" && item.Snippet.Title != "Deleted video")
                        {
                            Song song = new Song(item.Snippet.Title, item.Snippet.ChannelTitle, item.Snippet.Thumbnails.Default__.Url, item.ContentDetails.VideoId, -1, -1, item.ContentDetails.VideoId, true, false);
                            songs.Add(song);
                        }
                    }

                    nextPageToken = ytPlaylist.NextPageToken;
                }

                songs.Reverse();

                foreach (Song song in songs)
                    MusicPlayer.instance.AddToQueue(song);
            }
            catch (System.Net.Http.HttpRequestException)
            {
                MainActivity.instance.Timout();
            }
        }

        void Rename(int position, PlaylistItem playlist)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, MainActivity.dialogTheme);
            builder.SetTitle(Resource.String.rename_playlist);
            View view = LayoutInflater.Inflate(Resource.Layout.CreatePlaylistDialog, null);
            builder.SetView(view);
            builder.SetNegativeButton(Resource.String.cancel, (senderAlert, args) => { });
            builder.SetPositiveButton(Resource.String.rename, (senderAlert, args) =>
            {
                playlist.Name = view.FindViewById<EditText>(Resource.Id.playlistName).Text;
                RenamePlaylist(position, playlist);
            });
            builder.Show();
        }

        void RenamePlaylist(int position, PlaylistItem playlist)
        {
            ContentResolver resolver = Activity.ContentResolver;
            Android.Net.Uri uri = Playlists.ExternalContentUri;
            ContentValues value = new ContentValues();
            value.Put(Playlists.InterfaceConsts.Name, playlist.Name);
            resolver.Update(Playlists.ExternalContentUri, value, Playlists.InterfaceConsts.Id + "=?", new string[] { playlist.LocalID.ToString() });
            LocalPlaylists[position].Name = playlist.Name;

            adapter.UpdateElement(position, playlist);
        }

        async void RemovePlaylist(int position, long playlistID)
        {
            if (await MainActivity.instance.GetWritePermission())
            {
                AlertDialog dialog = new AlertDialog.Builder(MainActivity.instance, MainActivity.dialogTheme)
                    .SetTitle(GetString(Resource.String.delete_playlist, LocalPlaylists[position].Name))
                    .SetPositiveButton(Resource.String.yes, (sender, e) =>
                    {
                        ContentResolver resolver = Activity.ContentResolver;
                        Android.Net.Uri uri = Playlists.ExternalContentUri;
                        resolver.Delete(Playlists.ExternalContentUri, Playlists.InterfaceConsts.Id + "=?", new string[] { playlistID.ToString() });
                        LocalPlaylists.RemoveAt(position);
                        adapter.NotifyItemRemoved(position);

                        if (LocalPlaylists.Count == 1)
                        {
                            LocalPlaylists.Add(new PlaylistItem("EMPTY", -1) { Owner = Resources.GetString(Resource.String.local_playlist_empty) });
                            adapter.NotifyItemInserted(1);
                        }
                    })
                    .SetNegativeButton(Resource.String.no, (sender, e) => { })
                    .Create();
                dialog.Show();
            }
        }

        public async void StartSyncing(string playlistName)
        {
            int LocalIndex = LocalPlaylists.FindIndex(x => x.Name == playlistName);
            if(LocalIndex != -1)
            {
                LocalPlaylists.RemoveAt(LocalIndex);
                adapter.NotifyItemRemoved(LocalIndex);
                if (LocalPlaylists.Count == 1)
                {
                    LocalPlaylists.Add(new PlaylistItem("EMPTY", -1) { Owner = Resources.GetString(Resource.String.local_playlist_empty) });
                    adapter.NotifyItemInserted(1);
                }
                await Task.Delay(500);
            }

            int YoutubeIndex = YoutubePlaylists.FindIndex(x => x.Name == playlistName);
            YoutubePlaylists[YoutubeIndex].SyncState = SyncState.Loading;
            PlaylistHolder holder = (PlaylistHolder)ListView.GetChildViewHolder(ListView.GetChildAt(LocalPlaylists.Count + YoutubeIndex));
            holder.sync.Visibility = ViewStates.Gone;
            holder.SyncLoading.Visibility = ViewStates.Visible;
            if (MainActivity.Theme == 1)
                holder.SyncLoading.IndeterminateTintList = ColorStateList.ValueOf(Color.White);
        }

        public void CheckForSync()
        {
            for (int i = 1; i < YoutubePlaylists.Count; i++)
            {
                if (YoutubePlaylists[i].SyncState != SyncState.False && Downloader.queue.Find(x => x.playlist == YoutubePlaylists[i].Name && (x.State == DownloadState.Downloading || x.State == DownloadState.Initialization || x.State == DownloadState.MetaData || x.State == DownloadState.None)) == null)
                {
                    YoutubePlaylists[i].SyncState = SyncState.True;
                    PlaylistHolder holder = (PlaylistHolder)ListView.GetChildViewHolder(ListView.GetChildAt(LocalPlaylists.Count + i));
                    holder.SyncLoading.Visibility = ViewStates.Gone;
                    holder.sync.SetImageResource(Resource.Drawable.Sync);
                    holder.sync.Visibility = ViewStates.Visible;
                    if (MainActivity.Theme == 1)
                        holder.sync.SetColorFilter(Color.White);
                }
            }
        }
        public void SyncCanceled()
        {
            for (int i = 0; i < YoutubePlaylists.Count; i++)
            {
                if(YoutubePlaylists[i].SyncState == SyncState.Loading)
                {
                    YoutubePlaylists[i].SyncState = SyncState.True;
                    adapter.NotifyItemChanged(i);
                }
            }
        }

        async void StopSyncing(int position, long LocalID)
        {
            await Task.Run(() =>
            {
                SQLiteConnection db = new SQLiteConnection(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "SyncedPlaylists.sqlite"));
                db.CreateTable<PlaylistItem>();

                db.Delete(db.Table<PlaylistItem>().ToList().Find(x => x.LocalID == LocalID));
            });
            YoutubePlaylists[position - LocalPlaylists.Count].LocalID = 0;
            YoutubePlaylists[position - LocalPlaylists.Count].SyncState = SyncState.False;
            PlaylistHolder holder = (PlaylistHolder)ListView.GetChildViewHolder(ListView.GetChildAt(position));
            holder.sync.Visibility = ViewStates.Gone;
            holder.SyncLoading.Visibility = ViewStates.Gone;

            PlaylistItem LocalPlaylist = new PlaylistItem(YoutubePlaylists[position - LocalPlaylists.Count].Name, LocalID, YoutubePlaylists[position - LocalPlaylists.Count].Count);
            if (LocalPlaylists.Count == 2 && LocalPlaylists[1].Name == "EMPTY")
            {
                LocalPlaylists.RemoveAt(1);
                adapter.NotifyItemRemoved(1);
            }

            LocalPlaylists.Add(LocalPlaylist);
            adapter.NotifyItemInserted(LocalPlaylists.Count);
        }

        public void RenameYoutubePlaylist(int position, string YoutubeID, long LocalID = -1)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, MainActivity.dialogTheme);
            builder.SetTitle(Resource.String.rename_playlist);
            View view = LayoutInflater.Inflate(Resource.Layout.CreatePlaylistDialog, null);
            builder.SetView(view);
            builder.SetNegativeButton(Resource.String.cancel, (senderAlert, args) => { });
            builder.SetPositiveButton(Resource.String.rename, (senderAlert, args) =>
            {
                RenameYT(position, view.FindViewById<EditText>(Resource.Id.playlistName).Text, YoutubeID, LocalID);
            });
            builder.Show();
        }

        void RenameYT(int position, string name, string YoutubeID, long LocalID = -1)
        {
            try
            {
                YoutubePlaylists[position - LocalPlaylists.Count].Snippet.Snippet.Title = name;
                YoutubePlaylists[position - LocalPlaylists.Count].Snippet.Id = YoutubeID;

                YoutubeEngine.youtubeService.Playlists.Update(YoutubePlaylists[position - LocalPlaylists.Count].Snippet, "snippet").Execute();
                adapter.UpdateElement(position, YoutubePlaylists[position - LocalPlaylists.Count - 1]);
            }
            catch (System.Net.Http.HttpRequestException)
            {
                MainActivity.instance.Timout();
                return;
            }

            if(LocalID != -1)
            {
                ContentResolver resolver = Activity.ContentResolver;
                Android.Net.Uri uri = Playlists.ExternalContentUri;
                ContentValues value = new ContentValues();
                value.Put(Playlists.InterfaceConsts.Name, name);
                resolver.Update(Playlists.ExternalContentUri, value, Playlists.InterfaceConsts.Id + "=?", new string[] { LocalID.ToString() });
            }
        }

        void DeleteYoutubePlaylist(int position, string playlistID)
        {
            AlertDialog dialog = new AlertDialog.Builder(MainActivity.instance, MainActivity.dialogTheme)
                .SetTitle(GetString(Resource.String.delete_playlist, YoutubePlaylists[position - LocalPlaylists.Count].Name))
                .SetPositiveButton(Resource.String.yes, async (sender, e) =>
                {
                    try
                    {
                        PlaylistsResource.DeleteRequest deleteRequest = YoutubeEngine.youtubeService.Playlists.Delete(playlistID);
                        await deleteRequest.ExecuteAsync();

                        YoutubePlaylists.RemoveAt(position - LocalPlaylists.Count);
                        adapter.NotifyItemRemoved(position);

                        if (YoutubePlaylists.Count == 1)
                        {
                            YoutubePlaylists.Add(new PlaylistItem("EMPTY", null) { Owner = Resources.GetString(Resource.String.youtube_playlist_empty) });
                            adapter.NotifyItemInserted(LocalPlaylists.Count + YoutubePlaylists.Count);
                        }
                    }
                    catch (System.Net.Http.HttpRequestException)
                    {
                        MainActivity.instance.Timout();
                    }
                })
                .SetNegativeButton(Resource.String.no, (sender, e) => {  })
                .Create();
            dialog.Show();
        }

        void Unfork(int position, string playlistID)
        {
            AlertDialog dialog = new AlertDialog.Builder(MainActivity.instance, MainActivity.dialogTheme)
                .SetTitle(GetString(Resource.String.unfork_playlist, YoutubePlaylists[position - LocalPlaylists.Count].Name))
                .SetPositiveButton(Resource.String.yes, async (sender, e) =>
                {
                    try
                    {
                        ChannelSectionsResource.ListRequest forkedRequest = YoutubeEngine.youtubeService.ChannelSections.List("snippet,contentDetails");
                        forkedRequest.Mine = true;
                        ChannelSectionListResponse forkedResponse = await forkedRequest.ExecuteAsync();

                        foreach (ChannelSection section in forkedResponse.Items)
                        {
                            if (section.Snippet.Title == "Saved Playlists")
                            {
                                if (section.ContentDetails.Playlists.Count > 1)
                                {
                                    section.ContentDetails.Playlists.Remove(playlistID);
                                    ChannelSectionsResource.UpdateRequest request = YoutubeEngine.youtubeService.ChannelSections.Update(section, "snippet,contentDetails");
                                    ChannelSection response = await request.ExecuteAsync();
                                }
                                else
                                {
                                    ChannelSectionsResource.DeleteRequest delete = YoutubeEngine.youtubeService.ChannelSections.Delete(section.Id);
                                    await delete.ExecuteAsync();
                                }
                            }
                        }

                        YoutubePlaylists.RemoveAt(position - LocalPlaylists.Count - 1);
                        adapter.NotifyItemRemoved(position);

                        if (YoutubePlaylists.Count == 1)
                        {
                            YoutubePlaylists.Add(new PlaylistItem("EMPTY", null) { Owner = Resources.GetString(Resource.String.youtube_playlist_empty) });
                            adapter.NotifyItemInserted(LocalPlaylists.Count + YoutubePlaylists.Count);
                        }
                    }
                    catch (System.Net.Http.HttpRequestException)
                    {
                        MainActivity.instance.Timout();
                    }
                })
                .SetNegativeButton(Resource.String.no, (sender, e) => { })
                .Create();
            dialog.Show();
        }

        public override void OnResume()
        {
            base.OnResume();
            instance = this;
        }
    }
}