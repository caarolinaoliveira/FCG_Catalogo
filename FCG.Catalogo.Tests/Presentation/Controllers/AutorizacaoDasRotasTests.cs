using System.Reflection;
using System.Security.Claims;
using FCG.Catalogo.Presentation.Configuration;
using FCG.Catalogo.Presentation.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.Catalogo.Tests.Presentation.Controllers;

public class AutorizacaoDasRotasTests
{
    private static readonly string[] RotasDeEscrita = { nameof(JogoController.CadastrarJogo), nameof(JogoController.AtualizarJogo), nameof(JogoController.DeletarJogo) };
    private static readonly string[] RotasPublicas = { nameof(JogoController.ObterTodosJogos), nameof(JogoController.Buscar), nameof(JogoController.ObterPorId), nameof(JogoController.ObterPorTitulo) };

    public static IEnumerable<object[]> Escrita() => RotasDeEscrita.Select(n => new object[] { n });
    public static IEnumerable<object[]> Publicas() => RotasPublicas.Select(n => new object[] { n });

    private static MethodInfo Acao(Type controller, string nome) => controller.GetMethod(nome)!;

    private static IEnumerable<MethodInfo> TodasAsAcoes()
        => typeof(JogoController).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(m => m.GetCustomAttributes().OfType<HttpMethodAttribute>().Any());

    private static List<IAuthorizeData> DadosDeAutorizacao(MethodInfo acao)
        => acao.DeclaringType!.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Cast<IAuthorizeData>()
            .Concat(acao.GetCustomAttributes<AuthorizeAttribute>(inherit: true)).ToList();

    private static async Task<bool> PermitidoAsync(MethodInfo acao, ClaimsPrincipal usuario)
    {
        var services = new ServiceCollection().AddLogging().AddAuthorization().BuildServiceProvider();
        var politica = await AuthorizationPolicy.CombineAsync(
            services.GetRequiredService<IAuthorizationPolicyProvider>(), DadosDeAutorizacao(acao));
        var resultado = await services.GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(usuario, null, politica!.Requirements);
        return resultado.Succeeded;
    }

    private static ClaimsPrincipal ComRole(string? role)
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, "maria@fcg.com") };
        if (role is not null) claims.Add(new Claim(ClaimTypes.Role, role));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Bearer"));
    }

    private static ClaimsPrincipal Anonimo() => new(new ClaimsIdentity());

    [Fact]
    public void Role_admin_tem_o_mesmo_nome_emitido_pelo_servico_de_usuarios()
    {
        Assert.Equal("Admin", Papeis.Admin);
        Assert.Equal("Usuario", Papeis.Usuario);
    }

    [Theory]
    [MemberData(nameof(Escrita))]
    public void Rotas_de_escrita_exigem_a_role_admin(string nome)
    {
        var acao = Acao(typeof(JogoController), nome);

        var authorize = Assert.Single(acao.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal(Papeis.Admin, authorize.Roles);
        Assert.Empty(acao.GetCustomAttributes<AllowAnonymousAttribute>());
    }

    [Theory]
    [MemberData(nameof(Escrita))]
    public async Task Rotas_de_escrita_aceitam_admin_e_recusam_usuario_comum_e_anonimo(string nome)
    {
        var acao = Acao(typeof(JogoController), nome);

        Assert.True(await PermitidoAsync(acao, ComRole(Papeis.Admin)));
        Assert.False(await PermitidoAsync(acao, ComRole(Papeis.Usuario)));
        Assert.False(await PermitidoAsync(acao, ComRole(null)));
        Assert.False(await PermitidoAsync(acao, Anonimo()));
    }

    [Theory]
    [MemberData(nameof(Publicas))]
    public void Leituras_e_busca_sao_publicas(string nome)
    {
        var acao = Acao(typeof(JogoController), nome);

        Assert.Single(acao.GetCustomAttributes<AllowAnonymousAttribute>());
        Assert.Empty(acao.GetCustomAttributes<AuthorizeAttribute>());
    }

    [Fact]
    public void Toda_acao_de_escrita_de_jogos_esta_na_lista_de_rotas_protegidas()
    {
        var escritaNoControllerDeJogos = typeof(JogoController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes().Any(a => a is HttpPostAttribute or HttpPutAttribute or HttpDeleteAttribute or HttpPatchAttribute))
            .Select(m => m.Name);

        Assert.Equal(RotasDeEscrita.Order(), escritaNoControllerDeJogos.Order());
    }

    [Fact]
    public void Nenhuma_acao_depende_so_da_politica_padrao()
    {
        var semAtributo = TodasAsAcoes()
            .Where(a => !a.GetCustomAttributes<AllowAnonymousAttribute>().Any() && DadosDeAutorizacao(a).Count == 0)
            .Select(a => $"{a.DeclaringType!.Name}.{a.Name}");

        Assert.Empty(semAtributo);
    }

    [Fact]
    public void Rotas_de_pedido_biblioteca_e_avaliacao_exigem_login()
    {
        var acoes = TodasAsAcoes().Where(a => a.DeclaringType != typeof(JogoController));

        Assert.All(acoes, a =>
        {
            Assert.NotEmpty(DadosDeAutorizacao(a));
            Assert.Empty(a.GetCustomAttributes<AllowAnonymousAttribute>());
        });
    }
}
