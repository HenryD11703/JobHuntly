using JobHuntly.Application.DTOs;
using JobHuntly.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobHuntly.Api.Controllers;

[Authorize]
public class JobApplicationsController(IJobApplicationService service, IOfferService offers) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobApplicationDto>>> GetAll() =>
        Ok(await service.GetAllApplicationsAsync(UserId));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobApplicationDto>> Get(Guid id) =>
        Ok(await service.GetApplicationAsync(id, UserId));

    [HttpPost]
    public async Task<ActionResult<JobApplicationDto>> Create(CreateJobApplicationDto dto)
    {
        var application = await service.CreateApplicationAsync(dto, UserId);
        return CreatedAtAction(nameof(Get), new { id = application.Id }, application);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<JobApplicationDto>> UpdateStatus(Guid id, UpdateJobApplicationStatusDto dto) =>
        Ok(await service.UpdateApplicationStatusAsync(id, dto, UserId));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JobApplicationDto>> Update(Guid id, UpdateJobApplicationDto dto) =>
        Ok(await service.UpdateApplicationAsync(id, dto, UserId));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteApplicationAsync(id, UserId);
        return NoContent();
    }

    [HttpGet("{id:guid}/timeline")]
    public async Task<ActionResult<IEnumerable<ApplicationEventDto>>> Timeline(Guid id) =>
        Ok(await service.GetTimelineAsync(id, UserId));

    [HttpPost("{id:guid}/follow-ups")]
    public async Task<ActionResult<ApplicationEventDto>> AddFollowUp(Guid id, FollowUpDto dto) =>
        Ok(await service.AddFollowUpAsync(id, dto, UserId));

    [HttpPost("{id:guid}/interviews")]
    public async Task<ActionResult<InterviewDto>> AddInterview(Guid id, CreateInterviewDto dto) =>
        Ok(await service.AddInterviewAsync(id, dto, UserId));

    [HttpPut("{id:guid}/interviews/{interviewId:guid}")]
    public async Task<ActionResult<InterviewDto>> UpdateInterview(Guid id, Guid interviewId, UpdateInterviewDto dto) =>
        Ok(await service.UpdateInterviewAsync(id, interviewId, dto, UserId));

    [HttpPut("{id:guid}/interviews/{interviewId:guid}/checklist")]
    public async Task<ActionResult<InterviewDto>> ToggleChecklist(Guid id, Guid interviewId, ToggleChecklistItemDto dto) =>
        Ok(await service.ToggleChecklistItemAsync(id, interviewId, dto, UserId));

    [HttpPut("{id:guid}/interviews/{interviewId:guid}/debrief")]
    public async Task<ActionResult<InterviewDto>> SaveDebrief(Guid id, Guid interviewId, SaveDebriefDto dto) =>
        Ok(await service.SaveDebriefAsync(id, interviewId, dto, UserId));

    [HttpPost("{id:guid}/offers")]
    public async Task<ActionResult<OfferDto>> AddOffer(Guid id, CreateOfferDto dto) =>
        Ok(await offers.AddOfferAsync(id, dto, UserId));
}
