using Android.Content;
using Android.Database;
using Android.Net;
using Android.OS;
using Android.Provider;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Opus.Adapter;
using Opus.Api;
using Opus.DataStructure;
using Opus.Others;
using Square.Picasso;
using System.Collections.Generic;
using CursorLoader = Android.Support.V4.Content.CursorLoader;

namespace Opus.Fragments
{
    public class FolderTracks : Fragment, LoaderManager.ILoaderCallbacks
    {
        public static FolderTracks instance;
        public string folderName;
        public string path;
        private RecyclerView ListView;
        public BrowseAdapter adapter;
        private TextView EmptyView;
        private string query;


        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            MainActivity.instance.contentRefresh.Refresh += OnRefresh;
            MainActivity.instance.AddFilterListener(Search);

            MainActivity.instance.SupportActionBar.SetDisplayShowTitleEnabled(true);
            MainActivity.instance.FindViewById(Resource.Id.toolbarLogo).Visibility = ViewStates.Gone;
            MainActivity.instance.DisplaySearch();
        }

        public override void OnDestroy()
        {
            MainActivity.instance.RemoveFilterListener(Search);
            MainActivity.instance.contentRefresh.Refresh -= OnRefresh;
            instance = null;
            base.OnDestroy();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.CompleteRecycler, container, false);
            view.FindViewById(Resource.Id.loading).Visibility = ViewStates.Visible;
            EmptyView = view.FindViewById<TextView>(Resource.Id.empty);
            ListView = view.FindViewById<RecyclerView>(Resource.Id.recycler);
            ListView.SetLayoutManager(new LinearLayoutManager(Android.App.Application.Context));
            ListView.SetItemAnimator(new DefaultItemAnimator());
            adapter = new BrowseAdapter((song, position) =>
            {
                LocalManager.PlayInOrder(path, position, "(" + MediaStore.Audio.Media.InterfaceConsts.Title + " LIKE \"%" + query + "%\" OR " + MediaStore.Audio.Media.InterfaceConsts.Artist + " LIKE \"%" + query + "%\")");
            }, (song, position) =>
            {
                More(song, position);
            }, (position) => 
            {
                LocalManager.ShuffleAll(path);
            });
            ListView.SetAdapter(adapter);

            PopulateList();
            return view;
        }

        public static Fragment NewInstance(string path, string folderName)
        {
            instance = new FolderTracks { Arguments = new Bundle() };
            instance.path = path;
            instance.folderName = folderName;
            return instance;
        }

        async void PopulateList()
        {
            if (await MainActivity.instance.GetReadPermission() == false)
            {
                MainActivity.instance.FindViewById(Resource.Id.loading).Visibility = ViewStates.Gone;
                EmptyView.Visibility = ViewStates.Visible;
                EmptyView.Text = GetString(Resource.String.no_permission);
                return;
            }

            LoaderManager.GetInstance(this).InitLoader(0, null, this);
        }

        public Android.Support.V4.Content.Loader OnCreateLoader(int id, Bundle args)
        {
            Uri musicUri = MediaStore.Audio.Media.ExternalContentUri;
            string selection;
            if (query != null)
            {
                selection = MediaStore.Audio.Media.InterfaceConsts.Data + " LIKE \"%" + path + "%\" AND (" + MediaStore.Audio.Media.InterfaceConsts.Title + " LIKE \"%" + query + "%\" OR " + MediaStore.Audio.Media.InterfaceConsts.Artist + " LIKE \"%" + query + "%\")";
                adapter.displayShuffle = false;
            }
            else
            {
                selection = MediaStore.Audio.Media.InterfaceConsts.Data + " LIKE \"%" + path + "%\"";
                adapter.displayShuffle = true;
            }

            return new CursorLoader(Android.App.Application.Context, musicUri, null, selection, null, null);
        }

        public void OnLoadFinished(Android.Support.V4.Content.Loader loader, Object data)
        {
            adapter.SwapCursor((ICursor)data);
        }

        public void OnLoaderReset(Android.Support.V4.Content.Loader loader)
        {
            adapter.SwapCursor(null);
        }

        private void OnRefresh(object sender, System.EventArgs e)
        {
            adapter.NotifyDataSetChanged();
        }

        public void Search(object sender, Android.Support.V7.Widget.SearchView.QueryTextChangeEventArgs e)
        {
            if (e.NewText == "")
                query = null;
            else
                query = e.NewText;

            LoaderManager.GetInstance(this).RestartLoader(0, null, this);
        }

        public void More(Song item, int position)
        {
            BottomSheetDialog bottomSheet = new BottomSheetDialog(MainActivity.instance);
            View bottomView = LayoutInflater.Inflate(Resource.Layout.BottomSheet, null);
            bottomView.FindViewById<TextView>(Resource.Id.bsTitle).Text = item.Title;
            bottomView.FindViewById<TextView>(Resource.Id.bsArtist).Text = item.Artist;
            if (item.Album == null)
            {
                var songCover = Uri.Parse("content://media/external/audio/albumart");
                var nextAlbumArtUri = ContentUris.WithAppendedId(songCover, item.AlbumArt);

                Picasso.With(MainActivity.instance).Load(nextAlbumArtUri).Placeholder(Resource.Drawable.noAlbum).Resize(400, 400).CenterCrop().Into(bottomView.FindViewById<ImageView>(Resource.Id.bsArt));
            }
            else
            {
                Picasso.With(MainActivity.instance).Load(item.Album).Placeholder(Resource.Drawable.noAlbum).Transform(new RemoveBlackBorder(true)).Into(bottomView.FindViewById<ImageView>(Resource.Id.bsArt));
            }
            bottomSheet.SetContentView(bottomView);

            bottomSheet.FindViewById<ListView>(Resource.Id.bsItems).Adapter = new BottomSheetAdapter(MainActivity.instance, Resource.Layout.BottomSheetText, new List<BottomSheetAction>
            {
                new BottomSheetAction(Resource.Drawable.Play, Resources.GetString(Resource.String.play), (sender, eventArg) => 
                {
                    LocalManager.PlayInOrder(path, position);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.PlaylistPlay, Resources.GetString(Resource.String.play_next), (sender, eventArg) => { SongManager.PlayNext(item); bottomSheet.Dismiss(); }),
                new BottomSheetAction(Resource.Drawable.Queue, Resources.GetString(Resource.String.play_last), (sender, eventArg) => { SongManager.PlayLast(item); bottomSheet.Dismiss(); }),
                new BottomSheetAction(Resource.Drawable.PlaylistAdd, Resources.GetString(Resource.String.add_to_playlist), (sender, eventArg) => { PlaylistManager.AddSongToPlaylistDialog(item); bottomSheet.Dismiss(); }),
                new BottomSheetAction(Resource.Drawable.Edit, Resources.GetString(Resource.String.edit_metadata), (sender, eventArg) => { LocalManager.EditMetadata(item); bottomSheet.Dismiss(); })
            });
            bottomSheet.Show();
        }

        public override void OnResume()
        {
            base.OnResume();
            instance = this;
        }

        public override void OnDestroyView()
        {
            MainActivity.instance.SupportActionBar.SetHomeButtonEnabled(false);
            MainActivity.instance.SupportActionBar.SetDisplayHomeAsUpEnabled(false);
            MainActivity.instance.SupportActionBar.SetDisplayShowTitleEnabled(false);
            MainActivity.instance.FindViewById(Resource.Id.toolbarLogo).Visibility = ViewStates.Visible;
            MainActivity.instance.HideSearch();
            base.OnDestroyView();
        }
    }
}