using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;

namespace Soat.Eleven.FastFood.Infra.Context;

public class DataContext
{
    public DataContext(IConfiguration configuration, ILogger<DataContext> logger)
    {
        _logger = logger;
        
        var connectionString = Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTIONSTRING")
            ?? configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING");

        _logger.LogInformation("Connection string obtida com sucesso - {string}", connectionString ?? "Não encontrado");

        Connection = new NpgsqlConnection(connectionString ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."));
        OpenConnection();
    }

    private void OpenConnection()
    {
        if (Connection.State != ConnectionState.Open)
            Connection.Open();
    }

    public void DisposeConnection()
    {
        if (Connection.State != ConnectionState.Closed)
            Connection.Close();
    }

    public readonly IDbConnection Connection;
    private readonly ILogger<DataContext> _logger;
}
