using Microsoft.EntityFrameworkCore;
using QuestionService.Data;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.AddServiceDefaults();
builder.Services.AddAuthentication()
                .AddKeycloakJwtBearer("keycloak",realm:"overflow", options =>
                {
                    options.Audience = "Overflow";
                    options.Authority = "http://localhost:6001/realms/overflow";
                    options.RequireHttpsMetadata = false;
                });

builder.AddNpgsqlDbContext<QuestionDbContext>("questionDb");

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
