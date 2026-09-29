namespace Portal.Infrastructure.Mocking;

/// <summary>EF entity — docs/05-database-design.md §2, docs/09-api-mocker.md.</summary>
public sealed class MockApi
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Environment { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MockEndpoint> Endpoints { get; set; } = [];
}
