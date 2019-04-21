using Org.Adw.Library.Widgets.Discreteseekbar;

namespace Opus.Others
{
    public class PercentTransform : DiscreteSeekBar.NumericTransformer
    {
        public override int Transform(int value)
        {
            return value;
        }

        public override string TransformToString(int value)
        {
            return value + "%";
        }

        public override bool UseStringTransform()
        {
            return true;
        }
    }
}