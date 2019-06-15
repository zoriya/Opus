using Android.Support.Design.Widget;
using Opus.Api;
using Opus.DataStructure;

namespace Opus.Others
{
    public class RemoveTrackFromPlaylistCallback : BaseTransientBottomBar.BaseCallback
    {
        private readonly Song song;
        private readonly long LocalPlaylistID;
        private readonly int position;
        public bool canceled = false;

        public RemoveTrackFromPlaylistCallback(Song song, long LocalPlaylistID, int position)
        {
            this.song = song;
            this.LocalPlaylistID = LocalPlaylistID;
            this.position = position;
        }

        public override void OnDismissed(Java.Lang.Object transientBottomBar, int @event)
        {
            base.OnDismissed(transientBottomBar, @event);
            if(canceled)
            {
                MainActivity.instance.SupportFragmentManager.PopBackStack();
                if (LocalPlaylistID != 0)
                    PlaylistManager.InsertToLocalPlaylist(LocalPlaylistID, song, position);
            }
            else
            {
                if (song.TrackID != null)
                    PlaylistManager.RemoveFromYoutubePlaylist(song.TrackID);
            }
        }

        public override void OnShown(Java.Lang.Object transientBottomBar)
        {
            base.OnShown(transientBottomBar);
        }
    }
}