using System.ComponentModel.DataAnnotations;

namespace QuestionService.Dtos
{
    public sealed record CreateAnswerDto([Required] string Content);
}
