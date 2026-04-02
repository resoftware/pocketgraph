//
// ConnectionStringManager.cs
//
// Trevi Awater
// 13-11-2021
//
// © Xamarin.Neo4j
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using Newtonsoft.Json;
using Xamarin.Neo4j.Models;

namespace Xamarin.Neo4j.Managers
{
    public class ConnectionStringManager
    {
        private const string ConnectionStringsKey = "connection_strings";

        public static Neo4jConnectionString ActiveConnectionString { get; set; }

        public static async Task<List<Neo4jConnectionString>> GetConnectionStrings()
        {
            var json = await SecureStorage.Default.GetAsync(ConnectionStringsKey);

            if (string.IsNullOrEmpty(json))
                return new List<Neo4jConnectionString>();

            return JsonConvert.DeserializeObject<List<Neo4jConnectionString>>(json);
        }

        public static async Task AddConnectionString(Neo4jConnectionString connectionString)
        {
            var connectionStrings = await GetConnectionStrings();

            connectionStrings.Add(connectionString);

            await SaveConnectionStrings(connectionStrings);
        }

        private static async Task SaveConnectionStrings(IEnumerable<Neo4jConnectionString> connectionStrings)
        {
            var json = JsonConvert.SerializeObject(connectionStrings);

            await SecureStorage.Default.SetAsync(ConnectionStringsKey, json);
        }

        public static async Task DeleteConnectionString(Neo4jConnectionString neo4JConnectionString)
        {
            var connectionStrings = await GetConnectionStrings();

            connectionStrings = connectionStrings.Where(cs => cs.Id != neo4JConnectionString.Id)
                .ToList();

            await SaveConnectionStrings(connectionStrings);
        }

        public static async Task UpdateConnectionString(Guid id, Neo4jConnectionString connectionString)
        {
            var connectionStrings = await GetConnectionStrings();

            var connectionStringToUpdate = connectionStrings.FirstOrDefault(cs => cs.Id == id);

            if (connectionStringToUpdate == null)
                return;

            connectionStringToUpdate.Name = connectionString.Name;
            connectionStringToUpdate.Scheme = connectionString.Scheme;
            connectionStringToUpdate.Host = connectionString.Host;
            connectionStringToUpdate.Username = connectionString.Username;
            connectionStringToUpdate.Password = connectionString.Password;

            await SaveConnectionStrings(connectionStrings);
        }
    }
}
