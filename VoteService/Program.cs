using Common;
using Contracts;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VoteService.Data;
using VoteService.DTOs;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddKeyCloakAuthentication();
await builder.UseWolverineWithRabbitMqAsync(options =>
{
    options.ApplicationAssembly = typeof(Program).Assembly;
});

builder.AddNpgsqlDbContext<VoteDbContext>("voteDb");

// Add services to the container.
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/votes", async (CastVoteDto dto, VoteDbContext db, ClaimsPrincipal user, IMessageBus bus) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null) return Results.Unauthorized();
    if(dto.TargetType is not ("Question" or  "Answer"))
    {
        return Results.BadRequest("Invalid target type");
    }

    var alreadyVoted = await db.Votes.AnyAsync(x => x.UserId == userId && x.TargetId == dto.TargetId);
    if (alreadyVoted) return Results.BadRequest("Target already voted");

    db.Votes.Add(new VoteService.Models.Vote
    {
        TargetId = dto.TargetId,
        TargetType = dto.TargetType,
        UserId = userId,
        VoteValue = dto.VoteValue,
        QuestionId = dto.QuestionId
    });

    await db.SaveChangesAsync();
    await bus.PublishAsync(new VoteCasted(dto.TargetId,  dto.TargetType, dto.VoteValue));
    return Results.NoContent();
}).RequireAuthorization();

app.MapGet("/votes/{questionId}", async (string questionId, VoteDbContext db, ClaimsPrincipal user) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null) return Results.Unauthorized();

    var votes = await db.Votes.Where(x=>x.UserId == userId && x.QuestionId == questionId)
    .Select(x=> new UserVotesResult(x.TargetId, x.TargetType, x.VoteValue))
    .ToListAsync();

    return Results.Ok(votes);
}).RequireAuthorization();

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
try
{
    var context = services.GetRequiredService<VoteDbContext>();
    await context.Database.MigrateAsync();
}
catch (Exception ex)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occured while migrating or seeding the database.");
}

app.Run();
