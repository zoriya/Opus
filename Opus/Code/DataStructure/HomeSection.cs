using Android.Support.V7.Widget;
using System.Collections.Generic;

namespace Opus.DataStructure
{
    [System.Serializable]
    public class Section
    {
        public string SectionTitle;
        public SectionType contentType;
        public List<Song> contentValue;
        public List<PlaylistItem> playlistContent;
        public List<Channel> channelContent;
        public PlaylistItem playlist;
        public RecyclerView recycler;

        public Section(string sectionTitle, SectionType contentType)
        {
            SectionTitle = sectionTitle;
            this.contentType = contentType;
        }

        public Section(string sectionTitle, SectionType contentType, List<Song> contentValue)
        {
            SectionTitle = sectionTitle;
            this.contentType = contentType;
            this.contentValue = contentValue;
        }

        public Section(string sectionTitle, SectionType contentType, List<Song> contentValue, PlaylistItem playlist)
        {
            SectionTitle = sectionTitle;
            this.contentType = contentType;
            this.contentValue = contentValue;
            this.playlist = playlist;
        }

        public Section(string sectionTitle, SectionType contentType, List<PlaylistItem> playlistContent)
        {
            SectionTitle = sectionTitle;
            this.contentType = contentType;
            this.playlistContent = playlistContent;
        }

        public Section(string sectionTitle, SectionType contentType, List<Channel> channelContent)
        {
            SectionTitle = sectionTitle;
            this.contentType = contentType;
            this.channelContent = channelContent;
        }
    }

    public enum SectionType
    {
        None,
        ChannelList,
        PlaylistList,
        SinglePlaylist,
        TopicSelector,
        Shuffle
    };
}