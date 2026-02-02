using Common;
using Contracts;
using ImTools;
using JasperFx.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using QuestionService.Data;
using QuestionService.Services;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.AddServiceDefaults();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<TagService>();
builder.Services.AddKeyCloakAuthentication();

var connectionString = builder.Configuration.GetConnectionString("questionDb");

// Register a DbContext or multiple DbContext types as normal
builder.Services.AddDbContext<QuestionDbContext>(
    x => x.UseNpgsql(connectionString),

    // This is actually a significant performance gain
    // for Wolverine's sake
    optionsLifetime: ServiceLifetime.Singleton);

await builder.UseWolverineWithRabbitMqAsync(opt =>
{
    opt.ApplicationAssembly = typeof(Program).Assembly;
    opt.PersistMessagesWithPostgresql(connectionString!);
    opt.UseEntityFrameworkCoreTransactions();
    opt.PublishMessage<QuestionCreated>().ToRabbitExchange("Contracts.QuestionCreated").UseDurableOutbox();
    opt.PublishMessage<QuestionUpdated>().ToRabbitExchange("Contracts.QuestionUpdated").UseDurableOutbox();
    opt.PublishMessage<QuestionDeleted>().ToRabbitExchange("Contracts.QuestionDeleted").UseDurableOutbox();
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
