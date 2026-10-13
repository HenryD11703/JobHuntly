using JobHuntly.Application.DTOs;
using JobHuntly.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobHuntly.Api.Controllers;

[Authorize]
public class AnalyticsController(IAnalyticsService service) : ApiControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<AnalyticsSummaryDto>> GetSummary() =>
        Ok(await service.GetSummaryAsync(UserId));
}
