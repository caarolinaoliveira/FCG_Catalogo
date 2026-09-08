using System.Diagnostics;
using FCG.Catalogo.Application.Configuration;
using FCG.Catalogo.Infrastructure.Configuration;
using FCG.Catalogo.Infrastructure.Messaging;
using FCG.Catalogo.Presentation.Configuration;
using FCG.Catalogo.Presentation.Logging;
using FCG.Catalogo.Presentation.Middlewares;

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();