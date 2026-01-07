using Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestionService.Data;
using QuestionService.Dtos;
using QuestionService.Models;
using QuestionService.Services;
using System.Security.Claims;
using Wolverine;

namespace QuestionService.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class QuestionsController(QuestionDbContext dbContext, IMessageBus messageBus, TagService tagService) : ControllerBase
    {
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateQuestion(CreateQuestionDto dto)
        {
            var validTags = await tagService.IsValidTags(dto.Tags);

            if (!validTags)
            {
                return BadRequest("Invalid tags");
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

            await messageBus.PublishAsync(new QuestionCreated
                (
                question.Id,
                question.Title,
                question.Content,
                question.CreatedAt,
                question.TagSlugs
                ));


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

            return await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Question>> GetQuestion(string id)
        {
            var question = await dbContext.Questions.Include(x=>x.Answers).FirstOrDefaultAsync(x=>x.Id==id);
            if (question is null) return NotFound();

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


            var validTags = await tagService.IsValidTags(dto.Tags);

            if (!validTags)
            {
                return BadRequest("Invalid tags");
            }

            question.Title = dto.Title;
            question.Content = dto.Content;
            question.TagSlugs = dto.Tags;

            await messageBus.PublishAsync(new QuestionUpdated
             (
             question.Id,
             question.Title,
             question.Content,
             question.TagSlugs
             ));


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

        [Authorize]
        [HttpPost("{questionId}/answers")]
        public async Task<ActionResult> PostAnswer(string questionId, CreateAnswerDto answerDto)
        {
            var question = await dbContext.Questions.FindAsync(questionId);

            if (question is null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var name = User.FindFirstValue("name");

            if (userId == null || name == null) return BadRequest("Cannot get user details");

            if (userId != question.AskerId) return Forbid();

            var answer = new Answer
            {
                QuestionId = questionId,
                Content = answerDto.Content,
                UserDisplayName = name,
                UserId = userId,
            };

            question.Answers.Add(answer);

            question.AnswerCount++;
            await dbContext.SaveChangesAsync();

            await messageBus.PublishAsync(new AnswerCountUpdated(questionId, question.AnswerCount));

            return Created($"question/{questionId}", answer);
        }

        [Authorize]
        [HttpPut("{questionId}/answers/{answerId}")]
        public async Task<ActionResult> UpdateAnswer(string questionId, string answerId, CreateAnswerDto answerDto)
        {
            var answer = await dbContext.Answers.FindAsync(answerId);

            if (answer is null) return NotFound();

            if (answer.QuestionId != questionId) return BadRequest("Cannot update answer details");

            answer.Content = answerDto.Content;
            answer.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
            return NoContent();
        }

        [Authorize]
        [HttpDelete("{questionId}/answers/{answerId}")]
        public async Task<ActionResult> DeleteAnswer(string questionId, string answerId)
        {
            var answer = await dbContext.Answers.FindAsync(answerId); 
            var question =  await dbContext.Questions.FindAsync(questionId);

            if (answer is null || question is null) return NotFound();

            if (answer.QuestionId != questionId || answer.Accepted) return BadRequest("Cannot update answer details");

            dbContext.Answers.Remove(answer);
            question.AnswerCount--;
            await dbContext.SaveChangesAsync();
            await messageBus.PublishAsync(new AnswerCountUpdated(questionId, question.AnswerCount));
            return NoContent();
        }

        [Authorize]
        [HttpPost("{questionId}/answers/{answerId}/accept")]
        public async Task<ActionResult> AcceptAnswer(string questionId, string answerId)
        {
            var answer = await dbContext.Answers.FindAsync(answerId);
            var question = await dbContext.Questions.FindAsync(questionId);

            if (answer is null || question is null) return NotFound();

            if (answer.QuestionId != questionId || question.HasAcceptedAnswer) return BadRequest("Cannot update answer details");

            answer.Accepted = true;
            question.HasAcceptedAnswer = true;
            await dbContext.SaveChangesAsync();
            await messageBus.PublishAsync(new AnswerAccepted(questionId));
            return NoContent();
        }

        [HttpGet("errors")]
        public ActionResult GetErrorResponses(int code)
        {
            ModelState.AddModelError("Problem one", "Validation problem one");
            ModelState.AddModelError("Problem two", "Validation problem two");
            return code switch
            {
                400 => BadRequest("Opposite of good request"),
                401 => Unauthorized(),
                403 => Forbid(),
                404 => NotFound(),
                500 => throw new Exception("This is a server error"),
                _   => ValidationProblem(ModelState)
            };
             
        }


    }
}
