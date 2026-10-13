using JobHuntly.Application.DTOs;

namespace JobHuntly.Application.Interfaces;

public interface IJobApplicationService
{
    Task<IEnumerable<JobApplicationDto>> GetAllApplicationsAsync(Guid userId);
    Task<JobApplicationDto> GetApplicationAsync(Guid id, Guid userId);
    Task<JobApplicationDto> CreateApplicationAsync(CreateJobApplicationDto dto, Guid userId);
    Task<JobApplicationDto> UpdateApplicationStatusAsync(Guid id, UpdateJobApplicationStatusDto dto, Guid userId);
    Task<JobApplicationDto> UpdateApplicationAsync(Guid id, UpdateJobApplicationDto dto, Guid userId);
    Task DeleteApplicationAsync(Guid id, Guid userId);

    Task<IEnumerable<ApplicationEventDto>> GetTimelineAsync(Guid id, Guid userId);
    Task<ApplicationEventDto> AddFollowUpAsync(Guid id, FollowUpDto dto, Guid userId);

    Task<InterviewDto> AddInterviewAsync(Guid jobApplicationId, CreateInterviewDto dto, Guid userId);
    Task<InterviewDto> UpdateInterviewAsync(Guid jobApplicationId, Guid interviewId, UpdateInterviewDto dto, Guid userId);
    Task<InterviewDto> ToggleChecklistItemAsync(Guid jobApplicationId, Guid interviewId, ToggleChecklistItemDto dto, Guid userId);
    Task<InterviewDto> SaveDebriefAsync(Guid jobApplicationId, Guid interviewId, SaveDebriefDto dto, Guid userId);
}
