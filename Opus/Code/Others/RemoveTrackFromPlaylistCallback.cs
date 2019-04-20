using Android.Support.Design.Widget;
using Opus.Api;
using Opus.DataStructure;

namespace Opus.Others
{
    public class RemoveTrackFromPlaylistCallback : BaseTransientBottomBar.BaseCallback
    {
        private Song song;
        private readonly long LocalPlaylistID;
        public bool canceled = false;

        public RemoveTrackFromPlaylistCallback(Song song, long LocalPlaylistID)
        {
            this.song = song;
            this.LocalPlaylistID = LocalPlaylistID;
        }

        public override void OnDismissed(Java.Lang.Object transientBottomBar, int @event)
        {
            base.OnDismissed(transientBottomBar, @event);
            if(!canceled)
            {
                if (song.TrackID != null)
                {
                    PlaylistManager.RemoveFromYoutubePlaylist(song.TrackID);
                }
                if (LocalPlaylistID != 0)
                {
                    PlaylistManager.RemoveFromLocalPlaylist(song, LocalPlaylistID);
                }
            }
        }

        public override void OnShown(Java.Lang.Object transientBottomBar)
        {
            base.OnShown(transientBottomBar);
        }
    }
}