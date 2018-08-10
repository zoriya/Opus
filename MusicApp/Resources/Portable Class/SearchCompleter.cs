using Android.App;
using Android.Database;
using Android.OS;
using Android.Provider;
using Android.Support.V7.Widget;
using Java.Lang;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;

namespace MusicApp.Resources.Portable_Class
{
    public class SearchCompleter : AsyncTask<string, Void, ICursor>
    {
        private SearchView searchView;

        private static readonly string[] AutoCompleteNames = new string[] 
        { 
            BaseColumns.Id,
            SearchManager.SuggestColumnText1
        };

        public SearchCompleter(SearchView searchView)
        {
            this.searchView = searchView;
        }

        protected override ICursor RunInBackground(string[] query)
        {
            MatrixCursor cursor = new MatrixCursor(AutoCompleteNames);
            try
            {
                using (WebClient client = new WebClient())
                {
                    string json = client.DownloadString("http://suggestqueries.google.com/complete/search?client=youtube&ds=yt&client=firefox&q=" + query[0]);
                    json = json.Substring(4 + query[0].Length);
                    json = json.Remove(json.Length - 1);
                    List<string> items = JsonConvert.DeserializeObject<List<string>>(json);

                    for (int i = 0; i < items.Count; i++)
                        cursor.AddRow(new Object[] { i, items[i] });

                }
            }
            catch { }
            return cursor;
        }

        protected override void OnPostExecute(ICursor result)
        {
            base.OnPostExecute(result);
            searchView.SuggestionsAdapter.ChangeCursor(result);
        }
    }
}