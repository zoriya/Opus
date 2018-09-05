using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Widget;
using Java.Lang;

[Register("com.musicapp.BottomFollowBehavior")]
public class BottomFollowBehavior : CoordinatorLayout.Behavior
{
    public override bool LayoutDependsOn(CoordinatorLayout parent, Java.Lang.Object child, View dependency)
    {
        return base.LayoutDependsOn(parent, child, dependency);
    }
    //    override fun layoutDependsOn(parent: CoordinatorLayout?, child: View, dependency: View): Boolean {
    //        return dependency is Snackbar.SnackbarLayout
    //}

    //override fun onDependentViewRemoved(parent: CoordinatorLayout, child: View, dependency: View)
    //{
    //    child.translationY = 0f
    //    }

    //override fun onDependentViewChanged(parent: CoordinatorLayout, child: View, dependency: View): Boolean {
    //        return updateButton(child, dependency)
    //    }

    //    private fun updateButton(child: View, dependency: View): Boolean {
    //        if (dependency is Snackbar.SnackbarLayout) {
    //            val oldTranslation = child.translationY
    //            val height = dependency.height.toFloat()
    //            val newTranslation = dependency.translationY - height
    //            child.translationY = newTranslation

    //            return oldTranslation != newTranslation
    //        }
    //        return false
    //}
}