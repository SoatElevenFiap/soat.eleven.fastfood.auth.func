using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Soat.Eleven.FastFood.Domain.Repositories;
using Soat.Eleven.FastFood.Domain.Services;
using Soat.Eleven.FastFood.Infra.Context;
using Soat.Eleven.FastFood.Infra.Repositories;
using Soat.Eleven.FastFood.Infra.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services.AddTransient<DataContext>();
builder.Services.AddTransient<IUsuarioRepostiory, UsuarioRepository>();
builder.Services.AddTransient<ITokenAtendimentoRepository, TokenAtendimentoRepository>();
builder.Services.AddTransient<IJwtTokenService, JwtTokenService>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.ConfigureFunctionsWebApplication();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
