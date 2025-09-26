using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Soat.Eleven.FastFood.Domain.Entities;
using Soat.Eleven.FastFood.Domain.Repositories;
using Soat.Eleven.FastFood.Domain.Services;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Soat.Eleven.FastFood.AuthFunc;

public class AuthFunction(ILogger<AuthFunction> logger,
                          IUsuarioRepostiory usuarioRepository,
                          IJwtTokenService jwtTokenService,
                          ITokenAtendimentoRepository tokenAtendimentoRepository,
                          IConfiguration configuration)
{
    private readonly ILogger<AuthFunction> _logger = logger;
    private readonly IUsuarioRepostiory _usuarioRepository = usuarioRepository;
    private readonly IJwtTokenService _jwtTokenService = jwtTokenService;
    private readonly ITokenAtendimentoRepository _tokenAtendimentoRepository = tokenAtendimentoRepository;
    private readonly IConfiguration _configuration = configuration;

    [Function("Authentication")]
    public async Task<IActionResult> RunAuthentication([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth")] HttpRequest req)
    {
        try
        {
            Console.WriteLine("Iniciando processamento da requisi��o de autentica��o.");
            _logger.LogInformation("Iniciando processamento da requisi��o de autentica��o.");

            string email;
            string password;

            // Verifica se � uma requisi��o POST com JSON
            if (req.ContentType is null || !req.ContentType.Contains("application/json"))
            {
                _logger.LogInformation("Requisi��o v�lida recebida.");
            }

            // L� o corpo da requisi��o
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            try
            {
                JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<LoginRequest>(requestBody, options);
                email = data?.Email ?? string.Empty;
                password = data?.Senha ?? string.Empty;
            }
            catch
            {
                return new BadRequestObjectResult("Formato de dados inv�lido.");
            }

            // Valida se email e senha foram informados
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                return new BadRequestObjectResult("Email e senha s�o obrigat�rios.");
            }

            // Tenta fazer login com as credenciais
            var usuario = await _usuarioRepository.LoginAsync(email, GeneratePassword(password));

            // Verifica se o usu�rio foi encontrado
            if (usuario is null)
            {
                _logger.LogWarning("Usu�rio n�o encontrado para o email: {email}", email);
                return new NotFoundObjectResult("Usu�rio n�o encontrado ou credenciais inv�lidas.");
            }

            // Usu�rio encontrado, retorna os dados
            _logger.LogInformation("Usu�rio autenticado com sucesso: {nome}", usuario!.Nome);
            var token = _jwtTokenService.GenerateToken(usuario);
            return new OkObjectResult(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar a requisi��o de autentica��o.");
            throw;
        }
    }

    [Function("AuthenticationAtendimento")]
    public async Task<IActionResult> RunAtendimento([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/atendimento/{cpf?}")] HttpRequest req, string? cpf)
    {
        try
        {
            _logger.LogInformation("Iniciando autentica��o para atendimento. CPF: {cpf}", cpf ?? "n�o informado");
            
            if (!string.IsNullOrEmpty(cpf))
            {
                var cliente = await _usuarioRepository.GetClienteByCPF(cpf);
                var tokenAtendimento = await _tokenAtendimentoRepository.GenerateTokenAsync(cliente, cpf);
                
                if (tokenAtendimento is not null)
                {
                    _logger.LogInformation("Cliente encontrado pelo CPF: {cpf}", cpf);
                    
                    var token = _jwtTokenService.GenerateToken(tokenAtendimento.TokenId.ToString());
                    return new OkObjectResult(token);
                }
                else
                {
                    _logger.LogInformation("Cliente com CPF {cpf} n�o encontrado, gerando token an�nimo", cpf);
                }
            }
            
            // Se chegou aqui, n�o tem CPF ou n�o encontrou o cliente
            // Gera um token para cliente n�o identificado
            _logger.LogInformation("Gerando token de atendimento an�nimo");
            var tokenAtendimentoAnonimo = await _tokenAtendimentoRepository.GenerateTokenAsync();
            var tokenAnonimo = _jwtTokenService.GenerateToken(tokenAtendimentoAnonimo.TokenId.ToString());

            return new OkObjectResult(tokenAnonimo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar a requisi��o de atendimento.");
            throw;
        }
    }

    private string GeneratePassword(string password)
    {
        var saltByte = Encoding.UTF8.GetBytes(_configuration["SALT_KEY_PASSWORK"]!);
        var hmacMD5 = new HMACMD5(saltByte);
        var passwordConvert = Encoding.UTF8.GetBytes(password!);
        return Convert.ToBase64String(hmacMD5.ComputeHash(passwordConvert));
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}