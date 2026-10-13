using JobHuntly.Domain.Entities;

namespace JobHuntly.Application.Demo;

/// <summary>
/// The fictional data every demo sandbox starts with. Companies, people and links are made up.
/// Dates are relative to "now", so each sandbox shows a believable current state: one follow-up
/// is due, one interview needs preparation, one debrief is missing, and three offers can be compared.
/// </summary>
public static class DemoSeed
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    /// <summary>Company names used in the sample data (the fictional-data test checks against this list).</summary>
    public static readonly string[] Companies =
    {
        "Harbor & Pine", "Quillmark", "Tessel", "Orbitcraft", "Fernwood", "Mapleseed", "Northwind Labs",
        "Cobalt Street", "Brightwave", "Ardent IO", "Silverline", "Lumen Freight", "Kettle & Co", "Gridline"
    };

    private sealed record Step(ApplicationStatus Status, double DaysAgo);

    private sealed record Plan(
        string Company, string Role, ApplicationSource? Source, string? Location, bool Remote,
        Step[] Steps, string? SalaryRange = null, string? RejectionReason = null);

    public static User Build(string email, string passwordHash, DateTime now)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            FirstName = "Demo",
            LastName = "Visitor",
            CreatedAt = now,
            IsDemo = true
        };

        var rateLimiter = NewStory(user, now, "Rate limiter for the billing API",
            "Our billing API was hit by one client sending thousands of requests per second and slowing everyone down.",
            "Protect the service without blocking honest customers.",
            "I designed and shipped a per-customer token-bucket rate limiter backed by Redis, with clear 429 responses.",
            "The slowdowns stopped and p95 latency dropped by 40 percent.", "backend", "api");
        NewStory(user, now, "Night outage, payment queue",
            "A payment queue stalled at 2 a.m. and orders were not being confirmed.",
            "Restore service fast and find the cause.",
            "I rolled back the release, drained the queue by hand, then traced the stall to a poison message and added a dead-letter queue.",
            "Service was back in 25 minutes and a bad message can no longer block the queue.", "incident", "reliability");

        foreach (var plan in Plans)
            user.JobApplications.Add(BuildApplication(user, plan, now));

        // Interviews, debriefs and offers on top of the timelines.
        var northwind = App(user, "Northwind Labs");
        northwind.JobPostText = "Northwind Labs is hiring a Backend Engineer to own our ingestion services. Remote within Latin America. " +
                                "Stack: C#, PostgreSQL, Kafka, AWS. 3+ years of experience and good English. USD 3,000 to 3,600 per month.";
        northwind.Events.Add(Event(northwind, EventType.FollowedUp, now.AddDays(-13), note: "Emailed the hiring manager"));
        var hr = AddInterview(northwind, At(now, -8), InterviewRound.HrScreen, InterviewStatus.Done);
        hr.Debrief = NewDebrief(hr, now.AddDays(-8), 4,
            "Talked salary range early, they were fine with 3.2k. Asked about timezone overlap, need 4h with CET. Stumbled on the question about my last team size. Next is a technical with the lead.",
            new[] { "Salary expectations", "Timezone overlap (needs 4 h with CET)", "Size of my last team" },
            new[] { "Salary range accepted without pushback" }, new[] { "Hesitated on team size" },
            new[] { "Prepare a clear answer about team size and my role in it" });
        northwind.Events.Add(Event(northwind, EventType.InterviewAdded, now.AddDays(-9), note: "HrScreen"));
        northwind.Events.Add(Event(northwind, EventType.DebriefSaved, now.AddDays(-8), note: "HrScreen"));
        var technical = AddInterview(northwind, NextTechnicalTime(now), InterviewRound.Technical, InterviewStatus.Scheduled);
        technical.ChecklistDone = new List<string> { "reread-post", "product", "setup" };
        technical.PrepQuestionsJson = PrepJson(now.AddDays(-1), rateLimiter.Id, rateLimiter.Title);
        northwind.Events.Add(Event(northwind, EventType.InterviewAdded, now.AddDays(-7), note: "Technical"));

        var cobalt = App(user, "Cobalt Street");
        AddInterview(cobalt, At(now, -1), InterviewRound.HrScreen, InterviewStatus.Scheduled); // no debrief yet: Today asks for it
        cobalt.Events.Add(Event(cobalt, EventType.InterviewAdded, now.AddDays(-5), note: "HrScreen"));

        var brightwave = App(user, "Brightwave");
        var behavioral = AddInterview(brightwave, At(now, -25), InterviewRound.Behavioral, InterviewStatus.Done);
        behavioral.Debrief = NewDebrief(behavioral, now.AddDays(-25), 5, "Great conversation about ownership and mentoring. Used the outage story.",
            new[] { "A time I disagreed with a teammate", "How I mentor juniors" }, new[] { "Outage story landed well" }, Array.Empty<string>(), Array.Empty<string>());
        brightwave.Events.Add(Event(brightwave, EventType.InterviewAdded, now.AddDays(-26), note: "Behavioral"));
        brightwave.Events.Add(Event(brightwave, EventType.DebriefSaved, now.AddDays(-25), note: "Behavioral"));
        brightwave.Offers.Add(NewOffer(brightwave, now.AddDays(-4), 38400m, Currency.EUR, PayPeriod.Annual, "Employee, health insurance, equity unclear"));
        brightwave.Events.Add(Event(brightwave, EventType.OfferAdded, now.AddDays(-4), note: "38400 EUR Annual"));

        var silverline = App(user, "Silverline");
        silverline.Offers.Add(NewOffer(silverline, now.AddDays(-5), 3400m, Currency.USD, PayPeriod.Monthly, "Remote, contractor, 15 days paid vacation"));
        silverline.Events.Add(Event(silverline, EventType.OfferAdded, now.AddDays(-5), note: "3400 USD Monthly"));

        var lumen = App(user, "Lumen Freight");
        lumen.Offers.Add(NewOffer(lumen, now.AddDays(-6), 24000m, Currency.GTQ, PayPeriod.Monthly, "On-site zone 10, bonus in December"));
        lumen.Events.Add(Event(lumen, EventType.OfferAdded, now.AddDays(-6), note: "24000 GTQ Monthly"));

        var ardent = App(user, "Ardent IO");
        var screen = AddInterview(ardent, At(now, -9), InterviewRound.HrScreen, InterviewStatus.Done);
        screen.Debrief = NewDebrief(screen, now.AddDays(-9), 3, "Friendly call, they described a take-home due Friday.",
            new[] { "Why I want to leave my current job" }, new[] { "Clear explanation of the process" }, Array.Empty<string>(), new[] { "Finish the take-home before Friday" });
        ardent.Events.Add(Event(ardent, EventType.InterviewAdded, now.AddDays(-10), note: "HrScreen"));

        var kettle = App(user, "Kettle & Co");
        var kettleTech = AddInterview(kettle, At(now, -18), InterviewRound.Technical, InterviewStatus.Done);
        kettleTech.Debrief = NewDebrief(kettleTech, now.AddDays(-18), 2, "Struggled with the system design question and ran out of time.",
            new[] { "Design a URL shortener" }, Array.Empty<string>(), new[] { "Ran out of time on design", "Weak on database indexing" }, new[] { "Practise system design with a timer" });
        kettle.Events.Add(Event(kettle, EventType.InterviewAdded, now.AddDays(-19), note: "Technical"));
        kettle.Events.Add(Event(kettle, EventType.DebriefSaved, now.AddDays(-18), note: "Technical"));

        return user;
    }

    // ---- the cast ----

    private static readonly Plan[] Plans =
    {
        new("Harbor & Pine", "Full-stack Developer", ApplicationSource.CompanySite, "Guatemala City", false, new[] { new Step(ApplicationStatus.Backlog, 2) }),
        new("Quillmark", "Frontend Engineer", ApplicationSource.LinkedIn, "Remote", true, new[] { new Step(ApplicationStatus.Backlog, 5) }),
        new("Tessel", "Platform Engineer", ApplicationSource.Referral, "Remote", true, new[] { new Step(ApplicationStatus.Backlog, 10), new Step(ApplicationStatus.Applied, 9) }),
        new("Orbitcraft", "Software Engineer", ApplicationSource.LinkedIn, "Remote", true, new[] { new Step(ApplicationStatus.Applied, 2) }),
        new("Fernwood", "Backend Developer", ApplicationSource.LinkedIn, "Remote", true, new[] { new Step(ApplicationStatus.Applied, 1) }),
        new("Mapleseed", "API Engineer", ApplicationSource.LinkedIn, "Remote", true, new[] { new Step(ApplicationStatus.Applied, 0.1) }),
        new("Northwind Labs", "Backend Engineer", ApplicationSource.LinkedIn, "Remote (LATAM)", true,
            new[] { new Step(ApplicationStatus.Applied, 19), new Step(ApplicationStatus.Interviewing, 11) }, "USD 3,000 to 3,600 / month"),
        new("Cobalt Street", "Software Engineer", ApplicationSource.Recruiter, "Remote", true,
            new[] { new Step(ApplicationStatus.Applied, 10), new Step(ApplicationStatus.Interviewing, 5) }),
        new("Brightwave", "Full-stack Engineer", ApplicationSource.Referral, "Madrid, hybrid", false,
            new[] { new Step(ApplicationStatus.Applied, 35), new Step(ApplicationStatus.Interviewing, 28), new Step(ApplicationStatus.Offer, 4) }),
        new("Ardent IO", "Backend Engineer", ApplicationSource.CompanySite, "Remote", true,
            new[] { new Step(ApplicationStatus.Applied, 15), new Step(ApplicationStatus.Interviewing, 10), new Step(ApplicationStatus.TechnicalTest, 3) }),
        new("Silverline", "Senior Backend (remote)", ApplicationSource.LinkedIn, "Remote", true,
            new[] { new Step(ApplicationStatus.Applied, 40), new Step(ApplicationStatus.Interviewing, 33), new Step(ApplicationStatus.TechnicalTest, 24), new Step(ApplicationStatus.Offer, 5) },
            "USD 3,400 / month"),
        new("Lumen Freight", ".NET Developer", ApplicationSource.Recruiter, "Guatemala City, zone 10", false,
            new[] { new Step(ApplicationStatus.Applied, 30), new Step(ApplicationStatus.Interviewing, 22), new Step(ApplicationStatus.Offer, 6) }),
        new("Kettle & Co", "Developer", ApplicationSource.Recruiter, "Remote", true,
            new[] { new Step(ApplicationStatus.Applied, 30), new Step(ApplicationStatus.Interviewing, 22), new Step(ApplicationStatus.Rejected, 14) },
            RejectionReason: "Skills gap"),
        new("Gridline", "Engineer", null, "Remote", true,
            new[] { new Step(ApplicationStatus.Applied, 25), new Step(ApplicationStatus.Rejected, 18) })
    };

    /// <summary>
    /// A believable interview time: 15:00 UTC on the given day offset (09:00 in Guatemala, mid-afternoon in
    /// Europe), instead of "now minus some hours", which landed at random times such as 00:38.
    /// </summary>
    private static DateTime At(DateTime now, int days) => now.Date.AddDays(days).AddHours(15);

    /// <summary>The next 15:00 UTC that is 20 to 44 hours away: always inside the 48-hour "prepare" window.</summary>
    private static DateTime NextTechnicalTime(DateTime now)
    {
        var candidate = At(now, 1);
        return candidate - now < TimeSpan.FromHours(20) ? candidate.AddDays(1) : candidate;
    }

    // ---- builders ----

    private static JobApplication BuildApplication(User user, Plan plan, DateTime now)
    {
        var application = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CompanyName = plan.Company,
            PositionTitle = plan.Role,
            Source = plan.Source,
            Location = plan.Location,
            IsRemote = plan.Remote,
            SalaryRange = plan.SalaryRange,
            RejectionReason = plan.RejectionReason,
            Status = plan.Steps[^1].Status
        };

        ApplicationStatus? previous = null;
        foreach (var step in plan.Steps)
        {
            var at = now.AddDays(-step.DaysAgo);
            application.Events.Add(new ApplicationEvent
            {
                Id = Guid.NewGuid(),
                JobApplicationId = application.Id,
                Type = previous is null ? EventType.Created : EventType.StatusChanged,
                OccurredAt = at,
                FromStatus = previous,
                ToStatus = step.Status,
                Note = step.Status == ApplicationStatus.Rejected ? plan.RejectionReason : null
            });
            previous = step.Status;
        }

        var applied = plan.Steps.FirstOrDefault(s => s.Status != ApplicationStatus.Backlog) ?? plan.Steps[0];
        application.AppliedDate = now.AddDays(-applied.DaysAgo);
        return application;
    }

    private static JobApplication App(User user, string company) =>
        user.JobApplications.Single(a => a.CompanyName == company);

    private static ApplicationEvent Event(JobApplication app, EventType type, DateTime at, string? note = null) => new()
    {
        Id = Guid.NewGuid(), JobApplicationId = app.Id, Type = type, OccurredAt = at, Note = note
    };

    private static Interview AddInterview(JobApplication app, DateTime date, InterviewRound round, InterviewStatus status)
    {
        var interview = new Interview
        {
            Id = Guid.NewGuid(), JobApplicationId = app.Id, InterviewDate = date, RoundType = round, Status = status
        };
        app.Interviews.Add(interview);
        return interview;
    }

    private static Debrief NewDebrief(Interview interview, DateTime at, int rating, string raw,
        string[] asked, string[] well, string[] poorly, string[] followUps) => new()
    {
        Id = Guid.NewGuid(), InterviewId = interview.Id, Rating = rating, RawNotes = raw, SavedAt = at,
        QuestionsAsked = asked.ToList(), WentWell = well.ToList(), WentPoorly = poorly.ToList(), FollowUps = followUps.ToList()
    };

    private static Offer NewOffer(JobApplication app, DateTime at, decimal amount, Currency currency, PayPeriod period, string notes) => new()
    {
        Id = Guid.NewGuid(), JobApplicationId = app.Id, Amount = amount, Currency = currency, Period = period, Notes = notes, CreatedAt = at
    };

    private static Story NewStory(User user, DateTime now, string title, string s, string t, string a, string r, params string[] tags)
    {
        var story = new Story
        {
            Id = Guid.NewGuid(), UserId = user.Id, Title = title, Situation = s, Task = t, Action = a, Result = r,
            Tags = tags.ToList(), CreatedAt = now.AddDays(-20)
        };
        user.Stories.Add(story);
        return story;
    }

    private static string PrepJson(DateTime generatedAt, Guid storyId, string storyTitle) =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            generatedAt,
            questions = new object[]
            {
                new { question = "How would you design rate limiting for a public API?", storyId, storyTitle },
                new { question = "When would you pick PostgreSQL over a document store?", storyId = (Guid?)null, storyTitle = (string?)null },
                new { question = "How do you keep a Kafka consumer idempotent?", storyId = (Guid?)null, storyTitle = (string?)null }
            }
        }, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
}
