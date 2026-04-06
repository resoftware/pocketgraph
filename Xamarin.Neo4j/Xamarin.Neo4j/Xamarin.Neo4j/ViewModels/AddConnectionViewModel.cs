//
// AddConnectionViewModel.cs
//
// Trevi Awater
// 13-11-2021
//
// © Xamarin.Neo4j
//

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Xamarin.Neo4j.Annotations;
using Xamarin.Neo4j.Managers;
using Xamarin.Neo4j.Models;
using Xamarin.Neo4j.Pages;
using Xamarin.Neo4j.Services;

namespace Xamarin.Neo4j.ViewModels
{
    public class AddConnectionViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private Guid? _id;

        private string _name, _scheme, _host, _username, _password;

        private readonly Neo4jService _neo4jService;

        public event PropertyChangedEventHandler PropertyChanged;

        public AddConnectionViewModel(INavigation navigation, Neo4jConnectionString neo4JConnectionString = null) : base(navigation)
        {
            if (neo4JConnectionString == null)
                InitializeDefaultValues();

            else
                InitializeValues(neo4JConnectionString);

            _neo4jService = IPlatformApplication.Current.Services.GetRequiredService<Neo4jService>();

            Commands.Add("Test", new Command(async () =>
            {
                var connectionString = BuildConnectionString();

                var (_, message) = await _neo4jService.EstablishConnection(connectionString);

                await Application.Current.Windows[0].Page.DisplayAlertAsync("", message, "OK");
            }));

            Commands.Add("Save", new Command(async () =>
            {
                var connectionString = BuildConnectionString();

                if (string.IsNullOrWhiteSpace(connectionString.Name))
                    connectionString.Name = connectionString.Host;

                if (_id.HasValue)
                    await ConnectionStringManager.UpdateConnectionString(_id.Value, connectionString);
                else
                    await ConnectionStringManager.AddConnectionString(connectionString);

                await Navigation.PopAsync();
            }));

            Commands.Add("Connect", new Command(async () =>
            {
                var connectionString = BuildConnectionString();

                var (couldConnect, message) = await _neo4jService.EstablishConnection(connectionString);

                if (couldConnect)
                    await Navigation.PushAsync(new SessionPage(connectionString));

                else
                    await Application.Current.Windows[0].Page.DisplayAlertAsync("", message, "OK");
            }));
        }

        private void InitializeValues(Neo4jConnectionString neo4JConnectionString)
        {
            _id = neo4JConnectionString.Id;

            Name = neo4JConnectionString.Name;
            Scheme = neo4JConnectionString.Scheme;
            Host = neo4JConnectionString.Host;
            Username = neo4JConnectionString.Username;
            Password = neo4JConnectionString.Password;
        }

        private void InitializeDefaultValues()
        {
            Username = "neo4j";
            Scheme = "neo4j://";
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private Neo4jConnectionString BuildConnectionString()
        {
            return new Neo4jConnectionString
            {
                Id = Guid.NewGuid(),
                Name = Name,
                Scheme = Scheme,
                Host = Host,
                Username = Username,
                Password = Password
            };
        }

        #region Bindable Properties

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public string Scheme
        {
            get => _scheme;

            set
            {
                _scheme = value;

                OnPropertyChanged(nameof(Scheme));
            }
        }

        public string Host
        {
            get => _host;

            set
            {
                _host = value;

                OnPropertyChanged(nameof(Host));
            }
        }

        public string Username
        {
            get => _username;

            set
            {
                _username = value;

                OnPropertyChanged(nameof(Username));
            }
        }

        public string Password
        {
            get => _password;

            set
            {
                _password = value;

                OnPropertyChanged(nameof(Password));
            }
        }

        #endregion
    }
}
