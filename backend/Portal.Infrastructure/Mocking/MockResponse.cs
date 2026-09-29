namespace Portal.Infrastructure.Mocking;

public sealed class MockResponse
{
    public Guid Id { get; set; }
    public Guid MockEndpointId { get; set; }
    public MockEndpoint MockEndpoint { get; set; } = null!;

    public required string Name { get; set; }
    public int HttpStatusCode { get; set; } = 200;

    /// <summary>JSON object of header name → value.</summary>
    public string? ResponseHeaders { get; set; }

    /// <summary>Supports {{token}} substitution — see docs/09-api-mocker.md §4. Never executed as code.</summary>
    public string? ResponseBody { get; set; }

    public int DelayMilliseconds { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Lower evaluates first among matching responses for the same endpoint.</summary>
    public int Priority { get; set; }

    public ICollection<MockMatchRule> MatchRules { get; set; } = [];
}
