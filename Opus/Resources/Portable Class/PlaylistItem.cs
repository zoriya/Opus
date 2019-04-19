using Opus.DataStructure;
using Opus.Resources.values;
using SQLite;
using System;

namespace Opus.Resources.Portable_Class
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
        public bool SongContained; //For AddToPlaylist dialog

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

        public Song ToSong()
        {
            return new Song(Name, Owner, ImageURL, YoutubeID, -1, LocalID, null, false, HasWritePermission);
        }

        public static Song ToSong(PlaylistItem item)
        {
            return new Song(item.Name, item.Owner, item.ImageURL, item.YoutubeID, -1, item.LocalID, null, false, item.HasWritePermission);
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