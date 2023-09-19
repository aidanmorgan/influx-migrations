﻿using DotNet.Testcontainers;
using InfluxDB.Client;
using InfluxMigrations.Core;
using Microsoft.Extensions.Logging;
using Testcontainers.InfluxDb;

namespace InfluxMigrations.IntegrationCommon;

public class InfluxFixture
{
    private InfluxDbContainer? _container;
    private ILogger? _logger;

    public async Task<IInfluxFactory> Setup()
    {
        _logger = ConsoleLogger.Instance;

        var builder = new InfluxDbBuilder()
            .WithBucket(InfluxConstants.Bucket)
            .WithOrganization(InfluxConstants.Organisation)
            .WithRetention(InfluxConstants.Retention)
            .WithPassword(InfluxConstants.Password)
            .WithUsername(InfluxConstants.Username)
            .WithAdminToken(InfluxConstants.AdminToken)
            .WithPortBinding(8086, 8086)
            .WithAutoRemove(true)
            .WithCleanUp(true)
            .WithImage("influxdb:latest");

        _container = builder.Build();
        await _container.StartAsync();

        return new InfluxFactory().WithHost(_container.GetAddress()).WithToken(InfluxConstants.AdminToken);
    }

    public async Task TearDown()
    {
        if (_container != null)
        {
            await _container.StopAsync();
        }
    }
    
}