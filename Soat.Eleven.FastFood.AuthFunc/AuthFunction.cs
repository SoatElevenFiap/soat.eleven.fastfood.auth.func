using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Soat.Eleven.FastFood.Domain.Repositories;
using Soat.Eleven.FastFood.Domain.Services;
using System.Net;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

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
    [OpenApiOperation(operationId: "LoginUser", tags: new[] { "Autentica��o" }, Summary = "Autentica um usu�rio", Description = "Este endpoint autentica um usu�rio com base no email e senha fornecidos e retorna um token de acesso.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiRequestBody("application/json", typeof(LoginRequest), Description = "Credenciais de acesso do usu�rio (email e senha).")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Summary = "Autentica��o bem-sucedida", Description = "Retorna o token de acesso.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Summary = "N�o autorizado", Description = "As credenciais fornecidas s�o inv�lidas.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Requisi��o inv�lida", Description = "Os dados enviados est�o em um formato inv�lido.")]
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
            var usuario = await _usuarioRepository.LoginAsync(email, Common.GeneratePassword(password, _configuration["SALT_KEY_PASSWORK"]!));

            // Verifica se o usu�rio foi encontrado
            if (usuario is null)
            {
                _logger.LogWarning("Usu�rio n�o encontrado para o email: {email}", email);
                return new UnauthorizedObjectResult("Usu�rio n�o encontrado ou credenciais inv�lidas.");
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
    [OpenApiOperation(operationId: "LoginGuestOrByCpf", tags: new[] { "Autentica��o" }, Summary = "Autentica um usu�rio an�nimo ou por CPF", Description = "Este endpoint gera um token de acesso para um usu�rio convidado. Opcionalmente, um CPF pode ser fornecido para vincular a sess�o a um usu�rio existente.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "cpf", In = ParameterLocation.Path, Required = false, Type = typeof(string), Summary = "CPF do usu�rio", Description = "CPF opcional para identificar o usu�rio. Se n�o for fornecido, a autentica��o ser� an�nima.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Summary = "Autentica��o bem-sucedida", Description = "Retorna o token de acesso.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "O CPF fornecido n�o corresponde a nenhum usu�rio no sistema.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "O formato do CPF � inv�lido.")]
    public async Task<IActionResult> RunAtendimento([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/atendimento/{cpf?}")] HttpRequest req, string? cpf)
    {
        try
        {
            _logger.LogInformation("Iniciando autentica��o para atendimento. CPF: {cpf}", cpf ?? "n�o informado");

            if (!string.IsNullOrEmpty(cpf))
            {
                // Limpa o CPF, removendo pontos e tra�os
                cpf = Common.CleanCpf(cpf);

                // Valida o formato e o c�lculo do CPF
                if (!Common.IsCpfValido(cpf))
                {
                    _logger.LogWarning("CPF inv�lido fornecido: {cpf}", cpf);
                    return new BadRequestObjectResult("CPF inv�lido");
                }

                var cliente = await _usuarioRepository.GetClienteByCPF(cpf);
                var tokenAtendimento = await _tokenAtendimentoRepository.GenerateTokenAsync(cliente, cpf);
                
                _logger.LogInformation("Cliente encontrado pelo CPF: {cpf}", cpf);
                    
                var token = _jwtTokenService.GenerateToken(tokenAtendimento.TokenId.ToString());
                return new OkObjectResult(token);
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
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}