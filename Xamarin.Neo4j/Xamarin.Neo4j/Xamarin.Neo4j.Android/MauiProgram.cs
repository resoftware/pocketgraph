using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Xamarin.Neo4j.Android.CustomRenderers;
using Xamarin.Neo4j.Android.Services;
using Xamarin.Neo4j.Services;
using Xamarin.Neo4j.Services.Interfaces;

namespace Xamarin.Neo4j.Android
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("iconize-fontawesome-brands.ttf", "FontAwesome5Brands-Regular");
                    fonts.AddFont("iconize-fontawesome-solid.ttf", "FontAwesome5Free-Solid");
                    fonts.AddFont("iconize-fontawesome-regular.ttf", "FontAwesome5Free-Regular");
                    fonts.AddFont("roboto-mono-regular.ttf", "RobotoMono");
                })
                .ConfigureMauiHandlers(handlers =>
                {
                    handlers.AddHandler<Controls.QueryEditor, QueryEditorHandler>();
                });

            builder.Services.AddSingleton<ITrustManagerService, TrustManagerService>();
            builder.Services.AddSingleton<IVersionService, VersionService>();
            builder.Services.AddSingleton<IScreenSizeService, ScreenSizeService>();
            builder.Services.AddSingleton<Neo4jService>();

            return builder.Build();
        }
    }
}
