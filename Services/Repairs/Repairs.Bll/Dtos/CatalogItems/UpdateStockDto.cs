using System.ComponentModel.DataAnnotations;

namespace Repairs.Bll.Dtos.CatalogItems;

public sealed class UpdateStockDto
{
    [Range(0, 1_000_000)]
    public int Stock { get; set; }
}
