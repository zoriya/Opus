using Org.Adw.Library.Widgets.Discreteseekbar;

namespace MusicApp.Resources.Portable_Class
{
    public class TimerTransformer : DiscreteSeekBar.NumericTransformer
    {
        public override int Transform(int value)
        {
            return value;
        }

        public override string TransformToString(int value)
        {
            int minutes = value / 60000;
            int seconds = value / 1000 % 60;

            string min = minutes.ToString();
            string sec = seconds.ToString();
            if (min.Length == 1)
                min = "0" + min;
            if (sec.Length == 1)
                sec = "0" + sec;

            return min + ":" + sec;
        }

        public override bool UseStringTransform()
        {
            return true;
        }
    }
}