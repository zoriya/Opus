using Android.Content;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Util;
using Java.Lang;
using System;

namespace MusicApp.Resources.Portable_Class
{
    public class FixedLinearLayoutManager : LinearLayoutManager
    {
        public FixedLinearLayoutManager(Context context) : base(context) { }

        public FixedLinearLayoutManager(Context context, int orientation, bool reverseLayout) : base(context, orientation, reverseLayout) { }

        public FixedLinearLayoutManager(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes) { }

        protected FixedLinearLayoutManager(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }

        public override void OnLayoutChildren(RecyclerView.Recycler recycler, RecyclerView.State state)
        {
            try
            {
                base.OnLayoutChildren(recycler, state);
            }
            catch (IndexOutOfRangeException)
            {
                Log.Error("MusicApp", "Recycler view found an IndexOutOfRangeException.");
            }
            catch (RuntimeException)
            {
                Log.Error("MusicApp", "Recycler view found a RuntimeException.");
            }
        }
    }
}