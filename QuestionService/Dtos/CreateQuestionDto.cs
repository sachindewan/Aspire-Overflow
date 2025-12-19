using System.ComponentModel.DataAnnotations;

namespace QuestionService.Dtos
{
    public sealed record CreateQuestionDto(
        [Required]string Title ,
        [Required]string Content , 
        [Required][MinLength(1,ErrorMessage = "at least 1 tag is required")] [MaxLength(2)]List<string> Tags);
}
