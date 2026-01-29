using Contracts;
using JasperFx.Events;
using Marten;
using Marten.Events.Projections;
using StatsService.Models;

namespace StatsService.Projections
{
    public class TopUserProjection : EventProjection
    {
        public TopUserProjection() => ProjectAsync<IEvent<UserReputationChange>>(Apply);

        private async Task Apply(IEvent<UserReputationChange> @event, IDocumentOperations operations, CancellationToken token)
        {
            var day = DateOnly.FromDateTime(@event.Timestamp.UtcDateTime);

            var data = @event.Data;

            var id = $"{data.UserId}:{day:yyyyMMdd}";
            var trendingTag = await operations.LoadAsync<UserDailyReputation>(id, token) ?? new UserDailyReputation { Id = id, UserId = data.UserId, Date = day, Delta = 0 };

            trendingTag.Delta += 1;
            operations.Store(trendingTag);
        }
    }
}
