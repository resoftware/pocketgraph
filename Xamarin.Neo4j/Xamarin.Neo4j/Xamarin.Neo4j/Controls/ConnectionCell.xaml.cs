using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace Xamarin.Neo4j.Controls
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConnectionCell : ViewCell
    {
        public ConnectionCell()
        {
            InitializeComponent();
        }
    }
}
