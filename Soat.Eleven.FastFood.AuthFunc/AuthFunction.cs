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
    [OpenApiOperation(operationId: "LoginUser", tags: new[] { "Autenticação" }, Summary = "Autentica um usuário", Description = "Este endpoint autentica um usuário com base no email e senha fornecidos e retorna um token de acesso.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiRequestBody("application/json", typeof(LoginRequest), Description = "Credenciais de acesso do usuário (email e senha).")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Summary = "Autenticação bem-sucedida", Description = "Retorna o token de acesso.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Summary = "Não autorizado", Description = "As credenciais fornecidas são inválidas.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Requisição inválida", Description = "Os dados enviados estão em um formato inválido.")]
    public async Task<IActionResult> RunAuthentication([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth")] HttpRequest req)
    {
        try
        {
            Console.WriteLine("Iniciando processamento da requisição de autenticação.");
            _logger.LogInformation("Iniciando processamento da requisição de autenticação.");

            string email;
            string password;

            // Verifica se é uma requisição POST com JSON
            if (req.ContentType is null || !req.ContentType.Contains("application/json"))
            {
                _logger.LogInformation("Requisição válida recebida.");
            }

            // Lê o corpo da requisição
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
                return new BadRequestObjectResult("Formato de dados inválido.");
            }

            // Valida se email e senha foram informados
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                return new BadRequestObjectResult("Email e senha são obrigatórios.");
            }

            // Tenta fazer login com as credenciais
            var usuario = await _usuarioRepository.LoginAsync(email, Common.GeneratePassword(password, _configuration["SALT_KEY_PASSWORK"]!));

            // Verifica se o usuário foi encontrado
            if (usuario is null)
            {
                _logger.LogWarning("Usuário não encontrado para o email: {email}", email);
                return new UnauthorizedObjectResult("Usuário não encontrado ou credenciais inválidas.");
            }

            // Usuário encontrado, retorna os dados
            _logger.LogInformation("Usuário autenticado com sucesso: {nome}", usuario!.Nome);
            var token = _jwtTokenService.GenerateToken(usuario);
            return new OkObjectResult(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar a requisição de autenticação.");
            throw;
        }
    }

    [Function("AuthenticationAtendimento")]
    [OpenApiOperation(operationId: "LoginGuestOrByCpf", tags: new[] { "Autenticação" }, Summary = "Autentica um usuário anônimo ou por CPF", Description = "Este endpoint gera um token de acesso para um usuário convidado. Opcionalmente, um CPF pode ser fornecido para vincular a sessão a um usuário existente.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "cpf", In = ParameterLocation.Path, Required = false, Type = typeof(string), Summary = "CPF do usuário", Description = "CPF opcional para identificar o usuário. Se não for fornecido, a autenticação será anônima.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Summary = "Autenticação bem-sucedida", Description = "Retorna o token de acesso.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "O CPF fornecido não corresponde a nenhum usuário no sistema.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "O formato do CPF é inválido.")]
    public async Task<IActionResult> RunAtendimento([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/atendimento/{cpf?}")] HttpRequest req, string? cpf)
    {
        try
        {
            _logger.LogInformation("Iniciando autenticação para atendimento. CPF: {cpf}", cpf ?? "não informado");

            if (!string.IsNullOrEmpty(cpf))
            {
                // Limpa o CPF, removendo pontos e traços
                cpf = Common.CleanCpf(cpf);

                // Valida o formato e o cálculo do CPF
                if (!Common.IsCpfValido(cpf))
                {
                    _logger.LogWarning("CPF inválido fornecido: {cpf}", cpf);
                    return new BadRequestObjectResult("CPF inválido");
                }

                var cliente = await _usuarioRepository.GetClienteByCPF(cpf);
                var tokenAtendimento = await _tokenAtendimentoRepository.GenerateTokenAsync(cliente, cpf);
                
                _logger.LogInformation("Cliente encontrado pelo CPF: {cpf}", cpf);
                    
                var token = _jwtTokenService.GenerateToken(tokenAtendimento.TokenId.ToString());
                return new OkObjectResult(token);
            }
            
            // Se chegou aqui, não tem CPF ou não encontrou o cliente
            // Gera um token para cliente não identificado
            _logger.LogInformation("Gerando token de atendimento anônimo");
            var tokenAtendimentoAnonimo = await _tokenAtendimentoRepository.GenerateTokenAsync();
            var tokenAnonimo = _jwtTokenService.GenerateToken(tokenAtendimentoAnonimo.TokenId.ToString());

            return new OkObjectResult(tokenAnonimo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar a requisição de atendimento.");
            throw;
        }
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}