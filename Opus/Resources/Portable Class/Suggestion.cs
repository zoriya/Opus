using SQLite;

namespace Opus.Resources.values
{
    [System.Serializable]
    [Table("Suggestion")]
    public class Suggestion
    {
        [PrimaryKey, AutoIncrement]
        public int Icon { get; set; }
        public string Text { get; set; }

        public Suggestion(int icon, string text)
        {
            Icon = icon;
            Text = text;
        }

        public Suggestion() { }
    }
}