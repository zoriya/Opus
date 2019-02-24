using Android.Content;
using Android.Runtime;
using Android.Support.V4.Widget;
using Android.Util;
using Android.Views;
using System;

[Register("Opus/FixedNestedScrollView")]
public class FixedNestedScrollView : NestedScrollView
{
    public static bool PreventSlide = false;

    public FixedNestedScrollView(Context context) : base(context) { }
    public FixedNestedScrollView(Context context, IAttributeSet attrs) : base(context, attrs) { }
    public FixedNestedScrollView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr) { }
    protected FixedNestedScrollView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }

    //Overriden because DrawerLayout need to be measured with MeasureSpecMode EXACLTY
    protected override void MeasureChildWithMargins(View child, int parentWidthMeasureSpec, int widthUsed, int parentHeightMeasureSpec, int heightUsed)
    {
        MarginLayoutParams lp = (MarginLayoutParams)child.LayoutParameters;
        int childWidthMeasureSpec = GetChildMeasureSpec(parentWidthMeasureSpec, PaddingLeft + PaddingRight + lp.LeftMargin + lp.RightMargin + widthUsed, lp.Width);
        int childHeightMeasureSpec = MeasureSpec.MakeMeasureSpec(parentHeightMeasureSpec, MeasureSpecMode.Exactly); //There is only one child and this child has match_parent so we want to make his height equal to this view's height
        child.Measure(childWidthMeasureSpec, childHeightMeasureSpec);
    }

    public override bool OnInterceptTouchEvent(MotionEvent ev)
    {
        if(PreventSlide)
            return false;

        return base.OnInterceptTouchEvent(ev);
    }

    public override bool OnTouchEvent(MotionEvent e)
    {
        if(PreventSlide)
            return false;

        return base.OnTouchEvent(e);
    }
}
