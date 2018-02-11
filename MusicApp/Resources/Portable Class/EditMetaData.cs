using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using System.IO;
using MusicApp.Resources.values;
using Square.Picasso;
using System.Threading.Tasks;

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
        private bool hasPermission = false;
        private const int RequestCode = 8539;
        private const int PickerRequestCode = 9852;
        private string[] actions = new string[] { "Pick an album art from storage", "Search for an album art on google" };

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

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
            new Android.Support.V7.App.AlertDialog.Builder(this)
                .SetTitle("Change Album Art")
                .SetItems(actions, (senderAlert, args) =>  
                {
                    switch(args.Which)
                    {
                        case 0:
                            PickAnAlbumArtLocally();
                            break;
                        case 1:
                            //Pick from google
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

            //Android.Net.Uri itemURI = ContentUris.WithAppendedId(MediaStore.Audio.Media.ExternalContentUri, song.GetID());
            //ContentResolver.Delete(itemURI, null, null);
            //await Task.Delay(10);
            //if(song.GetName() != title.Text || song.GetArtist() != artist.Text || song.youtubeID != youtubeID.Text || song.GetAlbum() != album.Text)
            //{
            //    ContentValues value = new ContentValues();
            //    value.Put(MediaStore.Audio.Media.InterfaceConsts.Title, title.Text);
            //    value.Put(MediaStore.Audio.Media.InterfaceConsts.Artist, artist.Text);
            //    value.Put(MediaStore.Audio.Media.InterfaceConsts.Album, album.Text);
            //    value.Put(MediaStore.Audio.Media.InterfaceConsts.Data, song.GetPath());
            //    value.Put(MediaStore.Audio.Media.InterfaceConsts.Composer, song.youtubeID);
            //    value.Put(MediaStore.Audio.Media.InterfaceConsts.IsMusic, true);
            //    Android.Net.Uri uri = ContentResolver.Insert(MediaStore.Audio.Media.ExternalContentUri, value);
            //    SendBroadcast(new Intent(Intent.ActionMediaScannerScanFile, Android.Net.Uri.Parse("file://" + uri)));
            //}
            //if(artURI != null)
            //{
            //    Android.Net.Uri path = ContentUris.WithAppendedId(Android.Net.Uri.Parse("content://media/external/audio/albumart"), song.GetAlbumArt());
            //    System.Console.WriteLine("&Path : " + path);
            //    bool albumArtExist = true;
            //    try
            //    {
            //        var inStream = ContentResolver.OpenInputStream(path); 
            //    }
            //    catch(FileNotFoundException e)
            //    {
            //        System.Console.WriteLine("&" + e.Message);
            //        albumArtExist = false;
            //    }

            //    if(albumArtExist)
            //        ContentResolver.Delete(path, null, null);

            //    await Task.Delay(10);
            //    ContentValues value = new ContentValues();
            //    value.Put(MediaStore.Audio.Media.InterfaceConsts.AlbumId, song.GetAlbumArt());
            //    value.Put(MediaStore.Audio.Media.InterfaceConsts.Data, artURI.ToString());
            //    Android.Net.Uri uri = ContentResolver.Insert(Android.Net.Uri.Parse("content://media/external/audio/albumart"), value);
            //    if(uri == null)
            //    {
            //        System.Console.WriteLine("&Uri == null");
            //        return;
            //    }
            //    System.Console.WriteLine("&Art uri : " + artURI.ToString() + " Result URI : " + uri.ToString());
            //    SendBroadcast(new Intent(Intent.ActionMediaScannerScanFile, Android.Net.Uri.Parse("file://" + uri)));
            //    artURI = null;
            //    Picasso.With(Application.Context).Load(uri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(albumArt);
            //}


            Stream stream = new FileStream(song.GetPath(), FileMode.Open, FileAccess.ReadWrite);
            //System.Console.WriteLine("&Read Stream created");
            //Stream writeStream = File.OpenWrite(song.GetPath());
            //System.Console.WriteLine("&Write Stream created");
            var meta = TagLib.File.Create(new TagLib.StreamFileAbstraction(song.GetPath(), stream, stream));
            System.Console.WriteLine("&File created");

            meta.Tag.Title = title.Text;
            meta.Tag.Performers = new string[] { artist.Text };
            meta.Tag.Album = album.Text;
            meta.Tag.AmazonId = youtubeID.Text;

            if (artURI != null)
            {
                TagLib.Picture art = new TagLib.Picture(artURI.ToString());
                meta.Tag.Pictures = new TagLib.IPicture[] { art };
            }

            meta.Save();
            stream.Dispose();
            Android.Media.MediaScannerConnection.ScanFile(this, new string[] { song.GetPath() }, null, null);

            /*TagLib.File file = TagLib.File.Create();
            *TagLib.Picture pic = new TagLib.Picture();
            *pic.Type = TagLib.PictureType.FrontCover;
            *pic.Description = "Cover";
            *pic.MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg;
            *MemoryStream ms = new MemoryStream();
            *.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            *ms.Position = 0;
            *pic.Data = TagLib.ByteVector.FromStream(ms);
            *file.Tag.Pictures = new TagLib.IPicture[] { pic };
            *file.Save();
            *ms.Close(); */


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