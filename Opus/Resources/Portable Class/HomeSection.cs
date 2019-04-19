using Android.Support.V7.Widget;
using Opus.DataStructure;
using Opus.Resources.Portable_Class;
using System.Collections.Generic;

namespace Opus.Resources.values
{
    [System.Serializable]
    public class HomeSection
    {
        public string SectionTitle;
        public SectionType contentType;
        public List<Song> contentValue;
        public List<PlaylistItem> playlistContent;
        public RecyclerView recycler;

        public HomeSection(string sectionTitle, SectionType contentType)
        {
            SectionTitle = sectionTitle;
            this.contentType = contentType;
        }

        public HomeSection(string sectionTitle, SectionType contentType, List<Song> contentValue)
        {
            SectionTitle = sectionTitle;
            this.contentType = contentType;
            this.contentValue = contentValue;
        }

        public HomeSection(string sectionTitle, SectionType contentType, List<PlaylistItem> playlistContent)
        {
            SectionTitle = sectionTitle;
            this.contentType = contentType;
            this.playlistContent = playlistContent;
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