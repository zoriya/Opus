using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using Opus.Api;
using Opus.Api.Services;
using Opus.DataStructure;
using Opus.Others;
using Square.Picasso;
using System.IO;
using System.Threading.Tasks;
using TagLib;
using YoutubeExplode;
using YoutubeExplode.Models;
using Picture = TagLib.Picture;

namespace Opus.Fragments
{
    [Activity(Label = "EditMetaData", Theme = "@style/Theme", WindowSoftInputMode = SoftInput.AdjustResize|SoftInput.StateHidden)]
    public class EditMetaData : AppCompatActivity
    {
        public static EditMetaData instance;
        public Song song;
        private int queuePosition;

        private TextView title, artist, album, youtubeID;
        private ImageView albumArt;
        private Android.Net.Uri artURI;
        private string ytThumbUri;
        private bool tempFile = false;
        private bool hasPermission = false;
        private const int RequestCode = 8539;
        private const int PickerRequestCode = 9852;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            MainActivity.LoadTheme(this);
            SetContentView(Resource.Layout.EditMetaData);
            Window.SetStatusBarColor(Color.Argb(70, 00, 00, 00));

            instance = this;
            song = (Song)Intent.GetStringExtra("Song");
            queuePosition = Intent.GetIntExtra("Position", -1);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.backToolbar);
            DisplayMetrics metrics = new DisplayMetrics();
            WindowManager.DefaultDisplay.GetMetrics(metrics);
            ((View)toolbar.Parent.Parent).LayoutParameters.Height = metrics.WidthPixels;
            toolbar.Parent.RequestLayout();
            toolbar.LayoutParameters.Height = metrics.WidthPixels / 3;
            toolbar.RequestLayout();
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

            title.Text = song.Title;
            artist.Text = song.Artist;
            album.Text = song.Album;
            youtubeID.Text = song.YoutubeID;
            albumArt.Click += AlbumArt_Click;

