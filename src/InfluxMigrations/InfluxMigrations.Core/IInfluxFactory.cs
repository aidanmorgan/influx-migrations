using InfluxDB.Client;

namespace InfluxMigrations.Core;

public interface IInfluxFactory
{
    IInfluxDBClient Create();

    IInfluxFactory WithToken(string token);
    IInfluxFactory WithHost(string host);
}

public class InfluxFactory : IInfluxFactory
{
    private string _host = "http://localhost:8086";
    private string _token = "default-token";  // this has to be set to a non-empty string to let influx work
    private IInfluxDBClient? _client;

    public InfluxFactory() {}

    public InfluxFactory(string host, string token)
    {
        this._host = host;
        this._token = token;
    }
    
    public IInfluxDBClient Create()
    {
        if (_client != null)
        {
            return _client;
        }
        
        _client = new InfluxDBClient(new InfluxDBClientOptions(_host)
        {
            Token = _token
        });

        return _client;
    }

    public IInfluxFactory WithToken(string token)
    {
        this._token = token;
        this._client = null;
        
        return this;
    }

    public IInfluxFactory WithHost(string host)
    {
        this._host = host;
        this._client = null;
        
        return this;
    }
}