using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Wolverine;
using Wolverine.RabbitMQ;

namespace Common
{
    public static class WolverineExtentions
    {
        public static async Task UseWolverineWithRabbitMqAsync(this IHostApplicationBuilder builder, Action<WolverineOptions> configureMessaging)
        {
            var retryPolicy = Policy
                .Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetryAsync(
                                    5,
                                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                                    (exception, timespan, retryCount) =>
                                    {
                                        Console.WriteLine($"Retry attempt {retryCount} failed: {exception.Message}. Retrying in {timespan.Seconds} seconds..");
                                    });

            await retryPolicy.ExecuteAsync(async () =>
            {
                var endpoint = builder.Configuration.GetConnectionString("messaging") ?? throw new InvalidOperationException("RabbitMQ connection string not found.");
                var factory = new ConnectionFactory
                {
                    Uri = new Uri(endpoint)
                };
                await using var connection = await factory.CreateConnectionAsync();
            });

            builder.Services.AddOpenTelemetry().WithTracing(config =>
            {
                config.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(builder.Environment.ApplicationName))
                .AddSource("Wolverine");
            });

            builder.UseWolverine(opts =>
            {
                opts.UseRabbitMqUsingNamedConnection("messaging")
                .AutoProvision()
                .DeclareExchange("questions");
                configureMessaging(opts);
            });
        }
    }
}
