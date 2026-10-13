using JobHuntly.Application.DTOs;
using JobHuntly.Application.Interfaces;
using JobHuntly.Application.Logic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobHuntly.Api.Controllers;

[Authorize]
public class TodayController(INudgeService nudges) : ApiControllerBase
{
    /// <summary>What needs the user today, computed by plain rules (no AI).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Nudge>>> Get() => Ok(await nudges.GetTodayAsync(UserId));
}

[Authorize]
public class OffersController(IOfferService offers) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<OfferComparisonDto>> Compare() => Ok(await offers.GetComparisonAsync(UserId));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await offers.DeleteOfferAsync(id, UserId);
        return NoContent();
    }
}

[Authorize]
public class SettingsController(ISettingsService settings) : ApiControllerBase
{
    [HttpGet("rates")]
    public async Task<ActionResult<RatesDto>> GetRates() => Ok(await settings.GetRatesAsync(UserId));

    [HttpPut("rates")]
    public async Task<ActionResult<RatesDto>> UpdateRates(RatesDto dto) => Ok(await settings.UpdateRatesAsync(dto, UserId));

    /// <summary>Suggests today's rates from a public provider. Does not save anything.</summary>
    [HttpGet("rates/suggested")]
    public async Task<ActionResult<SuggestedRatesDto>> SuggestedRates(CancellationToken ct) =>
        Ok(await settings.GetSuggestedRatesAsync(ct));
}

[Authorize]
public class StoriesController(IStoryService stories) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<StoryDto>>> GetAll() => Ok(await stories.GetAllAsync(UserId));

    [HttpPost]
    public async Task<ActionResult<StoryDto>> Create(SaveStoryDto dto) =>
        StatusCode(StatusCodes.Status201Created, await stories.CreateAsync(dto, UserId));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StoryDto>> Update(Guid id, SaveStoryDto dto) =>
        Ok(await stories.UpdateAsync(id, dto, UserId));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await stories.DeleteAsync(id, UserId);
        return NoContent();
    }
}
