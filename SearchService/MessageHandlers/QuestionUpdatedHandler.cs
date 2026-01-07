using Contracts;
using SearchService.Models;
using System.Text.RegularExpressions;
using Typesense;

namespace SearchService.MessageHandlers
{
    public class QuestionUpdatedHandler(ITypesenseClient typesenseClient)
    {
        public async Task HandleAsync(QuestionUpdated message)
        {

            var doc = new SearchQuestion()
            {
                Id = message.QuestionId,
                Title = message.Title,
                Content = StripHtml(message.Content),
                Tags = message.Tags.ToArray()
            };


            await typesenseClient.UpsertDocument("questions", doc);
            Console.WriteLine($"Updated question with Id {message.QuestionId}");
        }

        private static string StripHtml(string input)
        {
            return Regex.Replace(input, "<.*?>", string.Empty);
        }
    }
}
