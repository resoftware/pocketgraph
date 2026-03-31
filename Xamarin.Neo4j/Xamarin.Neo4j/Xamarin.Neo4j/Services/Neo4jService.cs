//
// Neo4jService.cs
//
// Trevi Awater
// 13-11-2021
//
// © Xamarin.Neo4j
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Neo4j.Driver;
using Neo4jClient;
using Neo4jClient.Cypher;
using Xamarin.Neo4j.Models;
using Xamarin.Neo4j.Security;
using Xamarin.Neo4j.Services;
using Xamarin.Neo4j.Services.Interfaces;
using Xamarin.Neo4j.Utilities;

namespace Xamarin.Neo4j.Services
{
    public class Neo4jService
    {
        private readonly TrustManager _nativeTrustManager;

        public Neo4jService()
        {
            var trustManagerService = IPlatformApplication.Current.Services.GetRequiredService<ITrustManagerService>();

            _nativeTrustManager = trustManagerService.GetNativeTrustManager();
        }

        private BoltGraphClient GraphClient { get; set; }

        public async Task<Tuple<bool, string>> EstablishConnection(Neo4jConnectionString connectionString)
        {
            try
            {
                var (url, isEncrypted, ignoreTrust) = connectionString.ParseHost();

                var driver = GraphDatabase.Driver(url,
                    AuthTokens.Basic(connectionString.Username, connectionString.Password), (config) =>
                    {
                        if (!ignoreTrust && DeviceInfo.Platform == DevicePlatform.iOS)
                            config.WithTrustManager(_nativeTrustManager);
                    });

                // Verify credentials with a real round-trip — this is the only reliable
                // way to catch auth failures, since the driver connects lazily.
                var verifySession = driver.AsyncSession();
                try
                {
                    var cursor = await verifySession.RunAsync("RETURN 1");
                    await cursor.ConsumeAsync();
                }
                finally
                {
                    await verifySession.CloseAsync();
                }

                GraphClient = new BoltGraphClient(driver);
            }

            catch (ServiceUnavailableException e)
            {
                return new Tuple<bool, string>(false, e.Message);
            }

            catch (AuthenticationException e)
            {
                return new Tuple<bool, string>(false, e.Message);
            }

            catch (SecurityException e)
            {
                return new Tuple<bool, string>(false, e.Message);
            }

            catch (UriFormatException e)
            {
                return new Tuple<bool, string>(false, e.Message);
            }

            catch (NotSupportedException e)
            {
                return new Tuple<bool, string>(false, e.Message);
            }

            catch (Exception e)
            {
                return new Tuple<bool, string>(false, $"[{e.GetType().FullName}] {e.Message}\n\n{e.InnerException?.Message}\n\n{e.StackTrace}");
            }

            return new Tuple<bool, string>(true, "Connection was successful!");
        }

        public async Task<List<Database>> LoadDatabases()
        {
            try
            {
                var session = GraphClient.Driver.AsyncSession(d => d.WithDatabase("system"));
                var cursor = await session.RunAsync("SHOW DATABASES;");

                var databases = new List<Database>();

                while (await cursor.FetchAsync())
                {
                    var name = cursor.Current.Values["name"].As<string>();
                    var status = cursor.Current.Values["currentStatus"].As<string>();
                    var @default = cursor.Current.Values["default"].As<bool>();

                    databases.Add(new Database()
                    {
                        Name = name,
                        Status = status,
                        Default = @default
                    });
                }

                return databases;
            }
            catch
            {
                // Memgraph and some Neo4j setups don't expose a system database.
                // Fall back to a single default database.
                return new List<Database>
                {
                    new Database { Name = "memgraph", Status = "online", Default = true }
                };
            }
        }

        public async Task<QueryResult> ExecuteQuery(string query, Neo4jConnectionString connectionString)
        {
            var results = new Dictionary<string, List<object>>();

            var canDisplayGraph = false;

            try
            {
                var session = GraphClient.Driver.AsyncSession(d => d.WithDatabase(connectionString.Database));
                var cursor = await session.RunAsync(query);

                while (await cursor.FetchAsync())
                {
                    foreach (var key in cursor.Current.Keys)
                        if (results.ContainsKey(key) == false)
                            results.Add(key, new List<object>());

                    foreach (var record in cursor.Current.Values)
                    {
                        var value = record.Value;

                        switch (value)
                        {
                            case INode _:
                                canDisplayGraph = true;
                                results[record.Key].Add(value.As<INode>());
                                break;

                            case IRelationship _:
                                results[record.Key].Add(value.As<IRelationship>());
                                break;

                            default:
                                results[record.Key].Add(value);
                                break;
                        }
                    }
                }

                return new QueryResult()
                {
                    Id = Guid.NewGuid(),
                    Success = true,
                    CanDisplayGraph = canDisplayGraph,
                    Query = query,
                    DisplayQuery = QueryHelper.ToDisplayQuery(query),
                    ConnectionString = connectionString,
                    Results = results
                };
            }

            catch (ClientException e)
            {
                return new QueryResult()
                {
                    Id = Guid.NewGuid(),
                    Success = false,
                    Query = query,
                    ConnectionString = connectionString,
                    ErrorMessage = e.Message
                };
            }
        }

        public async Task<QueryResult> ExpandNode(long nodeId, Neo4jConnectionString connectionString)
        {
            var query = "MATCH (n)-[r]-(m) WHERE id(n) = $nodeId RETURN n, r, m";

            var results = new Dictionary<string, List<object>>();

            try
            {
                var session = GraphClient.Driver.AsyncSession(d => d.WithDatabase(connectionString.Database));
                var cursor = await session.RunAsync(query, new { nodeId });

                while (await cursor.FetchAsync())
                {
                    foreach (var key in cursor.Current.Keys)
                        if (results.ContainsKey(key) == false)
                            results.Add(key, new List<object>());

                    foreach (var record in cursor.Current.Values)
                    {
                        var value = record.Value;

                        switch (value)
                        {
                            case INode _:
                                results[record.Key].Add(value.As<INode>());
                                break;

                            case IRelationship _:
                                results[record.Key].Add(value.As<IRelationship>());
                                break;

                            default:
                                results[record.Key].Add(value);
                                break;
                        }
                    }
                }

                return new QueryResult()
                {
                    Id = Guid.NewGuid(),
                    Success = true,
                    CanDisplayGraph = true,
                    Query = query,
                    ConnectionString = connectionString,
                    Results = results
                };
            }
            catch (ClientException e)
            {
                return new QueryResult()
                {
                    Id = Guid.NewGuid(),
                    Success = false,
                    Query = query,
                    ConnectionString = connectionString,
                    ErrorMessage = e.Message
                };
            }
        }
    }
}
