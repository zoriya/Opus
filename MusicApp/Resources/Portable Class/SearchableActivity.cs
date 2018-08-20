using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using SearchView = Android.Support.V7.Widget.SearchView;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace MusicApp.Resources.Portable_Class
{
    [Activity(Label = "SearchableActivity", Theme = "@style/Theme")]
    public class SearchableActivity : AppCompatActivity, IMenuItemOnActionExpandListener
    {
        public static SearchableActivity instance;
        public SearchView searchView;
        public bool searched = false;
        private ListView ListView;
        private List<Suggestion> History = new List<Suggestion>();
        private List<Suggestion> suggestions = new List<Suggestion>();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            if (MainActivity.Theme == 1)
                SetTheme(Resource.Style.DarkTheme);

            SetContentView(Resource.Layout.SearchLayout);

            Toolbar ToolBar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(ToolBar);
            SupportActionBar.Title = "";
            ListView = FindViewById<ListView>(Resource.Id.searchSuggestions);
            instance = this;

            ListView.Divider = null;
            ListView.DividerHeight = 0;
            ListView.ItemClick += (sender, e) =>
            {
                searched = true;
                searchView.SetQuery(suggestions[e.Position].Text, true);
            };
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.search_toolbar, menu);
            IMenuItem searchItem = menu.FindItem(Resource.Id.search);
            searchItem.ExpandActionView();
            searchView = searchItem.ActionView.JavaCast<SearchView>();
            searchView.MaxWidth = int.MaxValue;
            searchView.QueryHint = "Search Youtube";
            searchView.QueryTextChange += (s, e) =>
            {
                if (e.NewText.Length > 0)
                {
                    new Thread(() =>
                    {
                        try
                        {
                            using (WebClient client = new WebClient())
                            {
                                string json = client.DownloadString("http://suggestqueries.google.com/complete/search?client=youtube&ds=yt&client=firefox&q=" + e.NewText);
                                json = json.Substring(4 + e.NewText.Length);
                                json = json.Remove(json.Length - 1);
                                List<string> items = JsonConvert.DeserializeObject<List<string>>(json);
                                suggestions = items.ConvertAll(StringToSugest);

                                if(!searched)
                                    RunOnUiThread(new Java.Lang.Runnable(() => { ListView.Adapter = new SuggestionAdapter(instance, Resource.Layout.SuggestionLayout, suggestions); }));
                            }
                        }
                        catch { }
                    }).Start();
                }
            };
            searchView.QueryTextSubmit += (s, e) =>
            {
                searched = true;
                Finish();
                OverridePendingTransition(Android.Resource.Animation.FadeIn, Android.Resource.Animation.FadeOut);
                MainActivity.instance.SearchOnYoutube(e.Query);
                e.Handled = true;
            };
            searchItem.SetOnActionExpandListener(this);
            searchView.SetQuery(((SearchView)MainActivity.instance.menu.FindItem(Resource.Id.search).ActionView).Query, false);
            return base.OnCreateOptionsMenu(menu);
        }

        Suggestion StringToSugest(string text)
        {
            return new Suggestion(Android.Resource.Drawable.IcSearchCategoryDefault, text);
        }

        public void Refine(int position)
        {
            searchView.SetQuery(suggestions[position].Text, false);
        }

        protected override void OnStop()
        {
            base.OnStop();
            if(!searched)
                MainActivity.instance.CancelSearch();
        }

        public bool OnMenuItemActionCollapse(IMenuItem item)
        {
            Finish();
            OverridePendingTransition(Android.Resource.Animation.FadeIn, Android.Resource.Animation.FadeOut);
            return true;
        }

        public bool OnMenuItemActionExpand(IMenuItem item) { return true; }
    }
}