using JobHuntly.Application.Interfaces;
using JobHuntly.Application.Logic;

namespace JobHuntly.Application.Services;

public class NudgeService(IJobApplicationRepository repository, TimeProvider clock) : INudgeService
{
    public async Task<IReadOnlyList<Nudge>> GetTodayAsync(Guid userId)
    {
        var applications = await repository.GetAllByUserIdAsync(userId);
        return NudgeCalculator.Calculate(applications, clock.GetUtcNow().UtcDateTime);
    }
}
