using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Catalog.Bll.Caching;

public static class CatalogCacheKeys
{
    public const string Prefix = "catalog";

    public const string ItemListsVersion = $"{Prefix}:items:list-version";
    public const string CategoryListsVersion = $"{Prefix}:categories:list-version";
    public const string BrandListsVersion = $"{Prefix}:brands:list-version";

    public static string Item(long id) => $"{Prefix}:item:{id}";

    public static string Category(int id) => $"{Prefix}:category:{id}";

    public static string Brand(int id) => $"{Prefix}:brand:{id}";

    public static string ItemList(string version, object query) => $"{Prefix}:items:list:{version}:{Fingerprint(query)}";

    public static string CategoryList(string version, object query)
        => $"{Prefix}:categories:list:{version}:{Fingerprint(query)}";

    public static string BrandList(string version, object query) => $"{Prefix}:brands:list:{version}:{Fingerprint(query)}";

    public static string EntryKind(string key)
    {
        var parts = key.Split(':');
        return parts.Length > 2 && parts[2] is "list" or "list-version" ? $"{parts[1]}:{parts[2]}" : parts[1];
    }

    private static string Fingerprint(object query)
    {
        var json = JsonSerializer.Serialize(query, query.GetType());
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(json)), 0, 8);
    }
}
