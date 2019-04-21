namespace Opus.DataStructure
{
    [System.Serializable]
    public class YtFile
    {
        public Song song;
        public PlaylistItem playlist;
        public Channel channel;
        public YtKind Kind;

        public YtFile(Song song, YtKind kind)
        {
            this.song = song;
            Kind = kind;
        }

        public YtFile(PlaylistItem playlist, YtKind kind)
        {
            this.playlist = playlist;
            Kind = kind;
        }

        public YtFile(Channel channel, YtKind kind)
        {
            this.channel = channel;
            Kind = kind;
        }
    }
}

public enum YtKind { Null, Video, Playlist, Channel, ChannelPreview, Loading }