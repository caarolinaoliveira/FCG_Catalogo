using System.Diagnostics;
using FCG.Catalogo.Application.Configuration;
using FCG.Catalogo.Infrastructure.Configuration;
using FCG.Catalogo.Infrastructure.Context;
using FCG.Catalogo.Infrastructure.Messaging;
using FCG.Catalogo.Presentation.Configuration;
using FCG.Catalogo.Presentation.Logging;
using FCG.Catalogo.Presentation.Middlewares;
using Microsoft.EntityFrameworkCore;

ActivitySource.AddActivityListener(new ActivityListener
{
    ShouldListenTo = _ => true,
    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
});

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole(options => options.FormatterName = "compact")
    .AddConsoleFormatter<CompactConsoleFormatter, Microsoft.Extensions.Logging.Console.ConsoleFormatterOptions>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPresentation(builder.Configuration);
builder.Services.AddHostedService<PaymentProcessedConsumer>();
builder.Services.AddHealthChecks();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogoDbContext>();
    db.Database.Migrate();
}

// Swagger: sempre em Development; fora dele, só se Swagger:Habilitado for true (padrão: desligado).
// Swagger:RoutePrefix muda o caminho da UI e do JSON (padrão "swagger"). Atrás do Ingress, onde "/swagger"
// já é usado pelo serviço de Usuários, usa-se um prefixo próprio (ex.: "swagger-catalogo").
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Habilitado"))
{
    var swaggerPrefixo = app.Configuration["Swagger:RoutePrefix"]?.Trim('/');
    if (string.IsNullOrWhiteSpace(swaggerPrefixo))
        swaggerPrefixo = "swagger";

    app.UseSwagger(c => c.RouteTemplate = swaggerPrefixo + "/{documentName}/swagger.json");
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = swaggerPrefixo;
        // Caminho RELATIVO (sem "/" inicial): resolve a partir de onde a UI foi aberta, em qualquer prefixo.
        c.SwaggerEndpoint("v1/swagger.json", "FCG Catálogo v1");
    });
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
// Anônimo de propósito: as probes do Kubernetes não enviam token (a política padrão exige autenticação).
app.MapHealthChecks("/health").AllowAnonymous();
app.Run();