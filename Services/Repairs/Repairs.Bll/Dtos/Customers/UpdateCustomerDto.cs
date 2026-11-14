using System.ComponentModel.DataAnnotations;

namespace Repairs.Bll.Dtos.Customers;

public sealed class UpdateCustomerDto
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(256)]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Email format is invalid")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\+?[0-9]{10,15}$", ErrorMessage = "Phone must contain 10 to 15 digits")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(100)]
    public string? UpdatedBy { get; set; }
}
