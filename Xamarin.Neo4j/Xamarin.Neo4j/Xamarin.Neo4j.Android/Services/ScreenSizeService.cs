//
// ScreenSizeService.cs
//
// © Xamarin.Neo4j.Android
//

using Xamarin.Neo4j.Services.Interfaces;

namespace Xamarin.Neo4j.Android.Services
{
    public class ScreenSizeService : IScreenSizeService
    {
        public int GetScreenHeight()
        {
            var metrics = global::Android.App.Application.Context.Resources.DisplayMetrics;
            return (int)(metrics.HeightPixels / metrics.Density);
        }

        public int GetScreenWidth()
        {
            var metrics = global::Android.App.Application.Context.Resources.DisplayMetrics;
            return (int)(metrics.WidthPixels / metrics.Density);
        }
    }
}
