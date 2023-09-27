using System.Reflection;
using Flurl.Http;
using InfluxDB.Client;
using InfluxDB.Client.Api.Client;

namespace InfluxMigrations.Core;

public interface IContext
{
    void Accept(IContextVisitor visitor);
    IInfluxDBClient Influx { get; }
}

public static class InfluxDbClientExtensions
{
    /// <summary>
    /// This is a DISGUSTING HACK to work around the fact that there are some API functions that are not exposed via the
    /// influx API library - however the alternatives are gross, so this attempts to find the InfluxClientOptions that are
    /// set in the base of the API and create a direct HTTP request (using Flurl.Http) to access the API endpoint via HTTP.
    /// </summary>
    public static IFlurlRequest Raw(this IInfluxDBClient client)
    {
        var apiClientField = typeof(InfluxDBClient).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(x => string.Equals(x.Name, "_apiClient"), null);
        if (apiClientField == null)
        {
            throw new MigrationException("Hack failed.");
        }

        var apiClient = (ApiClient?)apiClientField.GetValue(client);
        if (apiClient == null)
        {
            throw new MigrationException("Hack failed.");
        }

        var clientOptionsField = typeof(ApiClient).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(x => string.Equals(x?.Name, "_options"), null);
        if (clientOptionsField == null)
        {
            throw new MigrationException("Hack failed.");
        }

        var clientOptions = (InfluxDBClientOptions?)clientOptionsField.GetValue(apiClient);
        if (clientOptions == null)
        {
            throw new MigrationException("Hack failed.");
        }

        return clientOptions.Url
            .WithHeader("Authorization", $"Token {clientOptions.Token}")
            .WithHeader("Content-Type", "text/plain; charset=utf-8")
            .WithHeader("Accept", "application/json");
    }
}