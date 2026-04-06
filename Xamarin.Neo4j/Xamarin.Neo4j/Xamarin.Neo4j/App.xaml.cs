using System;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Xamarin.Neo4j.Pages;
using Xamarin.Neo4j.Themes;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace Xamarin.Neo4j
{
    public partial class App : Application
    {
        public static event EventHandler ThemeChanged;

        public App()
        {
            InitializeComponent();

            SetTheme(Current.RequestedTheme);
            RequestedThemeChanged += (s, e) => SetTheme(e.RequestedTheme);

#pragma warning disable CS0618
            MainPage = new NavigationPage(new ConnectionsPage());
#pragma warning restore CS0618

            // SetTheme ran before MainPage was assigned, so apply bar colours now.
            ApplyNavBarColors();
        }

        public void UpdateTheme(AppTheme theme) => SetTheme(theme);

        private void SetTheme(AppTheme theme)
        {
            Resources = theme switch
            {
                AppTheme.Dark => new DarkTheme(),
                AppTheme.Light => new LightTheme(),

                _ => new LightTheme()
            };

            ApplyNavBarColors();
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }

        // Called both from SetTheme and from MainActivity.OnResume so that
        // theme changes while the app is backgrounded are picked up on return.
        public void ApplyNavBarColors()
        {
            if (Windows.Count > 0 && Windows[0].Page is NavigationPage navPage)
            {
                if (Resources.TryGetValue("NavigationBarColor", out var navColor) && navColor is Color color)
                    navPage.BarBackgroundColor = color;
                else
                    navPage.BarBackgroundColor = Color.FromArgb("#31333b");

                navPage.BarTextColor = Colors.White;
            }
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
