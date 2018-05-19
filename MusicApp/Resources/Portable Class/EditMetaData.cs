using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Preferences;
using Android.Provider;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using Square.Picasso;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using TagLib;
using YoutubeExplode;
using YoutubeExplode.Models;

namespace MusicApp.Resources.Portable_Class
{
    [Activity(Label = "EditMetaData", Theme = "@style/Theme", WindowSoftInputMode = SoftInput.AdjustResize|SoftInput.StateHidden)]
    public class EditMetaData : AppCompatActivity
    {
        public static EditMetaData instance;
        public Song song;

        private TextView title, artist, album, youtubeID;
        private ImageView albumArt;
        private Android.Net.Uri artURI;
        private bool tempFile = false;
        private bool hasPermission = false;
        private const int RequestCode = 8539;
        private const int PickerRequestCode = 9852;
        private readonly string[] actions = new string[] { "Pick an album art from storage", "Download album art on youtube" };

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if(MainActivity.Theme == 1)
                SetTheme(Resource.Style.DarkTheme);

            SetContentView(Resource.Layout.EditMetaData);

            instance = this;
            song = (Song) Intent.GetStringExtra("Song");

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.backToolbar);
            DisplayMetrics metrics = new DisplayMetrics();
            WindowManager.DefaultDisplay.GetMetrics(metrics);
            ((View)toolbar.Parent.Parent).LayoutParameters.Height = metrics.WidthPixels;
            toolbar.Parent.RequestLayout();
            toolbar.LayoutParameters.Height = metrics.WidthPixels / 3;
            toolbar.RequestLayout();

            if (MainActivity.Theme == 1)
            {
                toolbar.PopupTheme = Resource.Style.DarkPopup;
            }

