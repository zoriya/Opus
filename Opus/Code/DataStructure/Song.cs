using Android.Database;
using Android.Gms.Cast;
using Android.Provider;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;

namespace Opus.DataStructure
{
    [Serializable]
    public class Song
    {
        [PrimaryKey, Unique, AutoIncrement]
        public int Index { get; set; }

        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public long AlbumArt { get; set; }
        public string YoutubeID { get; set; }
        public long LocalID { get; set; }
        public string Path { get; set; }
        public bool? IsParsed { get; set; }
        public bool IsYt { get; set; }
        public string ChannelID { get; set; }
        public DateTimeOffset? ExpireDate { get; set;}
        public int Duration { get; set; }
        public bool IsLiveStream = false;
        public string TrackID;

        public Song() { }

        public Song(string title, string artist, string album, string youtubeID, long albumArt, long id, string path, bool isYT = false, bool isParsed = true)
        {
            Title = title;
            Artist = artist;
            Album = album;
            YoutubeID = youtubeID;
            AlbumArt = albumArt;
            LocalID = id;
            Path = path;
            IsYt = isYT;
            IsParsed = isParsed;
        }

        public override string ToString()
        {
            return Title + " Artist: " + Artist + " Album: " + Album + " youtubeID: " + YoutubeID + " AlbumArt: " + AlbumArt + " Id: " + LocalID + " Path: " + Path + " isYT: " + IsYt + " isParsed: " + IsParsed;
        }

        public static Song FromCursor(ICursor cursor)
        {
            int titleID = cursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Title);
            int artistID = cursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Artist);
            int albumID = cursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Album);
            int thisID = cursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Id);
            int pathID = cursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Data);
            //int playOrderID = cursor.GetColumnIndex(MediaStore.Audio.Playlists.Members.PlayOrder);

            //string playOrder = PlaylistTracks.instance != null ? cursor.GetString(playOrderID) : null;
            string Artist = cursor.GetString(artistID);
            string Title = cursor.GetString(titleID);
            string Album = cursor.GetString(albumID);
            long AlbumArt = cursor.GetLong(cursor.GetColumnIndex(MediaStore.Audio.Albums.InterfaceConsts.AlbumId));
            long id = cursor.GetLong(thisID);
            string path = cursor.GetString(pathID);

            if (Title == null)
                Title = "Unknown Title";
            if (Artist == null)
                Artist = "Unknow Artist";
            if (Album == null)
                Album = "Unknow Album";

            return new Song(Title, /*playOrder ?? */Artist, Album, null, AlbumArt, id, path);
        }

        public static explicit operator Song(YoutubeExplode.Models.Video video)
        {
            return new Song(video.Title, video.Author, video.Thumbnails.HighResUrl, video.Id, -1, -1, null, true, false);
        }

        public static List<Song> FromVideoArray(IReadOnlyList<YoutubeExplode.Models.Video> videos)
        {
            List<Song> songs = new List<Song>();
            for (int i = 0; i < videos.Count; i++)
            {
                songs.Add((Song)videos[i]);
            }
            return songs;
        }


        public static explicit operator Song(string v)
        {
            if (v == null)
                return null;

            string Name = v.Substring(0, v.IndexOf(" Artist: "));
            string Artist = v.Substring(v.IndexOf(" Artist: ") + 9, v.IndexOf(" Album: ") - Name.Length - 8);
            string Album = v.Substring(v.IndexOf(" Album: ") + 8, v.IndexOf(" youtubeID: ") - v.IndexOf(" Album: ") - 8);
            string youtubeID = v.Substring(v.IndexOf(" youtubeID: ") + 12, v.IndexOf(" AlbumArt: ") - v.IndexOf(" youtubeID: ") - 12);
            long AlbumArt = long.Parse(v.Substring(v.IndexOf(" AlbumArt: ") + 11, v.IndexOf(" Id: ") - v.IndexOf(" AlbumArt: ") - 11));
            long id = long.Parse(v.Substring(v.IndexOf(" Id: ") + 5, v.IndexOf(" Path: ") - v.IndexOf(" Id: ") - 5));
            string path = v.Substring(v.IndexOf(" Path: ") + 7, v.IndexOf(" isYT: ") - v.IndexOf(" Path: ") - 7);
            bool isYT = bool.Parse(v.Substring(v.IndexOf(" isYT: ") + 7, v.IndexOf(" isParsed: ") - v.IndexOf(" isYT: ") - 7));
            bool isParsed = bool.Parse(v.Substring(v.IndexOf(" isParsed: ") + 11));

            Song song = new Song(Name, Artist, Album, youtubeID, AlbumArt, id, path, isYT, isParsed);
            return song;
        }

        public static explicit operator Song(MediaQueueItem v)
        {
            if (v == null)
                return null;

            return JsonConvert.DeserializeObject<Song>(v.Media.CustomData.ToString());
        }
    }

    public class SongHolder : RecyclerView.ViewHolder
    {
        public ImageView reorder;
        public TextView Title;
        public TextView Artist;
        public TextView Live;
        public ImageView AlbumArt;
        public ImageView youtubeIcon;
        public ImageView more;
        public TextView status;
        public Button action;
        public View RightButtons;
        public View TextLayout;

        public SongHolder(View itemView, Action<int> listener, Action<int> longListener) : base(itemView)
        {
            reorder = itemView.FindViewById<ImageView>(Resource.Id.reorder);
            Title = itemView.FindViewById<TextView>(Resource.Id.title);
            Artist = itemView.FindViewById<TextView>(Resource.Id.artist);
            Live = itemView.FindViewById<TextView>(Resource.Id.isLive);
            AlbumArt = itemView.FindViewById<ImageView>(Resource.Id.albumArt);
            youtubeIcon = itemView.FindViewById<ImageView>(Resource.Id.youtubeIcon);
            more = itemView.FindViewById<ImageView>(Resource.Id.moreButton);
            RightButtons = itemView.FindViewById(Resource.Id.rightButtons);
            TextLayout = itemView.FindViewById(Resource.Id.textLayout);
            status = itemView.FindViewById<TextView>(Resource.Id.status);
            action = itemView.FindViewById<Button>(Resource.Id.action);

            itemView.Click += (sender, e) => listener(AdapterPosition);
            itemView.LongClick += (sender, e) => longListener(AdapterPosition);
        }
    }
}