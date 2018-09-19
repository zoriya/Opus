using System;
using SQLite;

namespace MusicApp.Resources.values
{
    [Serializable]
    public class Song
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public long AlbumArt { get; set; }
        public string youtubeID { get; set; }
        public long Id { get; set; }
        public string Path { get; set; }
        public bool isParsed { get; set; }
        [PrimaryKey, Unique]
        public int queueSlot { get; set; }
        public bool IsYt { get; set; }
        public DateTimeOffset? expireDate { get; set;}

        public Song() { }

        public Song(string Title, string Artist, string Album, string youtubeID, long AlbumArt, long id, string path, bool isYT = false, bool isParsed = true, int queueSlot = -1)
        {
            this.Title = Title;
            this.Artist = Artist;
            this.Album = Album;
            this.youtubeID = youtubeID;
            this.AlbumArt = AlbumArt;
            Id = id;
            Path = path;
            IsYt = isYT;
            this.isParsed = isParsed;
            this.queueSlot = queueSlot;
        }

        public override string ToString()
        {
            return Title + " Artist: " + Artist + " Album: " + Album + " youtubeID: " + youtubeID + " AlbumArt: " + AlbumArt + " Id: " + Id + " Path: " + Path + " isYT: " + IsYt + " isParsed: " + isParsed + " queueSlot: " + queueSlot;
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
            int queueSlot = int.Parse(v.Substring(v.IndexOf(" queueSlot: ") + 12, v.Length - v.IndexOf(" queueSlot: ") - 12));

            Song song = new Song(Name, Artist, Album, youtubeID, AlbumArt, id, path, isYT, isParsed, queueSlot);
            return song;
        }
    }
}