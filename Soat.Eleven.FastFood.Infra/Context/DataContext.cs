using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

namespace Soat.Eleven.FastFood.Infra.Context;

public class DataContext
{
    public DataContext(IConfiguration configuration)
    {
        var connectionString = Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTIONSTRING")
            ?? configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        Connection = new NpgsqlConnection(connectionString);
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
}
