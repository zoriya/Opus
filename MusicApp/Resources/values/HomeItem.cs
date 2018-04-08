using System.Collections.Generic;

namespace MusicApp.Resources.values
{
    [System.Serializable]
    public class HomeItem
    {
        public string SectionTitle;
        public SectionType contentType;
        public List<string> contentValue;

        public HomeItem(string sectionTitle, SectionType contentType, List<string> contentValue)
        {
            SectionTitle = sectionTitle;
            this.contentType = contentType;
            this.contentValue = contentValue;
        }

        public void AddContent(HomeItem item)
        {
            if(item.contentType == contentType)
            {
                contentValue.AddRange(item.contentValue);
            }
            else
                System.Console.WriteLine("&Adding home content from another type is actualy not supported (adding " + item.contentType + " into " + contentType + ").");
        }
    }
}