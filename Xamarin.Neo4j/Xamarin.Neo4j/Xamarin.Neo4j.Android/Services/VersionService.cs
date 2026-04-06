//
// VersionService.cs
//
// © Xamarin.Neo4j.Android
//

using Android.OS;
using Xamarin.Neo4j.Services.Interfaces;

namespace Xamarin.Neo4j.Android.Services
{
    public class VersionService : IVersionService
    {
        public string GetVersion()
        {
            var context = global::Android.App.Application.Context;
            var info = context.PackageManager.GetPackageInfo(context.PackageName, 0);
            return info.VersionName;
        }

        public string GetBuild()
        {
            var context = global::Android.App.Application.Context;
            var info = context.PackageManager.GetPackageInfo(context.PackageName, 0);
#pragma warning disable CS0618, CA1416, CA1422
            return Build.VERSION.SdkInt >= BuildVersionCodes.P
                ? info.LongVersionCode.ToString()
                : info.VersionCode.ToString();
#pragma warning restore CS0618, CA1416, CA1422
        }
    }
}
