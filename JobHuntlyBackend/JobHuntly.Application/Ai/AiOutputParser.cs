using System.Text.Json;
using JobHuntly.Application.DTOs;

namespace JobHuntly.Application.Ai;

/// <summary>Raised internally when model output does not match what we asked for.</summary>
public sealed class AiOutputException(string message) : Exception(message);

/// <summary>
/// Validates model output against explicit limits before anything reaches the user or the database.
/// Policy, learned from earlier projects where models slightly exceeded what was asked:
///  - wrong types, missing required keys, or non-JSON output are rejected;
///  - lists longer than the limit are trimmed to the limit;
///  - strings longer than the limit are trimmed, but a string more than four times the limit is
///    treated as runaway output and rejected.
/// </summary>
public static class AiOutputParser
{
    public const int ShortText = 200;
    public const int MaxRequirements = 10;
    public const int MaxQuestions = 8;
    public const int MaxDebriefItems = 10;
    public const int MaxQuestionLength = 300;

    public static JobPostDraftDto ParseJobPost(string raw)
    {
        var root = ParseObject(raw);

        var draft = new JobPostDraftDto
        {
            CompanyName = OptionalString(root, "companyName", 150),
            PositionTitle = OptionalString(root, "positionTitle", 150),
            SalaryRange = OptionalString(root, "salaryRange", 100),
            Location = OptionalString(root, "location", 150),
            IsRemote = OptionalBool(root, "isRemote"),
            Requirements = OptionalStringList(root, "requirements", MaxRequirements, ShortText)
        };
        return draft;
    }

    public static List<PrepQuestionDto> ParsePrepQuestions(string raw, IReadOnlyDictionary<Guid, string> knownStories)
    {
        var root = ParseObject(raw);
        if (!root.TryGetProperty("questions", out var array) || array.ValueKind != JsonValueKind.Array)
            throw new AiOutputException("'questions' must be an array.");

        var result = new List<PrepQuestionDto>();
        foreach (var item in array.EnumerateArray())
        {
            if (result.Count == MaxQuestions) break;
            if (item.ValueKind != JsonValueKind.Object) throw new AiOutputException("Each question must be an object.");

            var question = RequiredString(item, "question", MaxQuestionLength);
            Guid? storyId = null;
            string? storyTitle = null;

            if (item.TryGetProperty("storyId", out var idElement) && idElement.ValueKind == JsonValueKind.String
                && Guid.TryParse(idElement.GetString(), out var parsed) && knownStories.TryGetValue(parsed, out var title))
            {
                storyId = parsed;     // only ids we actually sent are accepted; anything else becomes "no story"
                storyTitle = title;
            }
            else if (idElement.ValueKind is not (JsonValueKind.Null or JsonValueKind.String or JsonValueKind.Undefined))
            {
                throw new AiOutputException("'storyId' must be a string or null.");
            }

            result.Add(new PrepQuestionDto { Question = question, StoryId = storyId, StoryTitle = storyTitle });
        }

        if (result.Count == 0) throw new AiOutputException("No usable questions.");
        return result;
    }

    public static DebriefDraftDto ParseDebrief(string raw)
    {
        var root = ParseObject(raw);
        var keys = new[] { "questionsAsked", "wentWell", "wentPoorly", "followUps" };
        if (!keys.Any(k => root.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Array))
            throw new AiOutputException("None of the debrief lists are present.");

        return new DebriefDraftDto
        {
            QuestionsAsked = OptionalStringList(root, "questionsAsked", MaxDebriefItems, ShortText),
            WentWell = OptionalStringList(root, "wentWell", MaxDebriefItems, ShortText),
            WentPoorly = OptionalStringList(root, "wentPoorly", MaxDebriefItems, ShortText),
            FollowUps = OptionalStringList(root, "followUps", MaxDebriefItems, ShortText)
        };
    }

    // ---- helpers ----

    /// <summary>Finds the JSON object in the model's text, tolerating code fences or a stray sentence around it.</summary>
    public static JsonElement ParseObject(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) throw new AiOutputException("Empty answer.");

        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start < 0 || end <= start) throw new AiOutputException("No JSON object found.");

        try
        {
            using var document = JsonDocument.Parse(raw[start..(end + 1)]);
            if (document.RootElement.ValueKind != JsonValueKind.Object) throw new AiOutputException("Answer is not an object.");
            return document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new AiOutputException($"Invalid JSON: {ex.Message}");
        }
    }

    private static string? OptionalString(JsonElement obj, string name, int max)
    {
        if (!obj.TryGetProperty(name, out var element) || element.ValueKind == JsonValueKind.Null) return null;
        if (element.ValueKind != JsonValueKind.String) throw new AiOutputException($"'{name}' must be a string or null.");
        var text = Clean(element.GetString(), name, max);
        return text.Length == 0 ? null : text;
    }

    private static string RequiredString(JsonElement obj, string name, int max)
    {
        if (!obj.TryGetProperty(name, out var element) || element.ValueKind != JsonValueKind.String)
            throw new AiOutputException($"'{name}' must be a string.");
        var text = Clean(element.GetString(), name, max);
        if (text.Length == 0) throw new AiOutputException($"'{name}' is empty.");
        return text;
    }

    private static bool OptionalBool(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var element) || element.ValueKind == JsonValueKind.Null) return false;
        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw new AiOutputException($"'{name}' must be true or false.")
        };
    }

    private static List<string> OptionalStringList(JsonElement obj, string name, int maxItems, int maxLength)
    {
        if (!obj.TryGetProperty(name, out var element) || element.ValueKind == JsonValueKind.Null) return new List<string>();
        if (element.ValueKind != JsonValueKind.Array) throw new AiOutputException($"'{name}' must be an array.");

        var items = new List<string>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String) throw new AiOutputException($"'{name}' must contain only strings.");
            var text = Clean(item.GetString(), name, maxLength);
            if (text.Length == 0) continue;
            if (items.Count < maxItems) items.Add(text);   // too many items: keep the first ones
        }
        return items;
    }

    private static string Clean(string? value, string name, int max)
    {
        var text = (value ?? string.Empty).Trim();
        if (text.Length > max * 4) throw new AiOutputException($"'{name}' is far longer than allowed.");
        return text.Length > max ? text[..max].TrimEnd() : text;
    }
}
