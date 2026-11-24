using Catalog.Bll.Dtos.CatalogItems;
using Catalog.Bll.Dtos.Categories;
using Catalog.Bll.Interfaces;
using Catalog.Domain.Exceptions;
using Catalog.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using CatalogItemType = Catalog.Grpc.CatalogItemType;
using DomainValidationException = Catalog.Domain.Exceptions.ValidationException;

namespace Catalog.Api.Grpc;

public sealed class CatalogGrpcService(ICatalogItemService items, ICategoryService categories)
    : CatalogService.CatalogServiceBase
{
    public const int MaxIdsPerRequest = 100;

    public override async Task<CatalogItemReply> GetCatalogItem(GetCatalogItemRequest request, ServerCallContext context)
    {
        if (request.Id <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Id must be positive"));

        return ToReply(await CallAsync(() => items.GetByIdAsync(request.Id, context.CancellationToken)));
    }

    public override async Task<CatalogItemsReply> GetCatalogItemsByIds(
        GetCatalogItemsByIdsRequest request, ServerCallContext context)
    {
        if (request.Ids.Count > MaxIdsPerRequest)
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                $"At most {MaxIdsPerRequest} ids can be requested at once"));
        if (request.Ids.Any(id => id <= 0))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Ids must be positive"));

        var found = await CallAsync(() => items.GetByIdsAsync([.. request.Ids], context.CancellationToken));
        return new CatalogItemsReply { Items = { found.Select(ToReply) } };
    }

    public override async Task<CategoriesReply> ListCategories(ListCategoriesRequest request, ServerCallContext context)
    {
        if (request.HasParentCategoryId && request.ParentCategoryId <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Parent category id must be positive"));

        var query = new CategoryQuery
        {
            ParentCategoryId = request.HasParentCategoryId ? request.ParentCategoryId : null,
            SortBy = CategoryQuery.SortByDisplayOrder,
            PageSize = CategoryQuery.MaxPageSize
        };
        var page = await CallAsync(() => categories.GetListAsync(query, context.CancellationToken));
        return new CategoriesReply { Categories = { page.Items.Select(ToReply) } };
    }

    private static async Task<T> CallAsync<T>(Func<Task<T>> call)
    {
        try
        {
            return await call();
        }
        catch (EntityNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (DomainValidationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                string.Join("; ", ex.Errors.SelectMany(error => error.Value.Select(message => $"{error.Key}: {message}")))));
        }
        catch (DomainException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
    }

    private static CatalogItemReply ToReply(CatalogItemDetailsDto item)
    {
        var reply = new CatalogItemReply
        {
            Id = item.Id,
            Sku = item.Sku,
            Name = item.Name,
            ItemType = item.ItemType switch
            {
                nameof(Domain.Entities.CatalogItemType.Part) => CatalogItemType.Part,
                nameof(Domain.Entities.CatalogItemType.Service) => CatalogItemType.Service,
                _ => CatalogItemType.Unspecified
            },
            Price = DecimalValue.FromDecimal(item.Price),
            IsActive = item.IsActive,
            WarrantyMonths = item.WarrantyMonths,
            CreatedAt = ToTimestamp(item.CreatedAt),
            Categories =
            {
                item.Categories.Select(category => new ItemCategory
                {
                    CategoryId = category.CategoryId,
                    Name = category.Name,
                    Slug = category.Slug,
                    IsPrimary = category.IsPrimary
                })
            }
        };

        if (item.BrandId is { } brandId)
            reply.BrandId = brandId;
        if (item.BrandName is { } brandName)
            reply.BrandName = brandName;
        if (item.EstimatedDurationMinutes is { } duration)
            reply.EstimatedDurationMinutes = duration;
        if (item.UpdatedAt is { } updatedAt)
            reply.UpdatedAt = ToTimestamp(updatedAt);

        return reply;
    }

    private static CategoryReply ToReply(CategoryDto category)
    {
        var reply = new CategoryReply
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            DisplayOrder = category.DisplayOrder
        };
        if (category.ParentCategoryId is { } parentId)
            reply.ParentCategoryId = parentId;
        return reply;
    }

    private static Timestamp ToTimestamp(DateTime utc)
        => Timestamp.FromDateTime(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
}
