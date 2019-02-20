using Android.Content;
using Android.Runtime;
using Android.Support.V4.Widget;
using Android.Util;
using Android.Views;
using System;

[Register("MusicApp/FixedNestedScrollView")]
public class FixedNestedScrollView : NestedScrollView
{
    public static bool PreventSlide = false;

    public FixedNestedScrollView(Context context) : base(context) { }
    public FixedNestedScrollView(Context context, IAttributeSet attrs) : base(context, attrs) { }
    public FixedNestedScrollView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr) { }
    protected FixedNestedScrollView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }

    ////Overriden because DrawerLayout need to be measured with MeasureSpecMode EXACLTY
    //protected override void MeasureChildWithMargins(View child, int parentWidthMeasureSpec, int widthUsed, int parentHeightMeasureSpec, int heightUsed)
    //{
    //    //if (child is DrawerLayout)
    //    //{
    //        MarginLayoutParams lp = (MarginLayoutParams)child.LayoutParameters;
    //        int childWidthMeasureSpec = GetChildMeasureSpec(parentWidthMeasureSpec, PaddingLeft + PaddingRight + lp.LeftMargin + lp.RightMargin + widthUsed, lp.Width);
    //        int childHeightMeasureSpec = MeasureSpec.MakeMeasureSpec(parentWidthMeasureSpec, MeasureSpecMode.Exactly);
    //        child.Measure(childWidthMeasureSpec, childHeightMeasureSpec);
    //    Console.WriteLine("&Height: " + child.MeasuredHeight + " Child: " + child);
    //    //}
    //    //else
    //    //    base.MeasureChildWithMargins(child, parentWidthMeasureSpec, widthUsed, parentWidthMeasureSpec, heightUsed);
    //}

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
