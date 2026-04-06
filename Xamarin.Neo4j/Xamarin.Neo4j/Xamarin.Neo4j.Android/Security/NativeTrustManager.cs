//
// NativeTrustManager.cs
//
// © Xamarin.Neo4j.Android
//

using System;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Android.Runtime;
using Java.Security;
using Java.Security.Cert;
using Javax.Net.Ssl;
using Neo4j.Driver;

namespace Xamarin.Neo4j
{
    public class NativeTrustManager : TrustManager
    {
        private readonly IX509TrustManager _androidTrustManager;

        public NativeTrustManager()
        {
            var factory = TrustManagerFactory.GetInstance(TrustManagerFactory.DefaultAlgorithm);
            factory.Init((KeyStore)null);
            _androidTrustManager = factory.GetTrustManagers()
                .OfType<Java.Lang.Object>()
                .Select(m => m.JavaCast<IX509TrustManager>())
                .First(m => m != null);
        }

        public override bool ValidateServerCertificate(Uri uri, X509Certificate2 certificate, X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            try
            {
                // The Xamarin binding of CertificateFactory.GenerateCertificate accepts a System.IO.Stream
                var certFactory = CertificateFactory.GetInstance("X.509");
                using var stream = new MemoryStream(certificate.RawData);
                var javaCert = certFactory.GenerateCertificate(stream) as Java.Security.Cert.X509Certificate;

                _androidTrustManager.CheckServerTrusted(
                    new Java.Security.Cert.X509Certificate[] { javaCert }, "RSA");
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Certificate validation failed for {uri.Host}: {ex.Message}", ex);
            }
        }
    }
}
