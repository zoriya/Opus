using Android.Content;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Util;
using Android.Views;
using System;

[Register("MusicApp/PlayerBehavior")]
public class PlayerBehavior : BottomSheetBehavior
{
    public bool PreventSlide = false;

    public PlayerBehavior() { }

    public PlayerBehavior(Context context, IAttributeSet attrs) : base(context, attrs) { }

    protected PlayerBehavior(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }

    public override bool OnInterceptTouchEvent(CoordinatorLayout parent, Java.Lang.Object child, MotionEvent ev)
    {
        if (PreventSlide)
            return false;

        return base.OnInterceptTouchEvent(parent, child, ev);
    }

    public override bool OnTouchEvent(CoordinatorLayout parent, Java.Lang.Object child, MotionEvent ev)
    {
        if (PreventSlide)
            return false;

        return base.OnTouchEvent(parent, child, ev);
    }

    public override bool OnStartNestedScroll(CoordinatorLayout coordinatorLayout, Java.Lang.Object child, View directTargetChild, View target, int axes, int type)
    {
        if (PreventSlide)
            return false;

        return base.OnStartNestedScroll(coordinatorLayout, child, directTargetChild, target, axes, type);
    }

    public override bool OnNestedPreFling(CoordinatorLayout coordinatorLayout, Java.Lang.Object child, View target, float velocityX, float velocityY)
    {
        if (PreventSlide)
            return false;

        return base.OnNestedPreFling(coordinatorLayout, child, target, velocityX, velocityY);
    }

    public override void OnNestedPreScroll(CoordinatorLayout coordinatorLayout, Java.Lang.Object child, View target, int dx, int dy, int[] consumed, int type)
    {
        if (PreventSlide)
            return;

        base.OnNestedPreScroll(coordinatorLayout, child, target, dx, dy, consumed, type);
    }

    public override void OnStopNestedScroll(CoordinatorLayout coordinatorLayout, Java.Lang.Object child, View target, int type)
    {
        if (PreventSlide)
            return;

        base.OnStopNestedScroll(coordinatorLayout, child, target, type);
    }
}