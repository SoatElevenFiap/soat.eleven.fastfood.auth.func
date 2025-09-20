using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Soat.Eleven.FastFood.Domain.Entities;
using Soat.Eleven.FastFood.Domain.Enums;
using Soat.Eleven.FastFood.Domain.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Soat.Eleven.FastFood.Infra.Services;

public class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    private readonly IConfiguration _configuration = configuration;
    private const string _keyTokenAtendimento = "TokenAtendimento";

    public string GenerateToken(Usuario usuario)
    {
        var role = usuario.Perfil == PerfilUsuario.Administrador ? RolesAuthorization.Administrador : RolesAuthorization.Cliente;

        return GenerateToken([
            new (JwtRegisteredClaimNames.Name, usuario.Nome),
            new (JwtRegisteredClaimNames.Email, usuario.Email),
            new (JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new (ClaimTypes.Role, role)
        ]);
    }

    public string GenerateToken(Usuario usuario, string tokenAtendimento)
    {
        return GenerateToken([
            new (JwtRegisteredClaimNames.Name, usuario.Nome),
            new (JwtRegisteredClaimNames.Email, usuario.Email),
            new (JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new (_keyTokenAtendimento, tokenAtendimento),
            new (ClaimTypes.Role, RolesAuthorization.IdentificacaoTotem)
        ]);
    }

    public string GenerateToken(string tokenAtendimento)
    {
        return GenerateToken([
            new (_keyTokenAtendimento, tokenAtendimento),
            new (ClaimTypes.Role, RolesAuthorization.IdentificacaoTotem)
        ]);
    }

    private string GenerateToken(IEnumerable<Claim> claims)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["SecretKeyPassword"]!);
        var expirationDate = DateTime.UtcNow.AddHours(2);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expirationDate,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
