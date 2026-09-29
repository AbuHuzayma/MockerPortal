namespace Portal.Infrastructure.Mocking;

public sealed class MockEndpoint
{
    public Guid Id { get; set; }
    public Guid MockApiId { get; set; }
    public MockApi MockApi { get; set; } = null!;

    public required string Path { get; set; }
    public required string HttpMethod { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<MockResponse> Responses { get; set; } = [];
}
