using Soat.Eleven.FastFood.Domain.Entities;

namespace Soat.Eleven.FastFood.Domain.Repositories;

public interface ITokenAtendimentoRepository
{
    Task<TokenAtendimento?> GetTokenByIdAsync(Guid tokenId);
    Task<TokenAtendimento?> GetMostRecentTokenByCpfAsync(string cpf);
}
