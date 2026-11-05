using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace FCG.Catalogo.Presentation.Configuration
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPresentation(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddControllers();
            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Insira o token JWT: ",
                    Name = "Authorization",
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
                c.UseInlineDefinitionsForEnums();
            });

            var key = Encoding.UTF8.GetBytes(ObterSegredoJwt(configuration));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = configuration["JwtSettings:Audiencia"],
                    ValidIssuer = configuration["JwtSettings:Emissor"],
                    ValidateIssuerSigningKey = true
                };
            });
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
            return services;
        }

        private const int TamanhoMinimoSegredoJwt = 32;

        private static string ObterSegredoJwt(IConfiguration configuration)
        {
            var segredo = configuration["JwtSettings:Segredo"];

            if (string.IsNullOrWhiteSpace(segredo))
                throw new InvalidOperationException(
                    "Configuração obrigatória 'JwtSettings:Segredo' não encontrada. " +
                    "Defina via 'dotnet user-secrets' (desenvolvimento) ou pela variável de ambiente " +
                    "'JwtSettings__Segredo' (produção, injetada pelo Azure Key Vault).");

            if (segredo.Length < TamanhoMinimoSegredoJwt)
                throw new InvalidOperationException(
                    $"'JwtSettings:Segredo' deve ter no mínimo {TamanhoMinimoSegredoJwt} caracteres " +
                    $"(valor atual tem {segredo.Length}).");

            return segredo;
        }
    }
}