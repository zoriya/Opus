using Android.Support.V4.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android;
using Android.Content.PM;
using Android.Support.Design.Widget;
using Google.Apis.YouTube.v3;
using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Common.Apis;
using Android.Gms.Common;
using Android.Gms.Auth.Api;
using System.Collections.Generic;
using Google.Apis.Services;
using Android.Accounts;

namespace MusicApp.Resources.Portable_Class
{
    public class YoutubeEngine : ListFragment
    {
        public static YoutubeEngine instance;
        public List<YtFile> result;

        private View emptyView;
        private bool isEmpty = true;

        private string ApiKey = "AIzaSyBOQyZVnBAKjur0ztBuYPSopS725Qudgc4";
        private YouTubeService youtubeService;
        //private string oAuthKey;
        //private GoogleApiClient googleClient;
        //private const int signPickerID = 9001;
        //private const int playServiceID = 9002;
        //private const string accountName = "accountName";


        //private static readonly string[] permissions = new string[] 
        //{             
        //    Manifest.Permission.GetAccounts
        //};

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            emptyView = LayoutInflater.Inflate(Resource.Layout.DownloadLayout, null);
            ListView.EmptyView = emptyView;
            Activity.AddContentView(emptyView, View.LayoutParameters);
            ListAdapter = null;

            if (youtubeService == null)
            {
                youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    ApiKey = ApiKey,
                    ApplicationName = "MusicApp"
                });
            }
        }

        public override void OnDestroy()
        {
            if (isEmpty)
            {
                ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                rootView.RemoveView(emptyView);
            }
            base.OnDestroy();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = base.OnCreateView(inflater, container, savedInstanceState);
            view.SetPadding(0, 100, 0, MainActivity.paddingBot);
            return view;
        }

        public static Fragment NewInstance()
        {
            instance = new YoutubeEngine { Arguments = new Bundle() };
            return instance;
        }

        #region AccountChooser
        //void CheckInitializationState()
        //{
        //    if (!GooglePlayServiceAvailable())
        //        GetGooglePlayServices();
        //    else if (oAuthKey == null)
        //        ChooseAccount();
        //    else
        //        GetPermissions(permissions);
        //}

        //private bool GooglePlayServiceAvailable()
        //{
        //    GoogleApiAvailability availability = GoogleApiAvailability.Instance;
        //    int result = availability.IsGooglePlayServicesAvailable(Android.App.Application.Context);
        //    return result == ConnectionResult.Success;
        //}

        //private void GetGooglePlayServices()
        //{
        //    GoogleApiAvailability availability = GoogleApiAvailability.Instance;
        //    int result = availability.IsGooglePlayServicesAvailable(Android.App.Application.Context);
        //    if(availability.IsUserResolvableError(result))
        //        availability.GetErrorDialog(MainActivity.instance, result, playServiceID).Show();
        //}

        //void ChooseAccount()
        //{
        //    bool hasPermissions = CheckPermissions(permissions);
        //    if (hasPermissions)
        //    {
        //        string account = Activity.GetPreferences(FileCreationMode.Private).GetString(accountName, null);
        //        oAuthKey = Activity.GetPreferences(FileCreationMode.Private).GetString("oAuth", null);
        //        if (account != null)
        //        {
        //            CheckInitializationState();
        //        }
        //        else
        //        {
        //            GoogleSignInOptions gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn).RequestEmail().Build();
        //            googleClient = new GoogleApiClient.Builder(Android.App.Application.Context).EnableAutoManage(Activity, (result) => { }).AddApi(Auth.GOOGLE_SIGN_IN_API, gso).Build();
        //            Intent intent = Auth.GoogleSignInApi.GetSignInIntent(googleClient);
        //            StartActivityForResult(intent, signPickerID);
        //        }
        //    }
        //    else
        //    {
        //        GetPermissions(permissions);
        //    }
        //}

        //public override void OnActivityResult(int requestCode, int resultCode, Intent data)
        //{
        //    base.OnActivityResult(requestCode, resultCode, data);
        //    if(requestCode == signPickerID)
        //    {
        //        GoogleSignInResult result = Auth.GoogleSignInApi.GetSignInResultFromIntent(data);
        //        if (result.IsSuccess)
        //        {
        //            GoogleSignInAccount signInAccount = result.SignInAccount;
        //            Account account = signInAccount.Account;
        //            ISharedPreferences settings = Activity.GetPreferences(FileCreationMode.Private);
        //            ISharedPreferencesEditor editor = settings.Edit();
        //            editor.PutString(accountName, signInAccount.DisplayName);
        //            editor.Apply();
        //        }
        //    }
        //}
        #endregion

        #region Permissions
        void GetPermissions(string[] permissions)
        {
            bool hasPermissions = CheckPermissions(permissions);

            if (!hasPermissions)
                RequestPermissions(permissions, 0);
            //else
            //    PopulateList();
        }

        bool CheckPermissions(string[] permissions)
        {
            bool hasPermissions = true;
            foreach (string permission in permissions)
            {
                if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(Android.App.Application.Context, permission) != (int)Permission.Granted)
                {
                    hasPermissions = false;
                    break;
                }
            }
            return hasPermissions;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            switch (requestCode)
            {
                case 0:
                    {
                        if (grantResults[0] == Permission.Granted)
                        {
                            PopulateList();
                        }
                        else
                        {
                            Snackbar.Make(View, "Permission denied, can't list search on youtube.", Snackbar.LengthShort).Show();
                        }
                    }
                    break;
            }
        }
#endregion

        public async void Search(string search)
        {
            if (search == null || search == "")
                return;

            SearchResource.ListRequest searchResult = youtubeService.Search.List("snippet");
            searchResult.Fields = "items(id/videoId,snippet/title,snippet/thumbnails/default/url,snippet/channelTitle)";
            searchResult.Q = search;
            searchResult.Type = "video";
            searchResult.MaxResults = 20;

            var searchReponse = await searchResult.ExecuteAsync();

            result = new List<YtFile>();

            foreach(var video in searchReponse.Items)
            {
                YtFile videoInfo = new YtFile(video.Snippet.Title, video.Snippet.ChannelTitle, video.Id.VideoId, video.Snippet.Thumbnails.Default__.Url);
                result.Add(videoInfo);
            }

            ListAdapter = new YtAdapter(Android.App.Application.Context, Resource.Layout.YtList, result);
        }

        void PopulateList()
        {

        }
    }
}