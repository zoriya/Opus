using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using SQLite;
using System;

namespace Opus.DataStructure
{
    [Serializable]
    public class SavedPlaylist : PlaylistItem
    {
        [PrimaryKey, Unique]
        public new string YoutubeID { get; set; }
        public new long LocalID { get; set; }

        public SavedPlaylist() { }

        public SavedPlaylist(PlaylistItem item)
        {
            Name = item.Name;
            LocalID = item.LocalID;
            YoutubeID = item.YoutubeID;
            Count = item.Count;
            Snippet = item.Snippet;
            Owner = item.Owner;
            ImageURL = item.ImageURL;
            HasWritePermission = item.HasWritePermission;
            SyncState = item.SyncState;
        }
    }

    [Serializable]
    public class PlaylistItem
    {
        public string Name { get; set; }
        [PrimaryKey, Unique]
        public long LocalID { get; set; }
        public string YoutubeID { get; set; }
        public int Count = -1;
        public Google.Apis.YouTube.v3.Data.Playlist Snippet;
        public string Owner { get; set; }
        public string ImageURL { get; set; }
        public bool HasWritePermission { get; set; }
        public SyncState SyncState = SyncState.False;
        public bool SongContained; //For AddToPlaylist dialog

        public PlaylistItem() { }

        public PlaylistItem(string Name, long LocalID, int Count = -1)
        {
            this.Name = Name;
            this.LocalID = LocalID;
            this.Count = Count;
        }

        public PlaylistItem(string Name, string YoutubeID, Google.Apis.YouTube.v3.Data.Playlist Snippet = null, int Count = -1)
        {
            this.Name = Name;
            LocalID = -1;
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

    public class PlaylistHolder : RecyclerView.ViewHolder
    {
        public TextView Title;
        public TextView Owner;
        public ImageView AlbumArt;
        public ImageView edit;
        public ImageView sync;
        public ProgressBar SyncLoading;
        public ImageView more;

        public PlaylistHolder(View itemView, Action<int> listener, Action<int> longListener) : base(itemView)
        {
            Title = itemView.FindViewById<TextView>(Resource.Id.title);
            Owner = itemView.FindViewById<TextView>(Resource.Id.artist);
            AlbumArt = itemView.FindViewById<ImageView>(Resource.Id.albumArt);
            edit = itemView.FindViewById<ImageView>(Resource.Id.edit);
            sync = itemView.FindViewById<ImageView>(Resource.Id.sync);
            SyncLoading = itemView.FindViewById<ProgressBar>(Resource.Id.syncLoading);
            more = itemView.FindViewById<ImageView>(Resource.Id.moreButton);

            itemView.Click += (sender, e) => listener(AdapterPosition);
            itemView.LongClick += (sender, e) => longListener(AdapterPosition);
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