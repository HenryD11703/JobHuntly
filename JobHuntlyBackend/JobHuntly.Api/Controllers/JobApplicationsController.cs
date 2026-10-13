using JobHuntly.Application.DTOs;
using JobHuntly.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobHuntly.Api.Controllers;

[Authorize]
public class JobApplicationsController(IJobApplicationService service) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobApplicationDto>>> GetAll() =>
        Ok(await service.GetAllApplicationsAsync(UserId));

    [HttpPost]
    public async Task<ActionResult<JobApplicationDto>> Create(CreateJobApplicationDto dto)
    {
        var application = await service.CreateApplicationAsync(dto, UserId);
        return CreatedAtAction(nameof(GetAll), new { id = application.Id }, application);
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

    [HttpPost("{id:guid}/interviews")]
    public async Task<ActionResult<InterviewDto>> AddInterview(Guid id, CreateInterviewDto dto) =>
        Ok(await service.AddInterviewAsync(id, dto, UserId));
}
