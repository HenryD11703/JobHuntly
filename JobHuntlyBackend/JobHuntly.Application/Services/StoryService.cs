using JobHuntly.Application.DTOs;
using JobHuntly.Application.Exceptions;
using JobHuntly.Application.Interfaces;
using JobHuntly.Application.Logic;
using JobHuntly.Domain.Entities;

namespace JobHuntly.Application.Services;

public class StoryService(IStoryRepository stories, TimeProvider clock) : IStoryService
{
    public async Task<IEnumerable<StoryDto>> GetAllAsync(Guid userId) =>
        (await stories.GetAllByUserIdAsync(userId)).Select(Mapper.ToDto).ToList();

    public async Task<StoryDto> CreateAsync(SaveStoryDto dto, Guid userId)
    {
        var story = new Story
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAt = clock.GetUtcNow().UtcDateTime
        };
        Apply(story, dto);
        await stories.AddAsync(story);
        return Mapper.ToDto(story);
    }

    public async Task<StoryDto> UpdateAsync(Guid id, SaveStoryDto dto, Guid userId)
    {
        var story = await stories.GetByIdAsync(id, userId) ?? throw new NotFoundException("Story not found.");
        Apply(story, dto);
        await stories.SaveAsync();
        return Mapper.ToDto(story);
    }

    public async Task DeleteAsync(Guid id, Guid userId) =>
        await stories.DeleteAsync(await stories.GetByIdAsync(id, userId) ?? throw new NotFoundException("Story not found."));

    private static void Apply(Story story, SaveStoryDto dto)
    {
        story.Title = dto.Title.Trim();
        story.Situation = dto.Situation.Trim();
        story.Task = dto.Task.Trim();
        story.Action = dto.Action.Trim();
        story.Result = dto.Result.Trim();

        var tags = dto.Tags.Select(t => t?.Trim() ?? string.Empty).Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (tags.Any(t => t.Length > 40)) throw new ValidationException("Each tag must be 40 characters or fewer.");
        story.Tags = tags;
    }
}
