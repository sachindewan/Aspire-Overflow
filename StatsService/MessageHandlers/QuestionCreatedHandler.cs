using Contracts;
using Marten;

namespace StatsService.MessageHandlers
{
    public class QuestionCreatedHandler
    {
        public static async Task HandleAsync(QuestionCreated message,IDocumentSession session, CancellationToken cancellationToken)
        {
            session.Events.StartStream(message.QuestionId, message);
            await session.SaveChangesAsync(cancellationToken);
        }
    }
}
