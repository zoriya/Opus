using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using Android.Support.V4.App;
using System;
using Square.Picasso;
using Android.Graphics;
using Android.Support.Design.Widget;
using MusicApp.Resources.Fragments;
using Android.Transitions;
using Android.Animation;
using Android.App.Job;
using System.Threading.Tasks;
using System.Threading;
using Android.Support.V7.App;

namespace MusicApp.Resources.Portable_Class
{
    public class Queue: ListFragment/*, ViewTreeObserver.IOnPreDrawListener*/
    {
        public static Queue instance;
        public Adapter adapter;
        public View emptyView;

        private View view;
        private bool isEmpty = false;
        //private float yFraction = 0;


        //public void SetYFraction(float fraction)
        //{
        //    yFraction = fraction;

        //    float translationY = View.Height * fraction;
        //    View.TranslationY = translationY;
        //}

        //public bool OnPreDraw()
        //{
        //    if(View.Height == 0)
        //        SetYFraction(yFraction);
        //    return true;
        //}

        //public float GetYFraction()
        //{
        //    return yFraction;
        //}

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            emptyView = LayoutInflater.Inflate(Resource.Layout.NoQueue, null);
            ListView.EmptyView = emptyView;

            PopulateView();
        }

        public override void OnDestroy()
        {
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
            view.SetPadding(0, 100, 0, 100);
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
                view.SetPadding(0, 100, 0, 0);
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