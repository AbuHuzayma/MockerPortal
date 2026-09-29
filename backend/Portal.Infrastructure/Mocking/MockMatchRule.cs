namespace Portal.Infrastructure.Mocking;

public static class MockMatchSource
{
    public const string Header = "Header";
    public const string QueryString = "QueryString";
    public const string Path = "Path";
    public const string RequestBody = "RequestBody";
}

public static class MockMatchOperator
{
    public const string Equals = "Equals";
    public const string NotEquals = "NotEquals";
    public const string Contains = "Contains";
    public const string StartsWith = "StartsWith";
}

public sealed class MockMatchRule
{
    public Guid Id { get; set; }
    public Guid MockResponseId { get; set; }
    public MockResponse MockResponse { get; set; } = null!;

    public required string Source { get; set; }
    public required string Field { get; set; }
    public required string Operator { get; set; }
    public required string ExpectedValue { get; set; }
}
