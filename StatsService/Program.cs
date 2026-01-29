using Common;
using Contracts;
using JasperFx.Events;
using JasperFx.Events.Projections;
using Marten;
using StatsService.Models;
using StatsService.Projections;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
// Add services to the container.
builder.Services.AddOpenApi();
await builder.UseWolverineWithRabbitMqAsync(opt =>
{
    opt.ApplicationAssembly = typeof(Program).Assembly;
});

builder.Services.AddMarten(opt =>
{
    opt.Connection(builder.Configuration.GetConnectionString("statDb") ?? throw new InvalidOperationException("statDb connection string not found."));
    opt.Events.StreamIdentity = StreamIdentity.AsString;
    opt.Events.AddEventType<QuestionCreated>();
    opt.Events.AddEventType<UserReputationChange>();
    opt.Schema.For<TagDailyUsage>().Index(x => x.Id).Index(x => x.Tag);
    opt.Schema.For<UserDailyReputation>().Index(x => x.Id).Index(x => x.UserId);
    opt.Projections.Add<TrendingTagsProjection>(ProjectionLifecycle.Inline);
    opt.Projections.Add<TopUserProjection>(ProjectionLifecycle.Inline);
}).UseLightweightSessions();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.MapDefaultEndpoints();
app.MapGet("/stats/trending-stats", async (IDocumentSession session) =>
{
    var since = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));
    var results = await session.Query<TagDailyUsage>()
        .Where(x => x.Date >= since && x.Date <= since)
        .OrderByDescending(x => x.Count)
        .ToListAsync();
    var data = results
    .GroupBy(x => x.Tag)
    .Select(x => new { x.Key, count = x.Sum(t => t.Count) })
    .OrderByDescending(x => x.count);
    return Results.Ok(data);
});

app.MapGet("/stats/top-users", async (IDocumentSession session) =>
{
    var since = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));
    var results = await session.Query<UserDailyReputation>()
        .Where(x => x.Date >= since && x.Date <= since)
        .ToListAsync();
    var data = results
    .GroupBy(x => x.UserId)
    .Select(x => new { UserId = x.Key, Reputation = x.Sum(t => t.Delta) })
    .OrderByDescending(x => x.Reputation)
    .Take(10)
    .ToList();
    return Results.Ok(data);
});
app.Run();
