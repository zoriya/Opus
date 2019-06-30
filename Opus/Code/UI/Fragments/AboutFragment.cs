using Android.Content;
using Android.Net;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Preferences;
using Android.Views;

namespace Opus.Fragments
{
    [Register("Opus/Fragments/AboutFragment")]
    public class AboutFragment : PreferenceFragmentCompat
    {
        public static AboutFragment instance;
        public View view;

        public override void OnCreatePreferences(Bundle savedInstanceState, string rootKey)
        {
            instance = this;
            SetPreferencesFromResource(Resource.Layout.AboutPreferences, rootKey);

            //OpenSource click
            Preference openSource = PreferenceScreen.FindPreference("view_opensource");
            openSource.IconSpaceReserved = false;
            openSource.PreferenceClick += (s, e) =>
            {
                Preferences.instance.SupportFragmentManager.BeginTransaction().Replace(Android.Resource.Id.ListContainer, new OpenSourceViewer()).AddToBackStack(null).Commit();
            };

            //Website click
            Preference website = PreferenceScreen.FindPreference("website");
            website.IconSpaceReserved = false;
            website.PreferenceClick += (s, e) => 
            {
                Intent intent = new Intent(Intent.ActionView);
                intent.SetData(Uri.Parse("https://www.raccoon-sdg.fr"));
                StartActivity(intent);
            };

            //Github click
            Preference github = PreferenceScreen.FindPreference("github");
            github.IconSpaceReserved = false;
            github.PreferenceClick += (s, e) =>
            {
                Intent intent = new Intent(Intent.ActionView);
                intent.SetData(Uri.Parse("https://github.com/AnonymusRaccoon/Opus"));
                StartActivity(intent);
            };
        }

        public override void OnResume()
        {
            base.OnResume();
            instance = this;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            instance = null;
        }

        public override void OnStop()
        {
            base.OnStop();
            if(OpenSourceViewer.instance == null)
                Preferences.instance.SupportActionBar.Title = Preferences.instance.GetString(Resource.String.settings);
        }
    }
}