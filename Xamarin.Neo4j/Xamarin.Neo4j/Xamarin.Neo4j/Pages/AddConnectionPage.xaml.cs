using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Xamarin.Neo4j.Fonts;
using Xamarin.Neo4j.Models;
using Xamarin.Neo4j.ViewModels;

namespace Xamarin.Neo4j.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AddConnectionPage : ContentPage
    {
        private bool _passwordVisible = false;

        public AddConnectionPage(Neo4jConnectionString neo4JConnectionString = null)
        {
            InitializeComponent();

            BindingContext = new AddConnectionViewModel(Navigation, neo4JConnectionString);
        }

        private void TogglePasswordVisibility(object sender, System.EventArgs e)
        {
            _passwordVisible = !_passwordVisible;
            passwordEntry.IsPassword = !_passwordVisible;
            eyeButton.Text = _passwordVisible ? FontAwesomeSolid.EyeSlash : FontAwesomeSolid.Eye;
        }
    }
}
