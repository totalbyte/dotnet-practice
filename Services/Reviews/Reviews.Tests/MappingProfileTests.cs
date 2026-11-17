using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Reviews.Application.Common.Mappings;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Entities;
using Reviews.Domain.Enums;
using Reviews.Domain.ValueObjects;

namespace Reviews.Tests;

public sealed class MappingProfileTests
{
    private readonly MapperConfiguration _configuration =
        new(cfg => cfg.AddProfile<ReviewProfile>(), NullLoggerFactory.Instance);

    [Fact]
    public void Configuration_is_valid() => _configuration.AssertConfigurationIsValid();

    [Fact]
    public void Review_is_mapped_with_string_id_value_objects_and_nested_arrays()
    {
        var review = Review.ForRepair(5, new Device("TV", "LG", "43UP7500"), new Author(4, "Taras"), 4, null, "Honest");
        review.AddComment("Olena", "Same experience", isStaff: false);
        review.ReplyAsStaff("Service center", "Thanks");

        var dto = _configuration.CreateMapper().Map<ReviewDto>(review);

        Assert.Equal(review.Id.ToString(), dto.Id);
        Assert.Equal(ReviewType.Repair, dto.ReviewType);
        Assert.Equal(new AuthorDto(4, "Taras"), dto.Author);
        Assert.Equal(new DeviceDto("TV", "LG", "43UP7500"), dto.Device);
        Assert.Equal("Same experience", Assert.Single(dto.Comments).Text);
        Assert.Equal("Thanks", dto.StaffReply!.Text);
        Assert.Empty(dto.Pros);
        Assert.Equal(1, dto.Version);
    }
}
