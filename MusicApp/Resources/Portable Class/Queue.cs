using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using Android.Support.V4.App;
using Square.Picasso;
using System;

namespace MusicApp.Resources.Portable_Class
{
    public class Queue: ListFragment
    {
        public static Queue instance;
        public Adapter adapter;
        public View emptyView;

        private View view;
        private bool isEmpty = false;

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            emptyView = LayoutInflater.Inflate(Resource.Layout.NoQueue, null);
            ListView.EmptyView = emptyView;

            PopulateView();

            if (MusicPlayer.isRunning)
            {
                Song current = MusicPlayer.queue[MusicPlayer.CurrentID()];

                RelativeLayout smallPlayer = Activity.FindViewById<RelativeLayout>(Resource.Id.smallPlayer);
                FrameLayout parent = (FrameLayout) smallPlayer.Parent;
                parent.Visibility = ViewStates.Visible;
                smallPlayer.Visibility = ViewStates.Visible;
                smallPlayer.FindViewById<TextView>(Resource.Id.spTitle).Text = current.GetName();
                smallPlayer.FindViewById<TextView>(Resource.Id.spArtist).Text = current.GetArtist();
                ImageView art = smallPlayer.FindViewById<ImageView>(Resource.Id.spArt);

                var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                var songAlbumArtUri = ContentUris.WithAppendedId(songCover, current.GetAlbumArt());

                Picasso.With(Android.App.Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Into(art);

                smallPlayer.FindViewById<ImageButton>(Resource.Id.spLast).Click += Last_Click;
                smallPlayer.FindViewById<ImageButton>(Resource.Id.spPlay).Click += Play_Click;
                smallPlayer.FindViewById<ImageButton>(Resource.Id.spNext).Click += Next_Click;
            }
        }

        private void Last_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("Previus");
            Activity.StartService(intent);
        }

        private void Play_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("Pause");
            Activity.StartService(intent);
        }

        private void Next_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("Next");
            Activity.StartService(intent);
        }

        public override void OnDestroy()
        {
            RelativeLayout smallPlayer = Activity.FindViewById<RelativeLayout>(Resource.Id.smallPlayer);
            FrameLayout parent = (FrameLayout)smallPlayer.Parent;
            parent.Visibility = ViewStates.Gone;
            smallPlayer.FindViewById<ImageButton>(Resource.Id.spLast).Click -= Last_Click;
            smallPlayer.FindViewById<ImageButton>(Resource.Id.spPlay).Click -= Play_Click;
            smallPlayer.FindViewById<ImageButton>(Resource.Id.spNext).Click -= Next_Click;

            if (isEmpty)
            {
                ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                rootView.RemoveView(emptyView);
            }
            base.OnDestroy();
            instance = null;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = base.OnCreateView(inflater, container, savedInstanceState);
            this.view = view;
            view.SetPadding(0, 100, 0, MainActivity.paddingBot);
            return view;
        }

        public static Fragment NewInstance()
        {
            instance = new Queue { Arguments = new Bundle() };
            return instance;
        }

        void PopulateView()
        {
            adapter = new Adapter(Android.App.Application.Context, Resource.Layout.SongList, MusicPlayer.queue);

            ListAdapter = adapter;
            ListView.TextFilterEnabled = true;
            ListView.ItemClick += ListView_ItemClick;


            if (adapter == null || adapter.Count == 0)
            {
                isEmpty = true;
                Activity.AddContentView(emptyView, View.LayoutParameters);
            }
        }

        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            Song item = MusicPlayer.queue[e.Position];

            Context context = Android.App.Application.Context;
            Intent intent = new Intent(context, typeof(MusicPlayer));
            intent.PutExtra("file", item.GetPath());
            intent.SetAction("QueueSwitch");
            context.StartService(intent);
        }
    }
}