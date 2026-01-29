using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QuestionService.Models
{
    public class Answer
    {
        [MaxLength(36)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [MaxLength(1000)]
        public required string Content { get; set; }

        [MaxLength(50)]
        public required string UserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }

        public bool Accepted { get; set; } = false;
        public int Votes { get; set; }

        public string QuestionId { get; set; } = string.Empty;

        [JsonIgnore]
        public Question? Question { get; set; } = null;


    }
}
