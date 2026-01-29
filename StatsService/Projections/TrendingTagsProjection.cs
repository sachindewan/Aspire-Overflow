using Contracts;
using JasperFx.Events;
using Marten;
using Marten.Events.Projections;
using StatsService.Models;

namespace StatsService.Projections
{
    public class TrendingTagsProjection: EventProjection
    {
        public TrendingTagsProjection() => ProjectAsync<IEvent<QuestionCreated>>(Apply);

        private async Task Apply(IEvent<QuestionCreated> @event, IDocumentOperations operations, CancellationToken token)
        {
            var day = DateOnly.FromDateTime(DateTime.SpecifyKind(@event.Data.Created,DateTimeKind.Utc));
            foreach(var tag in @event.Data.Tags)
            {
                var id = $"{tag}:{day:yyyyMMdd}";
                var trendingTag = await operations.LoadAsync<TagDailyUsage>(id,token) ?? new TagDailyUsage { Id = id, Tag = tag, Date = day, Count = 0 };
               
                    trendingTag.Count += 1;
                    operations.Store(trendingTag);
            }
        }
    }
}
