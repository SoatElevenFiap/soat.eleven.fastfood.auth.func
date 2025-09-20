using Dapper;
using Soat.Eleven.FastFood.Domain.Entities;
using Soat.Eleven.FastFood.Domain.Repositories;
using Soat.Eleven.FastFood.Infra.Context;
using System.Data.SqlTypes;

namespace Soat.Eleven.FastFood.Infra.Repositories;

public class TokenAtendimentoRepository(DataContext context) : ITokenAtendimentoRepository
{
    private readonly DataContext _context = context;

    public async Task<TokenAtendimento?> GetMostRecentTokenByCpfAsync(string cpf)
    {
        string query = @"
            SELECT *
            FROM TokenAtendimento
            WHERE Cpf = @cpf";

        var parameters = new { cpf };

        try
        {
            return await _context.Connection.QueryFirstOrDefaultAsync<TokenAtendimento>(query, parameters);
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

    public async Task<TokenAtendimento?> GetTokenByIdAsync(Guid tokenId)
    {
        string query = @"
            SELECT *
            FROM TokenAtendimento
            WHERE TokenId = @tokenId";

        var parameters = new { tokenId };

        try
        {
            return await _context.Connection.QueryFirstOrDefaultAsync<TokenAtendimento>(query, parameters);
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
