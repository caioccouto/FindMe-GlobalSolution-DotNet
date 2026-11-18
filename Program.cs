using System.Text.Json.Serialization;
using FindMe.Data;
using FindMe.Dtos;
using FindMe.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------------------------
// Banco de dados: Oracle da FIAP via Entity Framework Core
// ---------------------------------------------------------------------------------------------
// TODO(VOCÊ): configure a connection string "ConnectionStrings:Oracle" com seu RM e senha.
// NÃO escreva a senha em arquivo versionado. Use user-secrets (veja o README):
//   dotnet user-secrets set "ConnectionStrings:Oracle" "User Id=RMxxxxxx;Password=SUA_SENHA;Data Source=oracle.fiap.com.br:1521/ORCL;"
var connectionString = builder.Configuration.GetConnectionString("Oracle");

builder.Services.AddDbContext<FindMeDbContext>(options =>
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Connection string do Oracle não configurada. Defina 'ConnectionStrings:Oracle' " +
            "(user-secrets ou variável de ambiente ConnectionStrings__Oracle). Veja o README.");
    }
    options.UseOracle(connectionString);
});

// ---------------------------------------------------------------------------------------------
// Controllers, JSON e validação
// ---------------------------------------------------------------------------------------------
builder.Services
    .AddControllers(options =>
    {
        // As mensagens de "obrigatório" são as nossas ([Required(ErrorMessage = ...)]).
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .AddJsonOptions(options =>
    {
        // Enums como texto ("EM_ANALISE"), nunca como número.
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // 400 com mensagem em JSON (requisito 05 do PDF): lista de campos inválidos.
        options.InvalidModelStateResponseFactory = context =>
        {
            var erros = context.ModelState
                .Where(kv => kv.Value is { Errors.Count: > 0 })
                .SelectMany(kv => kv.Value!.Errors.Select(e => new ErroCampo(
                    NormalizarCampo(kv.Key),
                    e.Exception != null || string.IsNullOrWhiteSpace(e.ErrorMessage)
                        ? "Valor inválido ou em formato incorreto"
                        : e.ErrorMessage)))
                .ToList();

            return new BadRequestObjectResult(new ValidationErrorResponse(
                DateTime.UtcNow, StatusCodes.Status400BadRequest, "Bad Request", erros));
        };
    });

// 404/409/500 em JSON padronizado
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapControllers();

// Opcional: aplica as migrations ao iniciar (Database:MigrateOnStartup = true no appsettings).
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<FindMeDbContext>().Database.Migrate();
}

app.Run();

static string NormalizarCampo(string chave)
{
    var campo = chave.StartsWith("$.") ? chave[2..] : chave;
    if (string.IsNullOrEmpty(campo) || campo == "$") return "body";
    return char.ToLowerInvariant(campo[0]) + campo[1..];
}
