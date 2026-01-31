using Common;
using ImTools;
using Microsoft.EntityFrameworkCore;
using QuestionService.Data;
using QuestionService.Services;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.AddServiceDefaults();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<TagService>();
builder.Services.AddKeyCloakAuthentication();

builder.AddAzureNpgsqlDbContext<QuestionDbContext>("questionDb");


await builder.UseWolverineWithRabbitMqAsync(opt =>
{
    opt.ApplicationAssembly = typeof(Program).Assembly;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.MapDefaultEndpoints();

await app.MigrateDbContextAsync<QuestionDbContext>();

app.Run();
