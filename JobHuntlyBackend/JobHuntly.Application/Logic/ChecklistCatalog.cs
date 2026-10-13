using JobHuntly.Domain.Entities;

namespace JobHuntly.Application.Logic;

public sealed record ChecklistItem(string Id, string Text);

/// <summary>Deterministic preparation checklist for each interview round type (no AI involved).</summary>
public static class ChecklistCatalog
{
    private static readonly ChecklistItem QuestionsToAsk = new("questions-to-ask", "Prepare 2 questions to ask the interviewer");
    private static readonly ChecklistItem Setup = new("setup", "Test camera, microphone and editor setup");
    private static readonly ChecklistItem ResearchCompany = new("research-company", "Read the company's About page and one recent news item");

    private static readonly IReadOnlyDictionary<InterviewRound, IReadOnlyList<ChecklistItem>> Items =
        new Dictionary<InterviewRound, IReadOnlyList<ChecklistItem>>
        {
            [InterviewRound.HrScreen] = new[]
            {
                ResearchCompany,
                new ChecklistItem("salary-range", "Decide your salary range and the minimum you would accept"),
                new ChecklistItem("availability", "Check your availability and timezone overlap"),
                new ChecklistItem("pitch", "Practise a 60-second introduction"),
                QuestionsToAsk
            },
            [InterviewRound.Technical] = new[]
            {
                new ChecklistItem("reread-post", "Re-read the job post and list the stack they name"),
                new ChecklistItem("product", "Review their product and one recent engineering post"),
                Setup,
                QuestionsToAsk,
                new ChecklistItem("practice", "Solve one practice problem under a timer")
            },
            [InterviewRound.Behavioral] = new[]
            {
                new ChecklistItem("pick-stories", "Pick 3 STAR stories from your story bank"),
                new ChecklistItem("values", "Read the company's values and match one story to each"),
                new ChecklistItem("failure", "Prepare an honest answer about a failure and what you learned"),
                QuestionsToAsk,
                Setup
            },
            [InterviewRound.TakeHome] = new[]
            {
                new ChecklistItem("read-brief", "Read the whole brief and list what is explicitly required"),
                new ChecklistItem("clarify", "Send clarifying questions early"),
                new ChecklistItem("timebox", "Agree the deadline and plan your time"),
                new ChecklistItem("tests-readme", "Write tests and a short README"),
                new ChecklistItem("final-read", "Re-read the brief before submitting")
            },
            [InterviewRound.Final] = new[]
            {
                new ChecklistItem("review-debriefs", "Review your debriefs from earlier rounds"),
                new ChecklistItem("numbers", "Know your numbers: target, minimum and alternatives"),
                ResearchCompany,
                QuestionsToAsk,
                Setup
            },
            [InterviewRound.Other] = new[]
            {
                ResearchCompany,
                QuestionsToAsk,
                Setup
            }
        };

    public static IReadOnlyList<ChecklistItem> For(InterviewRound round) => Items[round];

    public static bool Contains(InterviewRound round, string itemId) =>
        Items[round].Any(i => i.Id == itemId);

    /// <summary>True when every item of the round's checklist has been ticked.</summary>
    public static bool IsComplete(InterviewRound round, IEnumerable<string> done)
    {
        var doneSet = done.ToHashSet();
        return Items[round].All(i => doneSet.Contains(i.Id));
    }
}
