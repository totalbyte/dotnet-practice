using AutoMapper;
using MongoDB.Bson;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Common.Mappings;

public sealed class ReviewProfile : Profile
{
    public ReviewProfile()
    {
        CreateMap<ObjectId, string>().ConvertUsing(id => id.ToString());

        CreateMap<Author, AuthorDto>();
        CreateMap<Device, DeviceDto>();
        CreateMap<StaffReply, StaffReplyDto>();
        CreateMap<Comment, CommentDto>();

        CreateMap<Review, ReviewDto>();
    }
}
