using Soat.Eleven.FastFood.Domain.Entities;

namespace Soat.Eleven.FastFood.Domain.Repositories;

public interface IUsuarioRepostiory
{
    Task<Usuario?> LoginAsync(string email, string password);
    Task<Cliente?> GetClienteByCPF(string cpf);
}