            SetSupportActionBar(toolbar);
            SupportActionBar.SetDisplayShowTitleEnabled(false);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            title = FindViewById<TextView>(Resource.Id.metadataTitle);
            artist = FindViewById<TextView>(Resource.Id.metadataArtist);
            album = FindViewById<TextView>(Resource.Id.metadataAlbum);
            youtubeID = FindViewById<TextView>(Resource.Id.metadataYID);
            albumArt = FindViewById<ImageView>(Resource.Id.metadataArt);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.metadataFAB);
            fab.Click += async (sender, e) => { await ValidateChanges(); };

            title.Text = song.GetName();
            artist.Text = song.GetArtist();
            album.Text = song.GetAlbum();
            youtubeID.Text = song.youtubeID;
            albumArt.Click += AlbumArt_Click;

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

        private void AlbumArt_Click(object sender, System.EventArgs e)
        {
            new Android.Support.V7.App.AlertDialog.Builder(this, MainActivity.dialogTheme)
                .SetTitle("Change Album Art")
                .SetItems(actions, (senderAlert, args) =>  
                {
                    switch(args.Which)
                    {
                        case 0:
                            PickAnAlbumArtLocally();
                            break;
                        case 1:
                            DownloadAlbumArtOnYT();
                            break;
                        default:
                            break;
                    }
                    
                }).Show();
        }

        void PickAnAlbumArtLocally()
        {
            Intent intent = new Intent(Intent.ActionPick, MediaStore.Images.Media.ExternalContentUri);
            StartActivityForResult(intent, PickerRequestCode);
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if(requestCode == PickerRequestCode)
            {
                if(resultCode == Result.Ok)
                {
                    Android.Net.Uri uri = data.Data;
                    Picasso.With(Application.Context).Load(uri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(albumArt);
                    artURI = uri;
                }
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.metaData_items, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Android.Resource.Id.Home)
            {
                LeaveAndValidate();
                return true;
            }
            if (item.ItemId == Resource.Id.downloadMDfromYT)
            {
                DownloadMetaDataFromYT();
                return true;
            }
            if(item.ItemId == Resource.Id.undoChange)
            {
                UndoChange();
                return true;
            }
            return false;
        }

        async void LeaveAndValidate()
        {
            await ValidateChanges();
            Finish();
        }

        async Task ValidateChanges()
        {
            if (song.GetName() == title.Text && song.GetArtist() == artist.Text && song.youtubeID == youtubeID.Text && song.GetAlbum() == album.Text && artURI == null)
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

            Stream stream = new FileStream(song.GetPath(), FileMode.Open, FileAccess.ReadWrite);
            var meta = TagLib.File.Create(new StreamFileAbstraction(song.GetPath(), stream, stream));

            meta.Tag.Title = title.Text;
            meta.Tag.Performers = new string[] { artist.Text };
            meta.Tag.Album = album.Text;
            meta.Tag.Comment = youtubeID.Text;

            if (artURI != null)
            {
                IPicture[] pictures = new IPicture[1];

                Android.Graphics.Bitmap bitmap = MediaStore.Images.Media.GetBitmap(ContentResolver, artURI);
                MemoryStream memoryStream = new MemoryStream();
                bitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Jpeg, 100, memoryStream);
                byte[] data = memoryStream.ToArray();

                pictures[0] = new Picture(data);
                meta.Tag.Pictures = pictures;

                if(!tempFile)
                    artURI = null;

                ContentResolver.Delete(ContentUris.WithAppendedId(Android.Net.Uri.Parse("content://media/external/audio/albumart"), song.GetAlbumArt()), null, null);
            }

            meta.Save();
            stream.Dispose();

            if (tempFile)
            {
                tempFile = false;
                System.IO.File.Delete(artURI.Path);
                artURI = null;
            }

            await Task.Delay(10);
            Android.Media.MediaScannerConnection.ScanFile(this, new string[] { song.GetPath() }, null, null);

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

        async void DownloadMetaDataFromYT()
        {
            if (song.youtubeID == "")
            {
                Toast.MakeText(this, "Can't get meta data on youtube, youtubeID isn't set.", ToastLength.Short).Show();
                return;
            }

            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(this);
            if (prefManager.GetString("downloadPath", null) == null)
            {
                Toast.MakeText(this, "Download path isn't set, can't download informations.", ToastLength.Short).Show();
                return;
            }

            const string permission = Manifest.Permission.WriteExternalStorage;
            if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(this, permission) != (int)Permission.Granted)
            {
                string[] permissions = new string[] { permission };
                RequestPermissions(permissions, 2659);

                await Task.Delay(1000);
                while (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(this, permission) != (int)Permission.Granted)
                    await Task.Delay(500);
            }

            YoutubeClient client = new YoutubeClient();
            Video video = await client.GetVideoAsync(youtubeID.Text);
            title.Text = video.Title;
            artist.Text = video.Author;

            if (tempFile)
            {
                tempFile = false;
                System.IO.File.Delete(artURI.ToString());
            }

            WebClient webClient = new WebClient();
            webClient.DownloadDataCompleted += (sender, e) =>
            {
                string tempArt = Path.Combine(prefManager.GetString("downloadPath", ""), "albumArt" + Path.GetExtension(video.Thumbnails.HighResUrl));
                System.Console.WriteLine("&Temp path: " + tempArt + "Url: " + video.Thumbnails.HighResUrl);
                System.IO.File.WriteAllBytes(tempArt, e.Result);

                Android.Net.Uri uri = Android.Net.Uri.FromFile(new Java.IO.File(tempArt));
                Picasso.With(this).Load(uri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(albumArt);
                artURI = uri;
                tempFile = true;
            };
            webClient.DownloadDataAsync(new System.Uri(video.Thumbnails.HighResUrl));
        }

        async void DownloadAlbumArtOnYT()
        {
            if (song.youtubeID == "")
            {
                Toast.MakeText(this, "Can't get meta data on youtube, youtubeID isn't set.", ToastLength.Short).Show();
                return;
            }

            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(this);
            if (prefManager.GetString("downloadPath", null) == null)
            {
                Toast.MakeText(this, "Download path isn't set, can't download informations.", ToastLength.Short).Show();
                return;
            }

            const string permission = Manifest.Permission.WriteExternalStorage;
            if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(this, permission) != (int)Permission.Granted)
            {
                string[] permissions = new string[] { permission };
                RequestPermissions(permissions, 2659);

                await Task.Delay(1000);
                while (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(this, permission) != (int)Permission.Granted)
                    await Task.Delay(500);
            }

            YoutubeClient client = new YoutubeClient();
            Video video = await client.GetVideoAsync(youtubeID.Text);

            if (tempFile)
            {
                tempFile = false;
                System.IO.File.Delete(artURI.ToString());
            }

            WebClient webClient = new WebClient();
            webClient.DownloadDataCompleted += (sender, e) =>
            {
                string tempArt = Path.Combine(prefManager.GetString("downloadPath", ""), "albumArt" + Path.GetExtension(video.Thumbnails.HighResUrl));
                System.Console.WriteLine("&Temp path: " + tempArt + "Url: " + video.Thumbnails.HighResUrl);
                System.IO.File.WriteAllBytes(tempArt, e.Result);

                Android.Net.Uri uri = Android.Net.Uri.FromFile(new Java.IO.File(tempArt));
                Picasso.With(this).Load(uri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(albumArt);
                artURI = uri;
                tempFile = true;
            };
            webClient.DownloadDataAsync(new System.Uri(video.Thumbnails.HighResUrl));
        }

        void UndoChange()
        {
            title.Text = song.GetName();
            artist.Text = song.GetArtist();
            album.Text = song.GetAlbum();
            youtubeID.Text = song.youtubeID;
            albumArt.Click += AlbumArt_Click;

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

            albumArt = null;
            tempFile = false;
        }
    }
}