using System.Collections.Generic;

namespace MusicApp.Resources.values
{
    [System.Serializable]
    public class HomeSection
    {
        public string SectionTitle;
        public SectionType contentType;
        public List<Song> contentValue;

        public HomeSection(string sectionTitle, string contentType, List<Song> contentValue)
        {
            SectionTitle = sectionTitle;
            this.contentType = contentType;
            this.contentValue = contentValue;
        }
    }

    public enum SectionType
    {
        ChannelList,
        PlaylistList
    };
}