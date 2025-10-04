using Microsoft.Azure.Functions.Worker.Builder;
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
            Title = "API de Autenticação",
            Description = "API para autenticação de usuários"
        }
    };

    return options;
});

builder.ConfigureFunctionsWebApplication();

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

builder.Build().Run();

//var host = new HostBuilder()
//    .ConfigureFunctionsWebApplication() // ou .ConfigureFunctionsWorkerDefaults() para apps não-HTTP
//    .ConfigureServices(services =>
//    {
//        services.Configure<JsonSerializerOptions>(options =>
//        {
//            options.PropertyNameCaseInsensitive = true;
//            options.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
//        });
//    })
//    .ConfigureOpenApi() // <-- Adicione esta linha
//    .Build();

//host.Run();