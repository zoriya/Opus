//using Android.Content;
//using Android.OS;
//using Android.Views;
//using Android.Widget;
//using Android.Support.V4.App;
//using System.Collections.Generic;
//using Android.Support.Design.Widget;
//using MusicApp.Resources.values;
//using Android.Support.V7.Preferences;
//using Android.Util;

//namespace MusicApp.Resources.Portable_Class
//{
//    public class DownloadList : ListFragment
//    {
//        public static DownloadList instance;

//        private View emptyView;
//        private bool isEmpty = true;
//        private List<Song> list = new List<Song>();


//        public override void OnActivityCreated(Bundle savedInstanceState)
//        {
//            base.OnActivityCreated(savedInstanceState);
//            emptyView = LayoutInflater.Inflate(Resource.Layout.DownloadLayout, null);
//            ListView.EmptyView = emptyView;
//            ListView.ItemClick += ListView_ItemClick;
//            ListView.ItemLongClick += ListView_ItemLongClick;
//            ListAdapter = null;
//            Activity.AddContentView(emptyView, View.LayoutParameters);
//        }

//        public override void OnDestroy()
//        {
//            if (isEmpty)
//            {
//                ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
//                rootView.RemoveView(emptyView);
//            }
//            base.OnDestroy();
//            instance = null;
//        }

//        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
//        {
//            View view = base.OnCreateView(inflater, container, savedInstanceState);
//            view.SetPadding(0, 100, 0, MainActivity.paddingBot);
//            return view;
//        }

//        public static Fragment NewInstance()
//        {
//            instance = new DownloadList { Arguments = new Bundle() };
//            return instance;
//        }

//        public void Search(string search)
//        {
//            if(search == null || search == "")
//            {
//                if(!isEmpty)
//                    Activity.AddContentView(emptyView, View.LayoutParameters);
//                return;
//            }
//            if (!isEmpty)
//            {
//                ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
//                rootView.RemoveView(emptyView);
//                isEmpty = false;
//            }

//            list.Clear();
//            var items = new VideoSearch();

//            foreach(var item in items.SearchQuery(search, 1))
//            {
//                new YTitemToSong(item, out Song song);
//                list.Add(song); 
//            }

//            ListAdapter = new Adapter(Android.App.Application.Context, Resource.Layout.SongList, list);
//        }

//        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
//        {

//        }

//        private void ListView_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
//        {
//            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Android.App.Application.Context);
//            if(prefManager.GetString("downloadPath", null) != null)
//            {
//                Toast.MakeText(Android.App.Application.Context, "Downloading...", ToastLength.Short).Show();
//                Context context = Android.App.Application.Context;
//                Intent intent = new Intent(context, typeof(Downloader));
//                intent.PutExtra("file", list[e.Position].GetPath());
//                intent.PutExtra("path", prefManager.GetString("downloadPath", null));
//                intent.PutExtra("name", list[e.Position].GetName());
//                context.StartService(intent);
//            }
//            else
//            {
//                Snackbar.Make(View, "Download Path Not Set.", Snackbar.LengthShort).SetAction("Set Path", (v) => 
//                {
//                    Intent intent = new Intent(Android.App.Application.Context, typeof(Preferences));
//                    StartActivity(intent);
//                }).Show();
//            }
//        }
//    }
//}