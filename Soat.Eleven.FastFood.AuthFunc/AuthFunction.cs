using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Soat.Eleven.FastFood.Domain.Repositories;
using Soat.Eleven.FastFood.Domain.Services;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Soat.Eleven.FastFood.AuthFunc
{
    public class AuthFunction(ILogger<AuthFunction> logger,
                              IUsuarioRepostiory usuarioRepostiory,
                              IJwtTokenService jwtTokenService)
    {
        private readonly ILogger<AuthFunction> _logger = logger;
        private readonly IUsuarioRepostiory _usuarioRepostiory = usuarioRepostiory;
        private readonly IJwtTokenService _jwtTokenService = jwtTokenService;

        [Function("auth")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
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
            var usuario = await _usuarioRepostiory.LoginAsync(email, password);

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

        private class LoginRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Senha { get; set; } = string.Empty;
        }
    }
}

//https://learn.microsoft.com/en-us/azure/azure-functions/functions-how-to-github-actions?tabs=windows%2Cdotnet&pivots=method-manual