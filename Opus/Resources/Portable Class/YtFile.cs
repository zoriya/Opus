using Opus.Resources.values;

namespace Opus.Resources.Portable_Class
{
    [System.Serializable]
    public class YtFile
    {
        public Song item;
        public YtKind Kind;

        public YtFile(Song item, YtKind kind)
        {
            this.item = item;
            Kind = kind;
        }
    }

    public enum YtKind { Null, Video, Playlist, Channel, ChannelPreview, Loading }
}