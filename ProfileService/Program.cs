using Common;
using Microsoft.EntityFrameworkCore;
using ProfileService.Data;
using ProfileService.Middleware;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddKeyCloakAuthentication();
builder.AddServiceDefaults();
await builder.UseWolverineWithRabbitMqAsync(opt =>
{
    opt.ApplicationAssembly = typeof(Program).Assembly;
});
builder.AddAzureNpgsqlDbContext<ProfileDbContext>("profileDb");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseMiddleware<UserProfileCreationMiddleware>();
app.MapDefaultEndpoints();

app.MapGet("/profiles/me", async (ClaimsPrincipal claims , ProfileDbContext dbContext) =>
{
    var userId = claims.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null) return Results.Unauthorized();
    var profile = await dbContext.Profiles.FirstOrDefaultAsync(p => p.Id == userId);
    if (profile == null) return Results.NotFound();
    return Results.Ok(profile);
}).RequireAuthorization();

app.MapGet("/profiles", async (string ids, ProfileDbContext dbContext) =>
{
    var id =  ids.Split(',',StringSplitOptions.RemoveEmptyEntries);
    var profiles = dbContext.Profiles.Where(x=>ids.Contains(x.Id)).ToList();
    return Results.Ok(profiles);
}).RequireAuthorization();

await app.MigrateDbContextAsync<ProfileDbContext>();

app.Run();

