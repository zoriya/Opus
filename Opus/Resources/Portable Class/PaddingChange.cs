using System;

namespace Opus.Resources.values
{
    public class PaddingChange : EventArgs
    {
        public int oldPadding;
        public int paddingChange = 0;

        public PaddingChange(int oldPadding)
        {
            this.oldPadding = oldPadding;
        }

        public PaddingChange(int oldPadding, int paddingChange)
        {
            this.oldPadding = oldPadding;
            this.paddingChange = paddingChange;
        }
    }
}