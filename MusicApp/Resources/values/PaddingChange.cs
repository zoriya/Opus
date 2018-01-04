using System;

namespace MusicApp.Resources.values
{
    public class PaddingChange : EventArgs
    {
        public int oldPadding;

        public PaddingChange(int oldPadding)
        {
            this.oldPadding = oldPadding;
        }
    }
}