using JobHuntly.Application.Ai;

namespace JobHuntly.Tests.Integration;

/// <summary>
/// Stands in for Groq in every automated test (no test ever calls the real service). Queue answers
/// with <see cref="Enqueue"/> or failures with <see cref="EnqueueFailure"/>; when the queue is empty it
/// answers with a sensible default for whichever feature is asking.
/// </summary>
public sealed class FakeAiClient : IAiClient
{
    private readonly Queue<object> _queue = new();
    private readonly object _gate = new();

    public bool IsConfigured { get; set; } = true;

    public List<(string System, string User)> Requests { get; } = new();

    public int Calls { get { lock (_gate) return Requests.Count; } }

    public void Enqueue(string json) { lock (_gate) _queue.Enqueue(json); }

    public void EnqueueFailure(Exception exception) { lock (_gate) _queue.Enqueue(exception); }

    public Task<string> CompleteJsonAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        lock (_gate)
        {
            Requests.Add((systemPrompt, userPrompt));
            if (_queue.Count > 0)
            {
                return _queue.Dequeue() switch
                {
                    Exception ex => Task.FromException<string>(ex),
                    string json => Task.FromResult(json),
                    _ => throw new InvalidOperationException()
                };
            }
        }
        return Task.FromResult(DefaultFor(systemPrompt));
    }

    private static string DefaultFor(string system)
    {
        if (system.Contains("extract structured data", StringComparison.OrdinalIgnoreCase))
            return """
                {"companyName":"Northwind Labs","positionTitle":"Backend Engineer","salaryRange":"USD 3,000 to 3,600 / month",
                 "location":"Remote (LATAM)","isRemote":true,"requirements":["C#","PostgreSQL","Kafka"]}
                """;

        if (system.Contains("structured notes", StringComparison.OrdinalIgnoreCase))
            return """
                {"questionsAsked":["Salary expectations","Team size"],"wentWell":["Salary accepted"],
                 "wentPoorly":["Hesitated on team size"],"followUps":["Prepare an answer about team size"]}
                """;

        return """
            {"questions":[{"question":"How would you design rate limiting for a public API?"},
                          {"question":"Tell me about a production bug you debugged."}]}
            """;
    }
}
