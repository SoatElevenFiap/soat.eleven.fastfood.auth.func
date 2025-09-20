using Soat.Eleven.FastFood.Domain.Entities;
using System.Security.Claims;

namespace Soat.Eleven.FastFood.Domain.Services;

public interface IJwtTokenService
{
    /// <summary>
    /// Gera JWT Token para usuário vindo de login: Administrador/Cliente
    /// </summary>
    /// <param name="usuario"></param>
    /// <returns>JWT (string)</returns>
    string GenerateToken(Usuario usuario);
    /// <summary>
    /// Gera JWT Token para usuário vindo identificação no atendimento por CPF
    /// </summary>
    /// <param name="usuario"></param>
    /// <returns>JWT (string)</returns>
    string GenerateToken(Usuario usuario, string tokenAtendimento);
    /// <summary>
    /// Gera JWT Token para usuário vindo sem identificação no atendimento
    /// </summary>
    /// <param name="usuario"></param>
    /// <returns>JWT (string)</returns>
    string GenerateToken(string tokenAtendimento);
}
