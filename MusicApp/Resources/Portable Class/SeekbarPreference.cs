using Android.App;
using Android.Content;
using Android.Preferences;
using Android.Runtime;
using Android.Util;
using Android.Views;
using MusicApp;
using MusicApp.Resources.Portable_Class;
using Org.Adw.Library.Widgets.Discreteseekbar;
using System;

public class SeekbarPreference : Preference
{
    public SeekbarPreference(Context context) : base(context) { }

    public SeekbarPreference(Context context, IAttributeSet attrs) : base(context, attrs) { }

    public SeekbarPreference(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr) { }

    public SeekbarPreference(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes) { }

    protected SeekbarPreference(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }

    protected override View OnCreateView(ViewGroup parent)
    {
        LayoutInflater inflater = (LayoutInflater)Context.GetSystemService(Context.LayoutInflaterService);
        View view = inflater.Inflate(Resource.Layout.SeekbarPreference, parent, false);
        ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
        DiscreteSeekBar seekbar = view.FindViewById<DiscreteSeekBar>(Resource.Id.seekbar);
        seekbar.Progress = prefManager.GetInt("volumeMultiplier", 100);
        seekbar.SetNumericTransformer(new PercentTransform());
        seekbar.ProgressChanged += (sender, e) =>
        {
            bool FromUser = e.P2;
            int Progress = e.P1;

            ISharedPreferences pref = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            ISharedPreferencesEditor editor = pref.Edit();
            editor.PutInt("volumeMultiplier", Progress);
            editor.Apply();

            if (MusicPlayer.instance != null)
                MusicPlayer.instance.ChangeVolume(Progress / 100f);
        };
        return view;
    }
}