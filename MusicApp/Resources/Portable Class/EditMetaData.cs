using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using Square.Picasso;

namespace MusicApp.Resources.Portable_Class
{
    [Activity(Label = "EditMetaData", Theme = "@style/Theme")]
    public class EditMetaData : AppCompatActivity
    {
        public static EditMetaData instance;
        public Song song;

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

            TextView title = FindViewById<TextView>(Resource.Id.metadataTitle);
            TextView artist = FindViewById<TextView>(Resource.Id.metadataArtist);
            TextView album = FindViewById<TextView>(Resource.Id.metadataAlbum);
            TextView youtubeID = FindViewById<TextView>(Resource.Id.metadataYID);
            ImageView albumArt = FindViewById<ImageView>(Resource.Id.metadataArt);
            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.metadataFAB);
            fab.Click += (sender, e) => { ValidateChanges(); };

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
                ValidateChanges();
                Intent intent = new Intent(this, typeof(MainActivity));
                StartActivity(intent);
                return true;
            }
            return false;
        }

        void ValidateChanges()
        {

        }
    }
}