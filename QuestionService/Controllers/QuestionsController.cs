using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestionService.Data;
using QuestionService.Dtos;
using QuestionService.Models;
using System.Security.Claims;

namespace QuestionService.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class QuestionsController(QuestionDbContext dbContext) : ControllerBase
    {
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateQuestion(CreateQuestionDto dto)
        {
            var validTags = await dbContext.Tags.Where(x=>dto.Tags.Contains(x.Slug)).ToListAsync();
            var missing = dto.Tags.Except(validTags.Select(x=>x.Slug)).ToList();
            if (missing.Any()) {
                return BadRequest($"Invalid tags:{string.Join(", ", missing)}");
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var name = User.FindFirstValue("name");

            if (userId == null || name == null) return BadRequest("can not get user details");

            var question = new Question
            {
                Title = dto.Title,
                Content = dto.Content,
                TagSlugs = dto.Tags,
                AskerId = userId,
                AskerDisplayName = name
            };

            dbContext.Questions.Add(question);
            await dbContext.SaveChangesAsync();

            return Created($"/questions/{question.Id}", question);
        }

        [HttpGet]
        public async Task<ActionResult<List<Question>>> GetQuestions(string? tag)
        {
            var query = dbContext.Questions.AsQueryable();
            if (!string.IsNullOrEmpty(tag))
            {
                query = query.Where(x => x.TagSlugs.Contains(tag));
            }

            return await query.OrderByDescending(x=>x.CreatedAt).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Question>> GetQuestion(string id)
        {
            var question = await dbContext.Questions.FindAsync(id);
            if(question is null) return NotFound();

            await dbContext.Questions.Where(x => x.Id == id)
                .ExecuteUpdateAsync(setter => setter.SetProperty(x => x.ViewCount, x => x.ViewCount + 1));
            return question;
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult> UpdateQuestion(string id, CreateQuestionDto dto)
        {
            var question = await dbContext.Questions.FindAsync(id);

            if (question is null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId != question.AskerId) return Forbid();

            var validTags = await dbContext.Tags.Where(x => dto.Tags.Contains(x.Slug)).ToListAsync();
            var missing = dto.Tags.Except(validTags.Select(x => x.Slug)).ToList();
            if (missing.Any())
            {
                return BadRequest($"Invalid tags:{string.Join(", ", missing)}");
            }

            question.Title = dto.Title;
            question.Content = dto.Content;
            question.TagSlugs = dto.Tags;

            await dbContext.SaveChangesAsync();
            return NoContent();
        }

        [Authorize]
        [HttpDelete]
        public async Task<ActionResult> DeleteQuestion(string id)
        {
            var question = await dbContext.Questions.FindAsync(id);

            if (question is null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId != question.AskerId) return Forbid();

            dbContext.Questions.Remove(question);
            await dbContext.SaveChangesAsync();
            return NoContent();
        }

    }
}
