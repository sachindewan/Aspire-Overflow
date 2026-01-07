using Contracts;
using Typesense;

namespace SearchService.MessageHandlers
{
    public class AcceptAnswerHandler(ITypesenseClient client)
    {
        public async Task HandleAsync(AnswerAccepted message)
        {
            await client.UpdateDocument("question", message.QuestionId, new { HasAcceptedAnswer = true });
        }
    }
}
