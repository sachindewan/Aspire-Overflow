using Contracts;
using SearchService.Models;
using System.Text.RegularExpressions;
using Typesense;
using Wolverine.Attributes;

namespace SearchService.MessageHandlers
{
    public class QuestionCreatedHandler(ITypesenseClient client)
    {
        public async Task HandleAsync(QuestionCreated message)
        {
            var createdAt = new DateTimeOffset(message.Created).ToUnixTimeSeconds();

            var doc = new SearchQuestion()
            {
                Id = message.QuestionId,
                Title = message.Title,
                Content = StripHtml(message.Content),
                CreatedAt = createdAt,
                Tags = message.Tags.ToArray()
            };


            await client.CreateDocument("questions", doc);
            Console.WriteLine($"Created question with Id {message.QuestionId}");
        }

        private static string StripHtml(string input)
        {
            return Regex.Replace(input, "<.*?>", string.Empty);
        }
    }
}
