using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Xamarin.Neo4j.Managers;
using Xamarin.Neo4j.Models;
using Xamarin.Neo4j.Services;
using Xamarin.Neo4j.Utilities;
using Xamarin.Neo4j.ViewModels;

namespace Xamarin.Neo4j.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class GraphPage : ContentPage
    {
        public GraphPage(string html, Neo4jConnectionString connectionString = null, Neo4jService neo4jService = null)
        {
            InitializeComponent();

            BindingContext = new GraphViewModel(Navigation, html);

            webView.Navigating += OnWebViewNavigating;
        }

        private async void OnWebViewNavigating(object sender, WebNavigatingEventArgs e)
        {
            Console.WriteLine($"[Graph] Navigating: {e.Url}");

            if (e.Url.Contains("interaction"))
            {
                e.Cancel = true;
                hintBorder.IsVisible = false;
                return;
            }

            if (!e.Url.Contains("expand") || !e.Url.Contains("nodeId")) return;

            e.Cancel = true;

            try
            {
                var connectionString = ConnectionStringManager.ActiveConnectionString;
                if (connectionString == null)
                {
                    Console.WriteLine("[Graph] Expand skipped: no active connection");
                    return;
                }

                var neo4jService = IPlatformApplication.Current.Services.GetRequiredService<Neo4jService>();

                var match = System.Text.RegularExpressions.Regex.Match(e.Url, @"nodeId=(\d+)");
                if (!match.Success || !long.TryParse(match.Groups[1].Value, out var nodeId)) return;

                Console.WriteLine($"[Graph] Expanding node {nodeId}");

                var result = await neo4jService.ExpandNode(nodeId, connectionString);

                if (!result.Success || result.Results == null)
                {
                    Console.WriteLine($"[Graph] Expand query failed: {result.ErrorMessage}");
                    return;
                }

                var (nodesJson, edgesJson) = GraphDataHelper.BuildJson(result.Results, connectionString.Id);

                Console.WriteLine($"[Graph] Pushing {nodesJson.Length} chars nodes, {edgesJson.Length} chars edges");

                var js = $"window.addGraphData({nodesJson}, {edgesJson}, {nodeId})";
                await webView.EvaluateJavaScriptAsync(js);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Graph] Expand failed: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
