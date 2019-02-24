using System.Collections.Generic;
using Android.Runtime;
using System;
using Java.Lang;
using Android.Support.V4.App;

namespace Opus.Resources.Portable_Class
{
    public class ViewPagerAdapter : FragmentStatePagerAdapter
    {
        private List<Fragment> fragmentList = new List<Fragment>();
        private List<string> titles = new List<string>();

        public ViewPagerAdapter(FragmentManager fm) : base(fm) { }
        protected ViewPagerAdapter(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }

        public override int Count => fragmentList.Count;

        public override Fragment GetItem(int position)
        {
            return fragmentList[position];
        }

        public void AddFragment (Fragment fragment, string title)
        {
            fragmentList.Add(fragment);
            titles.Add(title);
        }

        public void Clear()
        {
            fragmentList = new List<Fragment>();
            titles = new List<string>();
            NotifyDataSetChanged();
        }

        public override ICharSequence GetPageTitleFormatted(int position)
        {
            ICharSequence title = CharSequence.ArrayFromStringArray(new string[] { titles[position] })[0];
            return title;
        }

        public override int GetItemPosition(Java.Lang.Object @object)
        {
            return PositionNone;
        }
    }
}