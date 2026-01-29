using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    public record UserReputationChange(string QuestionId, string UserId, string Content, List<string> Tags);
}
