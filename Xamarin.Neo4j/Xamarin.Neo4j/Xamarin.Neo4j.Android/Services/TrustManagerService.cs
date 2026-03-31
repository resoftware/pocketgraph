//
// TrustManagerService.cs
//
// © Xamarin.Neo4j.Android
//

using Neo4j.Driver;
using Xamarin.Neo4j.Services.Interfaces;

namespace Xamarin.Neo4j.Android.Services
{
    public class TrustManagerService : ITrustManagerService
    {
        public TrustManager GetNativeTrustManager()
        {
            return new NativeTrustManager();
        }
    }
}