            var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
            var songAlbumArtUri = ContentUris.WithAppendedId(songCover, song.AlbumArt);

            Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.noAlbum).Into(albumArt);
        }

        private void AlbumArt_Click(object sender, System.EventArgs e)
        {
            new Android.Support.V7.App.AlertDialog.Builder(this, MainActivity.dialogTheme)
                .SetTitle(Resource.String.change_albumart)
                .SetItems(new string[] { GetString(Resource.String.pick_album_local), GetString(Resource.String.download_albumart) }, (senderAlert, args) =>  
                {
                    switch(args.Which)
                    {
                        case 0:
                            PickAnAlbumArtLocally();
                            break;
                        case 1:
                            DownloadMetaDataFromYT(true);
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
                    Picasso.With(Application.Context).Load(uri).Placeholder(Resource.Drawable.noAlbum).Into(albumArt);
                    if (tempFile)
                    {
                        tempFile = false;
                        System.IO.File.Delete(artURI.Path);
                    }
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
                DownloadMetaDataFromYT(false);
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
            if(await ValidateChanges(true))
                Finish();

            Player.instance?.RefreshPlayer();
        }

        async Task<bool> ValidateChanges(bool displayActionIfMountFail = false)
        {
            System.Console.WriteLine("&Validaing changes");
            if (song.Title == title.Text && song.Artist == artist.Text && song.YoutubeID == youtubeID.Text && song.Album == album.Text && artURI == null)
                return true;

            System.Console.WriteLine("&Requesting permission");
            const string permission = Manifest.Permission.WriteExternalStorage;
            hasPermission = Android.Support.V4.Content.ContextCompat.CheckSelfPermission(this, permission) == (int)Permission.Granted;
            if (!hasPermission)
            {
                string[] permissions = new string[] { permission };
                RequestPermissions(permissions, RequestCode);

                while (!hasPermission)
                    await Task.Delay(1000);
            }

            if(!Environment.MediaMounted.Equals(Environment.GetExternalStorageState(new Java.IO.File(song.Path))))
            {
                Snackbar snackBar = Snackbar.Make(FindViewById<CoordinatorLayout>(Resource.Id.snackBar), Resource.String.mount_error, Snackbar.LengthLong);
                snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
                if(displayActionIfMountFail)
                {
                    snackBar.SetAction(Resource.String.mount_error_action, (v) =>
                    {
                        Finish();
                    });
                }
                snackBar.Show();
                return false;
            }

            try
            {
                System.Console.WriteLine("&Creating write stream");
                Stream stream = new FileStream(song.Path, FileMode.Open, FileAccess.ReadWrite);
                var meta = TagLib.File.Create(new StreamFileAbstraction(song.Path, stream, stream));

                System.Console.WriteLine("&Writing tags");
                meta.Tag.Title = title.Text;
                song.Title = title.Text;
                meta.Tag.Performers = new string[] { artist.Text };
                song.Artist = artist.Text;
                meta.Tag.Album = album.Text;
                song.Album = album.Text;
                meta.Tag.Comment = youtubeID.Text;
                if (queuePosition != -1 && MusicPlayer.queue.Count > queuePosition)
                {
                    MusicPlayer.queue[queuePosition] = song;
                    Player.instance?.RefreshPlayer();
                    Queue.instance.NotifyItemChanged(queuePosition);
                }

                if (ytThumbUri != null)
                {
                    System.Console.WriteLine("&Writing YT Thumb");
                    await Task.Run(() => 
                    { 
                        IPicture[] pictures = new IPicture[1];
                        Bitmap bitmap = Picasso.With(Application.Context).Load(ytThumbUri).Transform(new RemoveBlackBorder(true)).Get();
                        byte[] data;
                        using (var MemoryStream = new MemoryStream())
                        {
                            bitmap.Compress(Bitmap.CompressFormat.Png, 0, MemoryStream);
                            data = MemoryStream.ToArray();
                        }
                        bitmap.Recycle();
                        pictures[0] = new Picture(data);
                        meta.Tag.Pictures = pictures;

                        ytThumbUri = null;
                    });
                }
                else if (artURI != null)
                {
                    System.Console.WriteLine("&Writing ArtURI");
                    IPicture[] pictures = new IPicture[1];

                    Bitmap bitmap = null;
                    if (tempFile)
                    {
                        await Task.Run(() => 
                        {
                            bitmap = Picasso.With(this).Load(artURI).Transform(new RemoveBlackBorder(true)).Get();
                        });
                    }
                    else
                    {
                        await Task.Run(() =>
                        {
                            bitmap = Picasso.With(this).Load(artURI).Get();
                        });
                    }

                    MemoryStream memoryStream = new MemoryStream();
                    bitmap.Compress(Bitmap.CompressFormat.Jpeg, 100, memoryStream);
                    byte[] data = memoryStream.ToArray();
                    pictures[0] = new Picture(data);
                    meta.Tag.Pictures = pictures;

                    if(!tempFile)
                        artURI = null;

                    ContentResolver.Delete(ContentUris.WithAppendedId(Android.Net.Uri.Parse("content://media/external/audio/albumart"), song.AlbumArt), null, null);
                }

                System.Console.WriteLine("&Saving");
                meta.Save();
                stream.Dispose();
            }
            catch(System.Exception e)
            {
                Toast.MakeText(this, Resource.String.format_unsupported, ToastLength.Long).Show();
                System.Console.WriteLine("&EditMetadata Validate exception: (probably due to an unsupported format) - " + e.Message);
            }

            System.Console.WriteLine("&Deleting temp file");
            if (tempFile)
            {
                tempFile = false;
                System.IO.File.Delete(artURI.Path);
                artURI = null;
            }

            System.Console.WriteLine("&Scanning file");
            await Task.Delay(10);
            Android.Media.MediaScannerConnection.ScanFile(this, new string[] { song.Path }, null, null);

            Toast.MakeText(this, Resource.String.changes_saved, ToastLength.Short).Show();
            return true;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode == RequestCode)
            {
                if (grantResults != null && grantResults.Length > 0)
                {
                    if (grantResults[0] == Permission.Granted)
                        hasPermission = true;
                    else
                        Snackbar.Make(FindViewById<View>(Resource.Id.contentView), Resource.String.no_permission, Snackbar.LengthShort).Show();
                }
                else
                {
                    hasPermission = false;
                    Snackbar.Make(FindViewById<View>(Resource.Id.contentView), Resource.String.no_permission, Snackbar.LengthShort).Show();
                }
            }
        }

        async void DownloadMetaDataFromYT(bool onlyArt)
        {
            if (song.YoutubeID == "")
            {
                Toast.MakeText(this, Resource.String.metdata_error_noid, ToastLength.Short).Show();
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
            if (!onlyArt)
            {
                title.Text = video.Title;
                artist.Text = video.Author;
                album.Text = video.Title + " - " + video.Author;
            }

            ytThumbUri = await YoutubeManager.GetBestThumb(video.Thumbnails);
            Picasso.With(this).Load(ytThumbUri).Placeholder(Resource.Drawable.noAlbum).Transform(new RemoveBlackBorder(true)).MemoryPolicy(MemoryPolicy.NoCache, MemoryPolicy.NoStore).Into(albumArt);
        }

        void UndoChange()
        {
            title.Text = song.Title;
            artist.Text = song.Artist;
            album.Text = song.Album;
            youtubeID.Text = song.YoutubeID;

            var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
            var songAlbumArtUri = ContentUris.WithAppendedId(songCover, song.AlbumArt);

            Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.noAlbum).Into(albumArt);

            artURI = null;
            ytThumbUri = null;
            tempFile = false;
        }

        protected override void OnResume()
        {
            base.OnResume();
            instance = this;
        }
    }
}