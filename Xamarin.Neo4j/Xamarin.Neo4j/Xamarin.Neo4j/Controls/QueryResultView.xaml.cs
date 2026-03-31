using System;
using System.IO;
using System.Reflection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Xamarin.Neo4j.Managers;
using Xamarin.Neo4j.Services;
using Xamarin.Neo4j.Models;
using Xamarin.Neo4j.Utilities;

namespace Xamarin.Neo4j.Controls
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class QueryResultView : ContentView
    {
        public static readonly BindableProperty GraphViewHeightProperty =
            BindableProperty.Create(nameof(GraphViewHeight), typeof(double), typeof(QueryResultView), 300.0);

        public double GraphViewHeight
        {
            get => (double)GetValue(GraphViewHeightProperty);
            set => SetValue(GraphViewHeightProperty, value);
        }

        private QueryResult QueryResult => (QueryResult)BindingContext;

        public QueryResultView()
        {
            InitializeComponent();
            BindingContextChanged += OnBindingContextChanged;
            graphView.Navigating += OnGraphViewNavigating;
            App.ThemeChanged += OnThemeChanged;
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            if (QueryResult == null || graphView == null) return;
            ParseNeovisHtmlSafe();
            graphView.Source = new HtmlWebViewSource { Html = QueryResult?.NeovisHtml };
        }

        private async void OnGraphViewNavigating(object sender, WebNavigatingEventArgs e)
        {
            if (!e.Url.Contains("expand") || !e.Url.Contains("nodeId")) return;

            e.Cancel = true;

            var connectionString = ConnectionStringManager.ActiveConnectionString;
            if (connectionString == null) return;

            try
            {
                var neo4jService = IPlatformApplication.Current.Services.GetRequiredService<Neo4jService>();

                var match = System.Text.RegularExpressions.Regex.Match(e.Url, @"nodeId=(\d+)");
                if (!match.Success || !long.TryParse(match.Groups[1].Value, out var nodeId)) return;

                var result = await neo4jService.ExpandNode(nodeId, connectionString);

                if (!result.Success || result.Results == null) return;

                var (nodesJson, edgesJson) = GraphDataHelper.BuildJson(result.Results, connectionString.Id);

                var js = $"window.addGraphData({nodesJson}, {edgesJson}, {nodeId})";
                await graphView.EvaluateJavaScriptAsync(js);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Graph] Inline expand failed: {ex.Message}");
            }
        }

        private void OnBindingContextChanged(object sender, EventArgs e)
        {
            if (BindingContext == null || graphView == null) return;

            ParseNeovisHtmlSafe();

            graphView.Source = new HtmlWebViewSource { Html = QueryResult?.NeovisHtml };
        }

        private void ParseNeovisHtml()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Xamarin.Neo4j.Visualization.visgraph.html";

            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                var available = string.Join(", ", assembly.GetManifestResourceNames());
                QueryResult.NeovisHtml = $"<html><body style='background:#111;color:#f55;font-family:monospace;padding:16px'>" +
                                         $"<b>Resource not found:</b><br>{resourceName}<br><br>" +
                                         $"<b>Available:</b><br>{available}</body></html>";
                return;
            }

            using (stream)
            using (var reader = new StreamReader(stream))
            {
                var html = reader.ReadToEnd();
                var connectionId = ConnectionStringManager.ActiveConnectionString?.Id ?? Guid.Empty;

                var (nodesJson, edgesJson) = GraphDataHelper.BuildJson(QueryResult.Results, connectionId);

                var isDark = Application.Current.RequestedTheme == AppTheme.Dark;
                html = html.Replace("{{nodes}}", nodesJson);
                html = html.Replace("{{edges}}", edgesJson);
                html = html.Replace("{{backgroundColor}}", isDark ? "#292C31" : "#FFFFFF");
                html = html.Replace("{{textColor}}", isDark ? "#FFFFFF" : "#000000");

                QueryResult.NeovisHtml = html;
            }
        }

        private void ParseNeovisHtmlSafe()
        {
            if (QueryResult == null) return;
            try
            {
                ParseNeovisHtml();
            }
            catch (Exception ex)
            {
                QueryResult.NeovisHtml = $"<html><body style='background:#111;color:#f55;font-family:monospace;padding:16px'>" +
                                         $"<b>{ex.GetType().Name}</b><br>{ex.Message}</body></html>";
            }
        }
    }
}
