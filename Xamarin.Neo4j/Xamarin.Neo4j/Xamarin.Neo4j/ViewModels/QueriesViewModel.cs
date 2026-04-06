//
// QueriesViewModel.cs
//
// Trevi Awater
// 13-11-2021
//
// © Xamarin.Neo4j
//

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;
using Xamarin.Neo4j.Annotations;
using Xamarin.Neo4j.Managers;
using Xamarin.Neo4j.Models;
using Xamarin.Neo4j.Pages;

namespace Xamarin.Neo4j.ViewModels
{
    public class QueriesViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private IEnumerable<Query> _queries;

        public QueriesViewModel(INavigation navigation) : base(navigation)
        {
            Commands.Add("DeleteQuery", new Command((o) =>
            {
                if (!(o is Query query))
                    return;

                SavedQueryManager.DeleteSavedQuery(query);

                LoadQueries();
            }));
        }

        public async void StartSession(Query query)
        {
            if (ConnectionStringManager.ActiveConnectionString == null)
            {
                await Application.Current.Windows[0].Page.DisplayAlertAsync("", "Please select a connection before starting a session.", "OK");

                return;
            }

            await Navigation.PushAsync(new SessionPage(ConnectionStringManager.ActiveConnectionString, query.QueryText));
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void LoadQueries()
        {
            Queries = SavedQueryManager.GetSavedQueries();
        }

        #region Bindable Properties

        public IEnumerable<Query> Queries
        {
            get => _queries;

            set
            {
                _queries = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(HasItems));
            }
        }

        public bool IsEmpty => !(_queries?.Any() ?? false);

        public bool HasItems => !IsEmpty;

        #endregion
    }
}
