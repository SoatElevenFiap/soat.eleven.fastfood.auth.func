using Dapper;
using Soat.Eleven.FastFood.Domain.Entities;
using Soat.Eleven.FastFood.Domain.Repositories;
using Soat.Eleven.FastFood.Infra.Context;
using System.Data.SqlTypes;

namespace Soat.Eleven.FastFood.Infra.Repositories;

public class UsuarioRepository(DataContext context) : IUsuarioRepostiory
{
    private readonly DataContext _context = context;

    public async Task<Cliente?> GetClienteByCPF(string cpf)
    {
        string query = @"
            SELECT *
            FROM ""Clientes""
            WHERE ""Cpf"" = @cpf";

        var parameters = new { cpf };

        try
        {
            return await _context.Connection.QueryFirstOrDefaultAsync<Cliente>(query, parameters);
        }
        catch (Exception ex)
        {
            throw new SqlTypeException(ex.Message);
        }
        finally
        {
            _context.DisposeConnection();
        }
    }

    public async Task<Usuario?> LoginAsync(string email, string password)
    {
        string query = @"
            SELECT *
            FROM ""Usuarios""
            WHERE ""Email"" = @Email AND ""Senha"" = @Password";

        var parameters = new { Email = email, Password = password };

        try
        {
            return await _context.Connection.QueryFirstOrDefaultAsync<Usuario>(query, parameters);
        }
        catch (Exception ex)
        {
            throw new SqlTypeException(ex.Message);
        }
        finally
        {
            _context.DisposeConnection();
        }
    }
}
