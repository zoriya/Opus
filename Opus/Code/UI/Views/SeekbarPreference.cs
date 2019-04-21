using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Support.V7.Preferences;
using Android.Util;
using Opus;
using Opus.Api.Services;
using Opus.Others;
using Org.Adw.Library.Widgets.Discreteseekbar;
using System;

[Register("Opus/SeekbarPreference")]
public class SeekbarPreference : Preference
{
    public SeekbarPreference(Context context) : base(context) { CreateView(); }

    public SeekbarPreference(Context context, IAttributeSet attrs) : base(context, attrs) { CreateView(); }

    public SeekbarPreference(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr) { CreateView(); }

    public SeekbarPreference(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes) { CreateView(); }

    protected SeekbarPreference(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { CreateView(); }

    public void CreateView()
    {
        LayoutResource = Resource.Layout.SeekbarPreference;
    }

    public override void OnBindViewHolder(PreferenceViewHolder holder)
    {
        base.OnBindViewHolder(holder);
        ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
        DiscreteSeekBar seekbar = holder.ItemView.FindViewById<DiscreteSeekBar>(Resource.Id.seekbar);
        seekbar.Progress = prefManager.GetInt("volumeMultiplier", 100);
        seekbar.SetNumericTransformer(new PercentTransform());
        seekbar.ProgressChanged += (sender, e) =>
        {
            bool FromUser = e.FromUser;
            int Progress = e.Value;

            ISharedPreferences pref = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            ISharedPreferencesEditor editor = pref.Edit();
            editor.PutInt("volumeMultiplier", Progress);
            editor.Apply();

            if (MusicPlayer.instance != null)
                MusicPlayer.instance.ChangeVolume(Progress / 100f);
        };
    }
}