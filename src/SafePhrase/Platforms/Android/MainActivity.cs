using Android.App;
using Android.Content.PM;
using Android.OS;

namespace SafePhrase
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnResume()
        {
            base.OnResume();
            SafetyModeState.IsAppVisible = true;
            SafetyModeState.Update(SafetyModeState.IsActive, SafetyModeState.Status);
        }

        protected override void OnPause()
        {
            SafetyModeState.IsAppVisible = false;
            SafetyModeState.Update(SafetyModeState.IsActive, SafetyModeState.Status);
            base.OnPause();
        }
    }
}
