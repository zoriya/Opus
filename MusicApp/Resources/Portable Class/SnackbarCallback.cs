using Android.Content;
using Android.Net;
using Android.Provider;
using Android.Support.Design.Widget;
using MusicApp.Resources.values;

namespace MusicApp.Resources.Portable_Class
{
    public class SnackbarCallback : BaseTransientBottomBar.BaseCallback
    {
        private int position;
        private Song song;
        private long playlistId;
        public bool canceled = false;

        public SnackbarCallback(int position, Song song, long playlistId)
        {
            this.position = position;
            this.song = song;
            this.playlistId = playlistId;
        }

        public override void OnDismissed(Java.Lang.Object transientBottomBar, int @event)
        {
            base.OnDismissed(transientBottomBar, @event);
            if(!canceled)
            {
                if (song.TrackID != null)
                {
                    YoutubeEngine.RemoveFromPlaylist(song.TrackID);
                }
                if (playlistId != 0)
                {
                    ContentResolver resolver = MainActivity.instance.ContentResolver;
                    Uri uri = MediaStore.Audio.Playlists.Members.GetContentUri("external", playlistId);
                    resolver.Delete(uri, MediaStore.Audio.Playlists.Members.Id + "=?", new string[] { song.Id.ToString() });
                }
            }
        }

        public override void OnShown(Java.Lang.Object transientBottomBar)
        {
            base.OnShown(transientBottomBar);
        }
    }
}