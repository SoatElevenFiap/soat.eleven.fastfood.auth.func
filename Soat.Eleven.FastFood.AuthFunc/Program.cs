using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Soat.Eleven.FastFood.Domain.Repositories;
using Soat.Eleven.FastFood.Domain.Services;
using Soat.Eleven.FastFood.Infra.Context;
using Soat.Eleven.FastFood.Infra.Repositories;
using Soat.Eleven.FastFood.Infra.Services;
using System.Text;
using System.Text.Encodings.Web;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services.AddTransient<DataContext>();
builder.Services.AddTransient<IUsuarioRepostiory, UsuarioRepository>();
builder.Services.AddTransient<ITokenAtendimentoRepository, TokenAtendimentoRepository>();
builder.Services.AddTransient<IJwtTokenService, JwtTokenService>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
});

builder.Services.AddSingleton<IOpenApiConfigurationOptions>(_ =>
{
    var options = new DefaultOpenApiConfigurationOptions
    {
        Info = new OpenApiInfo()
        {
            Version = "1.0.0",
            Title = "API de Autenticação - FastFood",
            Description = "API para autenticação de usuários"
        }
    };

    return options;
});

builder.ConfigureFunctionsWebApplication();

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

builder.Build().Run();

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(worker => {
        // Adicione esta linha
        worker.UseNewtonsoftJson();
    })
    .ConfigureServices(services => {
        // Seus outros serviços
    })
    .ConfigureOpenApi()
    .Build();

host.Run();