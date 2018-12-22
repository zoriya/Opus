using System;
using Android.Gms.Cast;
using Newtonsoft.Json;
using SQLite;

namespace MusicApp.Resources.values
{
    [Serializable]
    public class Song
    {
        [PrimaryKey, Unique, AutoIncrement]
        private int Index { get; set; }

        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public long AlbumArt { get; set; }
        public string YoutubeID { get; set; }
        public long Id { get; set; }
        public string Path { get; set; }
        public bool IsParsed { get; set; }
        public bool IsYt { get; set; }
        public DateTimeOffset? ExpireDate { get; set;}
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
            Id = id;
            Path = path;
            IsYt = isYT;
            IsParsed = isParsed;
        }

        public override string ToString()
        {
            return Title + " Artist: " + Artist + " Album: " + Album + " youtubeID: " + YoutubeID + " AlbumArt: " + AlbumArt + " Id: " + Id + " Path: " + Path + " isYT: " + IsYt + " isParsed: " + IsParsed;
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
            bool isParsed = bool.Parse(v.Substring(v.IndexOf(" isParsed: ") + 11, v.IndexOf(" queueSlot: ") - v.IndexOf(" isParsed: ") - 11));

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
}