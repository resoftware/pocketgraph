//
// SessionViewModel.cs
//
// Trevi Awater
// 13-11-2021
//
// © Xamarin.Neo4j
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Xamarin.Neo4j.Annotations;
using Xamarin.Neo4j.Managers;
using Xamarin.Neo4j.Models;
using Xamarin.Neo4j.Pages;
using Xamarin.Neo4j.Services;
using Xamarin.Neo4j.Services.Interfaces;
using Xamarin.Neo4j.Utilities;

namespace Xamarin.Neo4j.ViewModels
{
    public class SessionViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler ScrollToTop;

        private readonly Neo4jService _neo4jService;

        private readonly IScreenSizeService _screenSizeService;

        private Database _currentDatabase;

        private List<Database> _availableDatabases;

        private ObservableCollection<QueryResult> _queryResults;

        private List<Query> _savedQueries;

        private Neo4jConnectionString _connectionString;

        private string _query;

        public SessionViewModel(INavigation navigation, Neo4jConnectionString connectionString, string initialQuery) : base(navigation)
        {
            _connectionString = connectionString;

            _neo4jService = IPlatformApplication.Current.Services.GetRequiredService<Neo4jService>();
            _screenSizeService = IPlatformApplication.Current.Services.GetRequiredService<IScreenSizeService>();

            Query = initialQuery;
            QueryResults = new ObservableCollection<QueryResult>();
            QueryResults.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(HasResults));
            };

            Commands.Add("ExecuteQuery", new Command(async () =>
            {
                if (CanExecuteQuery == false)
                    return;

                _connectionString.Database = CurrentDatabase.Name;

                var result = await _neo4jService.ExecuteQuery(Query, _connectionString);

                Query = null;
                QueryResults.Insert(0, result);
                ScrollToTop?.Invoke(this, EventArgs.Empty);
            }));

            Commands.Add("DeleteQuery", new Command(async (o) =>
            {
                if (!(o is Query query))
                    return;

                var confirmed = await Application.Current.Windows[0].Page.DisplayAlertAsync(
                    "Delete Query",
                    $"Delete \"{query.Name}\"?",
                    "Delete", "Cancel");

                if (confirmed)
                {
                    SavedQueryManager.DeleteSavedQuery(query);
                    LoadSavedQueries();
                }
            }));

            Commands.Add("LoadQuery", new Command((o) =>
            {
                if (o is Query query)
                    LoadQuery(query);
            }));

            Commands.Add("LoadResultQuery", new Command((o) =>
            {
                if (o is QueryResult result)
                    Query = result.Query;
            }));

            Commands.Add("DeleteResult", new Command((o) =>
            {
                if (o is QueryResult result)
                    DeleteQueryResult(result);
            }));

            Commands.Add("SaveQuery", new Command(async (o) =>
            {
                if (!(o is QueryResult result)) return;
                var name = await Application.Current.Windows[0].Page.DisplayPromptAsync(
                    "Save Query", "What would you like to call this query?");
                if (!string.IsNullOrWhiteSpace(name))
                {
                    SavedQueryManager.AddSavedQuery(new Query
                    {
                        Id = Guid.NewGuid(),
                        QueryText = result.Query,
                        Name = name
                    });
                    LoadSavedQueries();
                }
            }));

            Commands.Add("OpenGraph", new Command(async (o) =>
            {
                if (!(o is QueryResult result) || !result.CanDisplayGraph || result.NeovisHtml == null) return;
                var connectionString2 = ConnectionStringManager.ActiveConnectionString;
                await Navigation.PushAsync(new GraphPage(result.NeovisHtml, connectionString2, _neo4jService));
            }));

            Commands.Add("OpenTable", new Command(async (o) =>
            {
                if (!(o is QueryResult result) || !result.Success) return;
                await Navigation.PushAsync(new TablePage(result));
            }));

            Commands.Add("ClearResults", new Command(() => ClearAllResults()));

            InitializeConnection(connectionString);
        }

        public void LoadSavedQueries()
        {
            SavedQueries = SavedQueryManager.GetSavedQueries();
        }

        public void LoadQuery(Query query)
        {
            Query = query.QueryText;
        }

        private async void InitializeConnection(Neo4jConnectionString connectionString)
        {
            var (isConnected, message) = await _neo4jService.EstablishConnection(connectionString);

            if (!isConnected)
            {
                await Application.Current.Windows[0].Page.DisplayAlertAsync("", message, "OK");

                return;
            }

            AvailableDatabases = await _neo4jService.LoadDatabases();

            if (!string.IsNullOrWhiteSpace(connectionString.Database))
                CurrentDatabase = AvailableDatabases.SingleOrDefault(ad => ad.Name == connectionString.Database);

            if (CurrentDatabase == null)
                CurrentDatabase = AvailableDatabases.SingleOrDefault(ad => ad.Default) ?? AvailableDatabases.FirstOrDefault();
        }

        public void DeleteQueryResult(QueryResult queryResult)
        {
            var item = QueryResults.FirstOrDefault(qr => qr.Id == queryResult.Id);
            if (item != null)
                QueryResults.Remove(item);
        }

        public void ClearAllResults()
        {
            QueryResults.Clear();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Bindable Properties

        public Database CurrentDatabase
        {
            get => _currentDatabase;

            set
            {
                _currentDatabase = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(CanExecuteQuery));
            }
        }

        public List<Database> AvailableDatabases
        {
            get => _availableDatabases;

            set
            {
                _availableDatabases = value;

                OnPropertyChanged();
            }
        }

        public ObservableCollection<QueryResult> QueryResults
        {
            get => _queryResults;

            set
            {
                _queryResults = value;

                OnPropertyChanged();
            }
        }

        public string Query
        {
            get => _query;

            set
            {
                _query = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(CanExecuteQuery));
            }
        }

        public List<Query> SavedQueries
        {
            get => _savedQueries;

            set
            {
                _savedQueries = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSavedQueries));
                OnPropertyChanged(nameof(HasNoSavedQueries));
            }
        }

        public bool CanExecuteQuery => !string.IsNullOrWhiteSpace(Query) && CurrentDatabase != null;

        public bool IsEmpty => QueryResults?.Count == 0;

        public bool HasResults => !IsEmpty;

        public bool HasSavedQueries => _savedQueries?.Count > 0;

        public bool HasNoSavedQueries => !HasSavedQueries;

        public double GraphViewHeight
        {
            get
            {
                var screenHeight = _screenSizeService.GetScreenHeight();
                return Math.Max(120, (screenHeight - 300) / 2);
            }
        }

        #endregion
    }
}
