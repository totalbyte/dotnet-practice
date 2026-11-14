namespace Repairs.Bll.Dtos.Customers;

public sealed record CustomerDto
{
    public long Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
