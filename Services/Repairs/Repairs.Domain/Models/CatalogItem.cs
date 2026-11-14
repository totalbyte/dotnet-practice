namespace Repairs.Domain.Models;

public class CatalogItem
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime SyncedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
