using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Xamarin.Neo4j.Fonts;
using Xamarin.Neo4j.Models;
using Xamarin.Neo4j.ViewModels;

namespace Xamarin.Neo4j.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SessionPage : ContentPage
    {
        private SessionViewModel ViewModel => (SessionViewModel)BindingContext;

        public SessionPage(Neo4jConnectionString connectionString, string initialQuery = null)
        {
            InitializeComponent();

            BindingContext = new SessionViewModel(Navigation, connectionString, initialQuery);

            ViewModel.ScrollToTop += async (_, _) =>
            {
                await mainScroll.ScrollToAsync(0, 0, true);
            };
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ViewModel.LoadSavedQueries();
        }

        private void FocusDatabasePicker(object sender, EventArgs e)
        {
            databasePicker.Focus();
        }

        private void ExecuteQuery(object sender, EventArgs e)
        {
            ViewModel.Commands["ExecuteQuery"].Execute(null);
        }

        private bool _savedQueriesExpanded = true;

        private void ToggleSavedQueries(object sender, EventArgs e)
        {
            _savedQueriesExpanded = !_savedQueriesExpanded;
            savedQueriesContent.IsVisible = _savedQueriesExpanded;
            savedQueriesChevron.Text = _savedQueriesExpanded
                ? FontAwesomeSolid.ChevronUp
                : FontAwesomeSolid.ChevronDown;
        }

        private void ToggleResultCollapse(object sender, EventArgs e)
        {
            if (sender is not Button btn) return;

            // Walk up to find the StackLayout that wraps action row + query + content
            var card = btn.Parent?.Parent as StackLayout; // btn -> Grid -> StackLayout
            if (card == null) return;

            // The collapsible content is the last child (QueryResultView)
            var content = card.Children[card.Children.Count - 1] as View;
            // The query label is second-to-last
            var queryLabel = card.Children.Count >= 2 ? card.Children[card.Children.Count - 2] as View : null;

            if (content == null) return;

            var collapse = content.IsVisible;
            content.IsVisible = !collapse;
            if (queryLabel != null) queryLabel.IsVisible = !collapse;
            btn.Text = collapse ? FontAwesomeSolid.ChevronDown : FontAwesomeSolid.ChevronUp;
        }
    }
}
