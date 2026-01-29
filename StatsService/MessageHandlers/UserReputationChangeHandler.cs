using Contracts;
using Marten;
using Wolverine.Attributes;

namespace StatsService.MessageHandlers
{
    public class UserReputationChangeHandler
    {
        [Transactional]
        public static async Task HandleAsync(UserReputationChange message, IDocumentSession session, CancellationToken cancellationToken)
        {
            session.Events.StartStream(message.QuestionId, message);
            await session.SaveChangesAsync(cancellationToken);
        }
    }
}
