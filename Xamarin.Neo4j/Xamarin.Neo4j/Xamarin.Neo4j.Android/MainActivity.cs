using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.View;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using AColor = Android.Graphics.Color;

namespace Xamarin.Neo4j.Android
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation |
                               ConfigChanges.UiMode | ConfigChanges.ScreenLayout |
                               ConfigChanges.SmallestScreenSize | ConfigChanges.Density
    )]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnResume()
        {
            base.OnResume();

            // Re-apply theme and nav bar colours when returning to the foreground.
            // ConfigChanges.UiMode prevents Activity recreation on system theme change,
            // so RequestedThemeChanged may not fire reliably — we re-derive the theme
            // from the current configuration instead.
            if (IPlatformApplication.Current?.Application is App app)
            {
                var nightMode = Resources.Configuration.UiMode & UiMode.NightMask;
                var isDark = nightMode == UiMode.NightYes;

                // Status bar: match page background, icons contrast with it
                if (Window != null)
                {
                    var statusBarColor = isDark
                        ? AColor.ParseColor("#0c0c0c")
                        : AColor.ParseColor("#f5f5f5");
#pragma warning disable CA1422
                    Window.SetStatusBarColor(statusBarColor);
#pragma warning restore CA1422

                    var controller = new WindowInsetsControllerCompat(Window, Window.DecorView);
                    controller.AppearanceLightStatusBars = !isDark;
                }

                MainThread.BeginInvokeOnMainThread(() =>
                    app.UpdateTheme(isDark ? AppTheme.Dark : AppTheme.Light));
            }
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            if (hasFocus)
                TintToolbarIcons(Window?.DecorView as ViewGroup);
        }

        // MAUI creates its toolbar programmatically so theme-based tinting doesn't reach
        // the overflow icon. Walk the view tree and apply the tint directly.
        private static void TintToolbarIcons(ViewGroup parent)
        {
            if (parent == null) return;
            for (var i = 0; i < parent.ChildCount; i++)
            {
                var child = parent.GetChildAt(i);
                if (child is Toolbar toolbar)
                {
                    toolbar.OverflowIcon?.SetTint(AColor.White);
                    toolbar.NavigationIcon?.SetTint(AColor.White);
                }
                else if (child is ViewGroup vg)
                    TintToolbarIcons(vg);
            }
        }
    }
}
