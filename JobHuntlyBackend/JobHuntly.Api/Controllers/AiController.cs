using JobHuntly.Application.Ai;
using JobHuntly.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobHuntly.Api.Controllers;

/// <summary>
/// Optional AI helpers. Each returns a draft for the user to review; the app works without them.
/// </summary>
[Authorize]
public class AiController(IAiAssistantService assistant) : ApiControllerBase
{
    /// <summary>Whether AI is available and how much of today's allowance is left (the UI hides the buttons when it is not).</summary>
    [HttpGet("status")]
    public async Task<ActionResult<AiStatusDto>> Status() => Ok(await assistant.GetStatusAsync(UserId));

    /// <summary>Turns pasted job-post text into a draft application. Nothing is saved.</summary>
    [HttpPost("extract-job-post")]
    public async Task<ActionResult<JobPostDraftDto>> ExtractJobPost(ExtractJobPostRequest request, CancellationToken ct) =>
        Ok(await assistant.ExtractJobPostAsync(request.Text, UserId, ct));

    /// <summary>Likely questions for an interview. The saved set is returned for free; "regenerate" uses one AI call.</summary>
    [HttpPost("~/api/jobapplications/{id:guid}/interviews/{interviewId:guid}/prep")]
    public async Task<ActionResult<PrepResultDto>> Prep(Guid id, Guid interviewId, PrepRequest? request, CancellationToken ct) =>
        Ok(await assistant.GetPrepQuestionsAsync(id, interviewId, request?.Regenerate ?? false, UserId, ct));

    /// <summary>Organises messy interview notes into a draft debrief. Nothing is saved.</summary>
    [HttpPost("~/api/jobapplications/{id:guid}/interviews/{interviewId:guid}/debrief/structure")]
    public async Task<ActionResult<DebriefDraftDto>> StructureDebrief(Guid id, Guid interviewId, StructureDebriefRequest request, CancellationToken ct) =>
        Ok(await assistant.StructureDebriefAsync(id, interviewId, request.RawNotes, UserId, ct));
}
