using ProfileService.Data;

namespace ProfileService.Middleware
{
    public class UserProfileCreationMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context, ProfileDbContext profileDbContext)
        {
            if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
            {
                var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (userId != null)
                {
                    var existingProfile = await profileDbContext.Profiles.FindAsync(userId);
                    if (existingProfile == null)
                    {
                        var name = context.User.FindFirst("name")?.Value ?? "Unnamed User";
                        var email = context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
                        var newProfile = new Models.UserProfile
                        {
                            Id = userId,
                            DisplayName = name
                        };

                        await profileDbContext.Profiles.AddAsync(newProfile);
                        await profileDbContext.SaveChangesAsync();
                    }
                }

                await next(context);
            }
        }
    }
}
