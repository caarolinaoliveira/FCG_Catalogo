using FCG.Catalogo.Application.Configuration;
using FCG.Catalogo.Infrastructure.Configuration;
using FCG.Catalogo.Infrastructure.Messaging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<PaymentProcessedConsumer>();

var app = builder.Build();
app.Run();