//
// SettingsViewModel.cs
//
// Trevi Awater
// 13-01-2022
//
// © Xamarin.Neo4j
//

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using Xamarin.Neo4j.Annotations;
using Xamarin.Neo4j.Pages;
using Xamarin.Neo4j.Services.Interfaces;

namespace Xamarin.Neo4j.ViewModels
{
    public class SettingsViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private string _versionLabel { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private IVersionService _versionService { get; set; }

        public SettingsViewModel(INavigation navigation) : base(navigation)
        {
            _versionService = IPlatformApplication.Current.Services.GetRequiredService<IVersionService>();

            VersionLabel = $"Version: {_versionService.GetVersion()} (Build: {_versionService.GetBuild()})";

            Commands.Add("OpenReSoftwareSite", new Command(async () =>
            {
                await Launcher.Default.OpenAsync(new Uri("https://resoftware.nl/"));
            }));

            Commands.Add("OpenLicensesPage", new Command(async () =>
            {
               await Navigation.PushAsync(new LicensesPage());
            }));

            Commands.Add("ContactSupport", new Command(async () =>
            {
                var version = _versionService.GetVersion();
                var build = _versionService.GetBuild();
                var deviceModel = DeviceInfo.Model;
                var manufacturer = DeviceInfo.Manufacturer;
                var osVersion = DeviceInfo.VersionString;
                var platform = DeviceInfo.Platform.ToString();
                var deviceType = DeviceInfo.DeviceType.ToString();
                var appName = AppInfo.Name;

                var body = $"""


                            --- Device Information ---
                            App: {appName} v{version} (Build {build})
                            Device: {manufacturer} {deviceModel}
                            Platform: {platform} {osVersion}
                            Device Type: {deviceType}
                            """;

                var message = new EmailMessage
                {
                    To = ["support@resoftware.nl"],
                    Subject = $"{appName} - Support Request",
                    Body = body
                };

                if (!Email.Default.IsComposeSupported)
                {
                    await Application.Current.Windows[0].Page.DisplayAlertAsync(
                        "No Email App", "No email client is configured on this device.", "OK");
                    return;
                }

                await Email.Default.ComposeAsync(message);
            }));

            Commands.Add("RateApp", new Command(async () =>
            {
                var url = DeviceInfo.Platform == DevicePlatform.Android
                    ? "https://play.google.com/store/apps/details?id=nl.resoftware.pocketgraph"
                    : "https://apps.apple.com/app/id1604368926?action=write-review";

                await Launcher.Default.OpenAsync(new Uri(url));
            }));

            Commands.Add("ClearConnections", new Command(async () =>
            {
                var clear = await Application.Current.Windows[0].Page.DisplayAlertAsync(
                    "Clear Connections",
                    "Are you sure you want to remove all saved connections?",
                    "Clear", "Cancel");

                if (clear)
                {
                    SecureStorage.Default.Remove("connection_strings");
                }
            }));

            Commands.Add("ClearQueries", new Command(async () =>
            {
                var clear = await Application.Current.Windows[0].Page.DisplayAlertAsync(
                    "Clear Saved Queries",
                    "Are you sure you want to remove all saved queries?",
                    "Clear", "Cancel");

                if (clear)
                {
                    Preferences.Default.Remove("saved_queries");
                }
            }));
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Bindable Properties

        public string VersionLabel
        {
            get => _versionLabel;

            set
            {
                _versionLabel = value;

                OnPropertyChanged(nameof(VersionLabel));
            }
        }

        public bool AutoCapitalize
        {
            get => Preferences.Default.Get("auto_capitalize", true);
            set
            {
                Preferences.Default.Set("auto_capitalize", value);
                OnPropertyChanged(nameof(AutoCapitalize));
            }
        }

        public bool AutoZoom
        {
            get => Preferences.Default.Get("auto_zoom", true);
            set
            {
                Preferences.Default.Set("auto_zoom", value);
                OnPropertyChanged(nameof(AutoZoom));
            }
        }

        #endregion
    }
}
