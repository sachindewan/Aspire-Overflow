using Common;
using Contracts;
using ImTools;
using JasperFx.Core;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SearchService.Data;
using SearchService.MessageHandlers;
using SearchService.Models;
using System.Text.RegularExpressions;
using Typesense;
using Typesense.Setup;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

var typeSenseUrl = builder.Configuration["services:typesense:typesense:0"];
if (string.IsNullOrEmpty(typeSenseUrl))
{
    throw new InvalidOperationException("Typesense URI not found in config");
}

var uri = new Uri(typeSenseUrl);


var typeSenseApiKey = builder.Configuration["typesense-api-key"];
if (string.IsNullOrEmpty(typeSenseApiKey))
{
    throw new InvalidOperationException("Typesense Api Key not found in config");
}

builder.Services.AddTypesenseClient(config =>
{
    config.ApiKey = typeSenseApiKey;
    config.Nodes = new List<Node>()
    {
        new Node(uri.Host,uri.Port.ToString(),uri.Scheme)
    };
});

await builder.UseWolverineWithRabbitMqAsync(opts =>
{
    opts.ApplicationAssembly = typeof(Program).Assembly;
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapDefaultEndpoints();

app.MapGet("/search", async (string query, ITypesenseClient client) =>
{
    string? tag = null;
    var tagMatch = Regex.Match(query, @"\[(.*)\]");
    if (tagMatch.Success)
    {
        tag = tagMatch.Groups[1].Value;
        query = query.Replace(tagMatch.Value, "").Trim();
    }

    var searchParams = new SearchParameters(query, "title,content");

    if (!string.IsNullOrWhiteSpace(tag))
    {
        searchParams.FilterBy = $"tags:=[{tag}]";
    }

    try
    {
        var result = await client.Search<SearchQuestion>("questions", searchParams);
        return Results.Ok(result.Hits.Select(hit => hit.Document));
    }
    catch (Exception ex)
    {
        return Results.Problem("Typesense search failed", ex.Message);
    }
});

app.MapGet("/search/similar-titles", async (string query, ITypesenseClient client) =>
{
    var searchParams = new SearchParameters(query, "title");
    try
    {
        var result = await client.Search<SearchQuestion>("questions", searchParams);
        return Results.Ok(result.Hits.Select(hit => hit.Document));
    }
    catch (Exception ex)
    {
        return Results.Problem("Typesense search failed", ex.Message);
    }
});

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
var typeSenseClientService = services.GetRequiredService<ITypesenseClient>();
await SearchInitializer.EnsureIndexExists(typeSenseClientService);
app.Run();
