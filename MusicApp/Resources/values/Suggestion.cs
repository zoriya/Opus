namespace MusicApp.Resources.values
{
    [System.Serializable]
    public class Suggestion
    {
        public int Icon;
        public string Text;

        public Suggestion(int icon, string text)
        {
            Icon = icon;
            Text = text;
        }
    }
}