namespace Catalog.Domain.Entities;

public class Brand
{
    public const int NameMaxLength = 100;
    public const int CountryMaxLength = 100;

    private readonly List<CatalogItem> _items = [];

    private Brand()
    {
    }

    public Brand(string name, string? country = null)
    {
        Rename(name);
        ChangeCountry(country);
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Country { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public IReadOnlyCollection<CatalogItem> Items => _items;

    public void Rename(string name) => Name = Guard.Required(name, NameMaxLength, "Brand name");

    public void ChangeCountry(string? country) =>
        Country = Guard.Optional(country, CountryMaxLength, "Brand country");
}
