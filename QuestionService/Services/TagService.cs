using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuestionService.Data;
using QuestionService.Models;

namespace QuestionService.Services
{
    public class TagService(IMemoryCache memoryCache, QuestionDbContext questionDbContext)
    {
        private const string CacheKey = "tags";

        public async Task<List<Tag>> GetTags()
        {
            return await memoryCache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
                var tags = await questionDbContext.Tags.ToListAsync();
                return tags;
            }) ?? [];
        }

        public async Task<bool> IsValidTags(List<string> slugs)
        {
            var tags = await GetTags();
            var tagSet = tags.Select(x=>x.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return slugs.All(x => tagSet.Contains(x));
        }
    }

}
