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

builder.AddNpgsqlDbContext<QuestionDbContext>("questionDb");


await builder.UseWolverineWithRabbitMqAsync(opt =>
{
    opt.PublishAllMessages().ToRabbitExchange("questions");
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

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
try
{
    var context = services.GetRequiredService<QuestionDbContext>();
    await context.Database.MigrateAsync();
}
catch(Exception ex)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occured while migrating or seeding the database.");
}

app.Run();
