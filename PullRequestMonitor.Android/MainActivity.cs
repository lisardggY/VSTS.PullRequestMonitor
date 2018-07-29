using Android.App;
using Android.Widget;
using Android.OS;
using Android.Preferences;
using Android.Support.V7.App;

namespace PullRequestMonitor.Android
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : PreferenceActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AddPreferencesFromResource(Resource.);
        }
    }
}

