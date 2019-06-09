using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Opus.Adapter;
using Opus.Api;
using Opus.Api.Services;
using Opus.DataStructure;
using Opus.Others;
using Square.Picasso;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application = Android.App.Application;
using PlaylistItem = Opus.DataStructure.PlaylistItem;

namespace Opus.Fragments
{
    public class Playlist : Fragment
    {
        public static Playlist instance;
        public RecyclerView ListView;
        private PlaylistAdapter adapter;
        private View LoadingView;
        private bool populating = false;

        private readonly List<PlaylistItem> LocalPlaylists = new List<PlaylistItem>();
        private readonly List<PlaylistItem> YoutubePlaylists = new List<PlaylistItem>();


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
            View view = inflater.Inflate(Resource.Layout.CompleteRecycler, container, false);
            LoadingView = view.FindViewById(Resource.Id.loading);
            LoadingView.Visibility = ViewStates.Visible;
            ListView = view.FindViewById<RecyclerView>(Resource.Id.recycler);
            ListView.SetLayoutManager(new LinearLayoutManager(Application.Context));
            instance = this;

            populating = false;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            PopulateView();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
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
                (List<PlaylistItem> locPlaylists, string error) = await PlaylistManager.GetLocalPlaylists(false);
                if (instance == null)
                    return;

                if (error != null) //an error has occured
                    LocalPlaylists.Add(new PlaylistItem("Error", -1) { Owner = error });

                //Handle synced playlist from the local playlist array we had before.
                (List<PlaylistItem> loc, List<PlaylistItem> SyncedPlaylists) = await PlaylistManager.ProcessSyncedPlaylists(locPlaylists);

                if (instance == null)
                    return;

                if(error == null)
                {
                    if (loc == null || loc.Count == 0) //Every local playlist is a synced one
                        LocalPlaylists.Add(new PlaylistItem("EMPTY", -1) { Owner = Resources.GetString(Resource.String.local_playlist_empty) });
                    else
                        LocalPlaylists.AddRange(loc);
                }

                YoutubePlaylists.AddRange(SyncedPlaylists);

                //Display this for now, we'll load non synced youtube playlist in the background.
                LoadingView.Visibility = ViewStates.Gone;
                YoutubePlaylists.Add(Loading);
                adapter = new PlaylistAdapter(LocalPlaylists, YoutubePlaylists);
                ListView.SetAdapter(adapter);
                adapter.ItemClick += ListView_ItemClick;
                adapter.ItemLongCLick += ListView_ItemLongClick;
                ListView.SetItemAnimator(new DefaultItemAnimator());

                //Youtube saved playlists
                List<PlaylistItem> yt = await PlaylistManager.GetSavedYoutubePlaylists(YoutubePlaylists, YoutubeItemSynced);

                if (instance == null)
                    return;

                if (yt != null)
                {
                    int startPos = YoutubePlaylists.Count - 1;
                    YoutubePlaylists.InsertRange(startPos, yt);
                    adapter.NotifyItemRangeInserted(LocalPlaylists.Count + startPos, yt.Count);
                }

                //Youtube owned playlists
                (yt, error) = await PlaylistManager.GetOwnedYoutubePlaylists(YoutubePlaylists, YoutubeItemSynced);

                if (instance == null)
                    return;

