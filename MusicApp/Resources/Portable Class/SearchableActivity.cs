using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using Newtonsoft.Json;
using SQLite;
using System.Collections.Generic;
using System.Linq;
using System.Net;
//using System.Threading;
using System.Threading.Tasks;
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

            Task.Run(() => 
            {
                SQLiteConnection db = new SQLiteConnection(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "RecentSearch.sqlite"));
                db.CreateTable<Suggestion>();

                History = db.Table<Suggestion>().ToList().ConvertAll(HistoryItem);
                History.Reverse();
                suggestions = History;
            });
        }

        Suggestion HistoryItem(Suggestion suggestion)
        {
            return new Suggestion(Resource.Drawable.History, suggestion.Text);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.search_toolbar, menu);
            IMenuItem searchItem = menu.FindItem(Resource.Id.search);
            searchItem.ExpandActionView();
            searchView = searchItem.ActionView.JavaCast<SearchView>();
            ListView.Adapter = ListView.Adapter = new SuggestionAdapter(instance, Resource.Layout.SuggestionLayout, suggestions);
            searchView.MaxWidth = int.MaxValue;
            searchView.QueryHint = "Search Youtube";
            searchView.QueryTextChange += (s, e) =>
            {
                if (e.NewText.Length > 0)
                {
                    Task.Run(() =>
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
                                suggestions.InsertRange(0, History.Where(x => x.Text.StartsWith(e.NewText)));

                                if(!searched)
                                    RunOnUiThread(new Java.Lang.Runnable(() => { ListView.Adapter = new SuggestionAdapter(instance, Resource.Layout.SuggestionLayout, suggestions); }));
                            }
                        }
                        catch { }
                    });
                }
                else
                {
                    suggestions = History;
                    ListView.Adapter = new SuggestionAdapter(instance, Resource.Layout.SuggestionLayout, suggestions);
                }
            };
            searchView.QueryTextSubmit += (s, e) =>
            {
                searched = true;
                AddQueryToHistory(e.Query);
                Finish();
                OverridePendingTransition(Android.Resource.Animation.FadeIn, Android.Resource.Animation.FadeOut);
                MainActivity.instance.SearchOnYoutube(e.Query);
                e.Handled = true;
            };
            searchItem.SetOnActionExpandListener(this);
            searchView.SetQuery(((SearchView)MainActivity.instance.menu.FindItem(Resource.Id.search).ActionView).Query, false);
            return base.OnCreateOptionsMenu(menu);
        }

        void AddQueryToHistory(string query)
        {
            if (History.ConvertAll(SuggestToQuery).Contains(query, new QueryComparer()))
                return;

            Task.Run(() =>
            {
                SQLiteConnection db = new SQLiteConnection(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "RecentSearch.sqlite"));
                db.CreateTable<Suggestion>();

                db.Insert(new Suggestion(-1, query));
            });
        }

        Suggestion StringToSugest(string text)
        {
            return new Suggestion(Android.Resource.Drawable.IcSearchCategoryDefault, text);
        }

        string SuggestToQuery(Suggestion suggestion)
        {
            return suggestion.Text;
        }

        public void Refine(int position)
        {
            searchView.SetQuery(suggestions[position].Text, false);
        }

        protected override void OnStop()
        {
            base.OnStop();
            if(!searched && YoutubeEngine.instances == null)
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

    public class QueryComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            return x.Trim().ToLower().Equals(y.Trim().ToLower());
        }

        public int GetHashCode(string obj)
        {
            return obj.GetHashCode();
        }
    }
}