using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Newtonsoft.Json;
using Xamarin.Neo4j.Models;

namespace Xamarin.Neo4j.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TablePage : ContentPage
    {
        public TablePage(QueryResult queryResult)
        {
            InitializeComponent();
            LoadJson(queryResult);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            App.ThemeChanged += OnThemeChanged;
        }

        protected override void OnDisappearing()
        {
            App.ThemeChanged -= OnThemeChanged;
            base.OnDisappearing();
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            var isDark = Application.Current.RequestedTheme == AppTheme.Dark;
            jsonWebView.EvaluateJavaScriptAsync($"setTheme({(isDark ? "true" : "false")})");
        }

        private void LoadJson(QueryResult queryResult)
        {
            // Transpose column-oriented dict into a list of row objects
            var results = queryResult.Results;
            string json;
            if (results == null || results.Count == 0)
            {
                json = "[]";
            }
            else
            {
                var columns = results.Keys.ToList();
                var rowCount = results[columns[0]].Count;
                var rows = Enumerable.Range(0, rowCount)
                    .Select(i => columns.ToDictionary(c => c, c => results[c][i]))
                    .ToList();
                json = JsonConvert.SerializeObject(rows, Formatting.None);
            }

            var html = LoadTemplate(json);
            jsonWebView.Source = new HtmlWebViewSource { Html = html };
        }

        private static string LoadTemplate(string json)
        {
            var assembly = Assembly.GetExecutingAssembly();
            const string resourceName = "Xamarin.Neo4j.Visualization.jsonview.html";
            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                return $"<html><body>Resource not found: {resourceName}</body></html>";

            string html;
            using (stream)
            using (var reader = new StreamReader(stream))
                html = reader.ReadToEnd();

            var isDark = Application.Current.RequestedTheme == AppTheme.Dark;
            html = html.Replace("{{json}}", json);
            html = html.Replace("{{backgroundColor}}", isDark ? "#1e1e1e" : "#ffffff");
            html = html.Replace("{{textColor}}", isDark ? "#d4d4d4" : "#1a1a1a");
            html = html.Replace("{{toolbarBg}}", isDark ? "#252526" : "#f5f5f5");
            html = html.Replace("{{inputBg}}", isDark ? "#3c3c3c" : "#ffffff");
            html = html.Replace("{{borderColor}}", isDark ? "#454545" : "#cccccc");
            html = html.Replace("{{mutedColor}}", isDark ? "#808080" : "#8a8a8a");
            return html;
        }
    }
}
