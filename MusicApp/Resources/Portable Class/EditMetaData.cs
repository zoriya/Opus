using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.Support.V4.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using Square.Picasso;

namespace MusicApp.Resources.Portable_Class
{
    public class EditMetaData : Fragment
    {
        public static EditMetaData instance;
        public Song song;

        private View view;


        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            view = inflater.Inflate(Resource.Layout.EditMetaData, container);
            ImageView albumArt = view.FindViewById<ImageView>(Resource.Id.metadataArt);


            if (song.GetAlbumArt() == -1 || song.IsYt)
            {
                var songAlbumArtUri = Android.Net.Uri.Parse(song.GetAlbum());
                Picasso.With(Android.App.Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(albumArt);
            }
            else
            {
                var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                var songAlbumArtUri = ContentUris.WithAppendedId(songCover, song.GetAlbumArt());

                Picasso.With(Android.App.Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(albumArt);
            }

            return view;
        }

        public static Fragment NewInstance(Song item)
        {
            instance = new EditMetaData { Arguments = new Bundle() };
            instance.song = item;
            return instance;
        }        
    }
}