                if (error != null)
                {
                    System.Console.WriteLine("&Error != null");
                    YoutubePlaylists.Remove(Loading);
                    adapter.NotifyItemRemoved(LocalPlaylists.Count + YoutubePlaylists.Count);
                    YoutubePlaylists.Add(new PlaylistItem("Error", null) { Owner = error, HasWritePermission = yt != null }); //I use this to check witch error is called. Not really clean but it'll go.
                    adapter.NotifyItemInserted(LocalPlaylists.Count + YoutubePlaylists.Count);
                    populating = false;
                    SyncError();
                    adapter.forkSaved = true;
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

        public async void StartSyncing(string playlistName)
        {
            int LocalIndex = LocalPlaylists.FindIndex(x => x.Name == playlistName);
            if (LocalIndex != -1)
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
            if(YoutubeIndex != -1)
            {
                YoutubePlaylists[YoutubeIndex].SyncState = SyncState.Loading;
                PlaylistHolder holder = (PlaylistHolder)ListView.GetChildViewHolder(ListView.GetChildAt(LocalPlaylists.Count + YoutubeIndex));
                holder.sync.Visibility = ViewStates.Gone;
                holder.SyncLoading.Visibility = ViewStates.Visible;
                if (MainActivity.Theme == 1)
                    holder.SyncLoading.IndeterminateTintList = ColorStateList.ValueOf(Color.White);
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

        //We give the item and the index since the public "YoutubePlaylists" array is not updated yet.
        private void YoutubeItemSynced(PlaylistItem item, int syncedPlaylistIndex)
        {
            /*The display order is 
             *  - Local Header
             *  - Local Playlists
             *  - Youtube Header
             *  - Synced Playlists
             *  Since local header and local playlists are both contained in the "LocalPlaylists" array, to get the position of the syncedPlaylist,
             *  we need to sum the LocalPlaylists count (this sum get the position of the youtube header) and then we add the syncedPlaylistIndex.*/
            PlaylistHolder holder = (PlaylistHolder)ListView.FindViewHolderForAdapterPosition(LocalPlaylists.Count + syncedPlaylistIndex);
            if(holder != null)
            {
                holder.Owner.Text = item.Owner;
                Picasso.With(Application.Context).Load(item.ImageURL).Placeholder(Resource.Color.background_material_dark).Resize(400, 400).CenterCrop().Into(holder.AlbumArt);

                if (item.HasWritePermission)
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

        public async void RefreshLocalPlaylists()
        {
            int count = LocalPlaylists.Count;
            LocalPlaylists.Clear();
            LocalPlaylists.Add(new PlaylistItem("Header", -1));

            (List<PlaylistItem> locPlaylists, string error) = await PlaylistManager.GetLocalPlaylists(true);
            if (instance == null)
                return;

            if (error != null) //an error has occured
                LocalPlaylists.Add(new PlaylistItem("Error", -1) { Owner = error });

            //Handle synced playlist from the local playlist array we had before.
            (List<PlaylistItem> loc, _) = await PlaylistManager.ProcessSyncedPlaylists(locPlaylists);

            if (instance == null)
                return;

            if (error == null)
            {
                if (loc == null || loc.Count == 0) //Every local playlist is a synced one
                    LocalPlaylists.Add(new PlaylistItem("EMPTY", -1) { Owner = GetString(Resource.String.local_playlist_empty) });
                else
                    LocalPlaylists.AddRange(loc);
            }

            adapter.NotifyItemRangeChanged(1, count - 1);
            if(LocalPlaylists.Count > count)
                adapter.NotifyItemRangeInserted(count, LocalPlaylists.Count - count);
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
                        if(YoutubeExplode.YoutubeClient.TryParsePlaylistId(view.FindViewById<EditText>(Resource.Id.playlistURL).Text, out string playlistID))
                        {
                            PlaylistItem addPlaylist = await PlaylistManager.GetPlaylist(playlistID);
                            PlaylistManager.ForkPlaylist(addPlaylist);

                            if (YoutubePlaylists[YoutubePlaylists.Count - 1].Name == "EMPTY" || YoutubePlaylists[YoutubePlaylists.Count - 1].Name == "Error")
                            {
                                if (YoutubePlaylists[YoutubePlaylists.Count - 1].Name == "EMPTY")
                                {
                                    YoutubePlaylists.Insert(YoutubePlaylists.Count - 2, addPlaylist);
                                    YoutubePlaylists.RemoveAt(2);
                                    adapter.NotifyItemChanged(LocalPlaylists.Count + YoutubePlaylists.Count - 2);
                                }
                                else
                                {
                                    YoutubePlaylists.Insert(YoutubePlaylists.Count - 1, addPlaylist);
                                    adapter.NotifyItemInserted(LocalPlaylists.Count + YoutubePlaylists.Count - 2);
                                }
                            }
                            else
                            {
                                YoutubePlaylists.Add(addPlaylist);
                                adapter.NotifyItemInserted(LocalPlaylists.Count + YoutubePlaylists.Count - 1);
                            }
                        }
                        else
                        {
                            Snackbar snackBar = Snackbar.Make(MainActivity.instance.FindViewById(Resource.Id.snackBar), MainActivity.instance.GetString(Resource.String.badplaylisturl), Snackbar.LengthLong);
                            snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
                            snackBar.Show();
                        }
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
            }

            instance = null;
            MainActivity.instance.contentRefresh.Refresh -= OnRefresh;
            MainActivity.instance.contentRefresh.Refresh -= OnRefresh;

            MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, PlaylistTracks.NewInstance(playlist)).AddToBackStack("Playlist Track").Commit();
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
                    PlaylistManager.PlayInOrder(item);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.Shuffle, Resources.GetString(Resource.String.random_play), (sender, eventArg) =>
                {
                    PlaylistManager.Shuffle(item);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.Queue, Resources.GetString(Resource.String.add_to_queue), (sender, eventArg) =>
                {
                    PlaylistManager.AddToQueue(item);
                    bottomSheet.Dismiss();
                })
            };

            if (local || item.HasWritePermission)
            {
                actions.AddRange(new BottomSheetAction[]{ new BottomSheetAction(Resource.Drawable.Edit, Resources.GetString(Resource.String.rename), (sender, eventArg) =>
                {
                    PlaylistManager.Rename(item, () => 
                    {
                        adapter.NotifyItemChanged(Position);
                    });
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.Delete, Resources.GetString(Resource.String.delete), (sender, eventArg) =>
                {
                    PlaylistManager.Delete(item, () => 
                    {
                        if(local)
                        {
                            LocalPlaylists.RemoveAt(Position);
                            adapter.NotifyItemRemoved(Position);

                            if (LocalPlaylists.Count == 1)
                            {
                                LocalPlaylists.Add(new PlaylistItem("EMPTY", -1) { Owner = Resources.GetString(Resource.String.local_playlist_empty) });
                                adapter.NotifyItemInserted(1);
                            }
                        }
                        else
                        {
                            YoutubePlaylists.RemoveAt(Position - LocalPlaylists.Count);
                            adapter.NotifyItemRemoved(Position);

                            if (YoutubePlaylists.Count == 1)
                            {
                                YoutubePlaylists.Add(new PlaylistItem("EMPTY", null) { Owner = Resources.GetString(Resource.String.youtube_playlist_empty) });
                                adapter.NotifyItemInserted(LocalPlaylists.Count + YoutubePlaylists.Count);
                            }
                        }
                    });    
                    bottomSheet.Dismiss();
                })});
            }

            if(item.SyncState == SyncState.True)
            {
                actions.AddRange(new BottomSheetAction[]{ new BottomSheetAction(Resource.Drawable.Sync, Resources.GetString(Resource.String.sync_now), (sender, eventArg) =>
                {
                    YoutubeManager.DownloadPlaylist(item.Name, item.YoutubeID, true, true);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.SyncDisabled, Resources.GetString(Resource.String.stop_sync), (sender, eventArg) =>
                {
                    PlaylistManager.StopSyncingDialog(item, () => 
                    {
                        YoutubePlaylists[Position - LocalPlaylists.Count].LocalID = 0;
                        YoutubePlaylists[Position - LocalPlaylists.Count].SyncState = SyncState.False;
                        PlaylistHolder holder = (PlaylistHolder)ListView.GetChildViewHolder(ListView.GetChildAt(Position));
                        holder.sync.Visibility = ViewStates.Gone;
                        holder.SyncLoading.Visibility = ViewStates.Gone;

                        PlaylistItem LocalPlaylist = new PlaylistItem(YoutubePlaylists[Position - LocalPlaylists.Count].Name, item.LocalID, YoutubePlaylists[Position - LocalPlaylists.Count].Count);
                        if (LocalPlaylists.Count == 2 && LocalPlaylists[1].Name == "EMPTY")
                        {
                            LocalPlaylists.RemoveAt(1);
                            adapter.NotifyItemRemoved(1);
                        }

                        LocalPlaylists.Add(LocalPlaylist);
                        adapter.NotifyItemInserted(LocalPlaylists.Count);
                    });
                    bottomSheet.Dismiss();
                })});
            }
            else if (!local)
            {
                actions.Add(new BottomSheetAction(Resource.Drawable.Sync, Resources.GetString(Resource.String.sync), (sender, eventArg) => 
                {
                    YoutubeManager.DownloadPlaylist(item.Name, item.YoutubeID, true, true);
                    bottomSheet.Dismiss();
                }));

                if(!item.HasWritePermission)
                {
                    actions.Add(new BottomSheetAction(Resource.Drawable.Delete, Resources.GetString(Resource.String.unfork), (sender, eventArg) =>
                    {
                        if (item.SyncState == SyncState.Error)
                            PlaylistManager.StopSyncing(item);

                        PlaylistManager.Unfork(item);
                        YoutubePlaylists.RemoveAt(Position - LocalPlaylists.Count);
                        adapter.NotifyItemRemoved(Position);
                        bottomSheet.Dismiss();
                    }));
                }
            }

            bottomSheet.FindViewById<ListView>(Resource.Id.bsItems).Adapter = new BottomSheetAdapter(MainActivity.instance, Resource.Layout.BottomSheetText, actions);
            bottomSheet.Show();
        }

        public void CheckForSync()
        {
            for (int i = 1; i < YoutubePlaylists.Count; i++)
            {
                if (YoutubePlaylists[i].SyncState != SyncState.False && YoutubePlaylists[i].SyncState != SyncState.Error && Downloader.queue.Find(x => x.playlist == YoutubePlaylists[i].Name && (x.State == DownloadState.Downloading || x.State == DownloadState.Initialization || x.State == DownloadState.MetaData || x.State == DownloadState.None)) == null)
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

        public override void OnResume()
        {
            base.OnResume();
            instance = this;
        }
    }
}