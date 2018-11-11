using SQLite;
using System;

namespace MusicApp.Resources.Portable_Class
{
    [Serializable]
    public class PlaylistItem
    {
        public string Name { get; set; }
        [PrimaryKey, Unique]
        public long LocalID { get; set; }
        public string YoutubeID { get; set; }
        public int Count;
        public Google.Apis.YouTube.v3.Data.Playlist Snippet;
        public string Owner { get; set; }
        public string ImageURL { get; set; }
        public bool HasWritePermission { get; set; }
        public SyncState SyncState = SyncState.False;

        public PlaylistItem() { }

        public PlaylistItem(string Name, long LocalID, int Count = 0)
        {
            this.Name = Name;
            this.LocalID = LocalID;
            this.Count = Count;
        }

        public PlaylistItem(string Name, string YoutubeID, Google.Apis.YouTube.v3.Data.Playlist Snippet = null, int Count = 0)
        {
            this.Name = Name;
            this.YoutubeID = YoutubeID;
            this.Snippet = Snippet;
            this.Count = Count;
        }

        public PlaylistItem(string Name, long LocalID, string YoutubeID)
        {
            this.Name = Name;
            this.LocalID = LocalID;
            this.YoutubeID = YoutubeID;
        }
    }

    public enum SyncState
    {
        False,
        True,
        Loading,
        Error
    }
}