using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using Square.Picasso;
using System;
using System.Threading.Tasks;

namespace MusicApp.Resources.Portable_Class
{
    [Activity(Label = "EditMetaData", Theme = "@style/Theme")]
    public class EditMetaData : AppCompatActivity
    {
        public static EditMetaData instance;
        public Song song;

        private TextView title, artist, album, youtubeID;
        private bool hasPermission = false;
        private const int RequestCode = 8539;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.EditMetaData);
            instance = this;
            song = (Song) Intent.GetStringExtra("Song");

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.backToolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.SetDisplayShowTitleEnabled(false);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.Elevation = 0;

            title = FindViewById<TextView>(Resource.Id.metadataTitle);
            artist = FindViewById<TextView>(Resource.Id.metadataArtist);
            album = FindViewById<TextView>(Resource.Id.metadataAlbum);
            youtubeID = FindViewById<TextView>(Resource.Id.metadataYID);
            ImageView albumArt = FindViewById<ImageView>(Resource.Id.metadataArt);
            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.metadataFAB);
            fab.Click += async (sender, e) => { await ValidateChanges(); };

            title.Text = song.GetName();
            artist.Text = song.GetArtist();
            album.Text = song.GetAlbum();
            youtubeID.Text = song.youtubeID;

            if (song.GetAlbumArt() == -1 || song.IsYt)
            {
                var songAlbumArtUri = Android.Net.Uri.Parse(song.GetAlbum());
                Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(albumArt);
            }
            else
            {
                var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                var songAlbumArtUri = ContentUris.WithAppendedId(songCover, song.GetAlbumArt());

                Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(albumArt);
            }
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Android.Resource.Id.Home)
            {
                LeaveAndValidate();
                return true;
            }
            return false;
        }

        async void LeaveAndValidate()
        {
            await ValidateChanges();
            Intent intent = new Intent(this, typeof(MainActivity));
            StartActivity(intent);
        }

        async Task ValidateChanges()
        {
            if (song.GetName() == title.Text && song.GetArtist() == artist.Text && song.youtubeID == youtubeID.Text && song.GetAlbum() == album.Text)
                return;

            const string permission = Manifest.Permission.WriteExternalStorage;
            hasPermission = Android.Support.V4.Content.ContextCompat.CheckSelfPermission(this, permission) == (int)Permission.Granted;
            if (!hasPermission)
            {
                string[] permissions = new string[] { permission };
                RequestPermissions(permissions, RequestCode);

                while (!hasPermission)
                    await Task.Delay(1000);
            }

            Android.Net.Uri itemURI = ContentUris.WithAppendedId(MediaStore.Audio.Media.ExternalContentUri, song.GetID());
            ContentResolver.Delete(itemURI, null, null);
            await Task.Delay(10);
            ContentValues value = new ContentValues();
            value.Put(MediaStore.Audio.Media.InterfaceConsts.Title, title.Text);
            value.Put(MediaStore.Audio.Media.InterfaceConsts.Artist, artist.Text);
            value.Put(MediaStore.Audio.Media.InterfaceConsts.Album, album.Text);
            value.Put(MediaStore.Audio.Media.InterfaceConsts.Data, song.GetPath());
            //value.Put(MediaStore.Audio.Media.InterfaceConsts.AlbumArt, song.GetAlbumArt());
            value.Put(MediaStore.Audio.Media.InterfaceConsts.IsMusic, true);
            Android.Net.Uri uri = ContentResolver.Insert(MediaStore.Audio.Media.ExternalContentUri, value);
            SendBroadcast(new Intent(Intent.ActionMediaScannerScanFile, Android.Net.Uri.Parse("file://" + uri)));
            Toast.MakeText(this, "Changes saved.", ToastLength.Short).Show();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode == RequestCode)
            {
                if (grantResults.Length > 0)
                {
                    if (grantResults[0] == Permission.Granted)
                        hasPermission = true;
                    else
                        Snackbar.Make(FindViewById<View>(Resource.Id.contentView), "Permission denied, can't edit metadata.", Snackbar.LengthShort).Show();
                }
            }
        }
    }
}