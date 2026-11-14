using AutoMapper;
using Repairs.Bll.Dtos.RepairOrders;
using Repairs.Domain.Models;

namespace Repairs.Bll.Mapping;

public sealed class RepairOrderProfile : Profile
{
    public RepairOrderProfile()
    {
        CreateMap<RepairOrderItem, RepairOrderItemDto>();
        CreateMap<RepairOrderStatusHistory, StatusHistoryDto>();
        CreateMap<RepairOrderSummary, RepairOrderSummaryDto>();

        CreateMap<RepairOrderDetails, DeviceDto>()
            .ForMember(dto => dto.RowVersion, o => o.MapFrom(details => Convert.ToBase64String(details.RowVersion)));

        CreateMap<RepairOrder, RepairOrderDto>()
            .ForMember(dto => dto.Device, o => o.MapFrom(order => order.Details))
            .ForMember(dto => dto.TotalAmount, o => o.MapFrom(order => order.Items.Sum(item => item.LineTotal)));

        CreateMap<CreateRepairOrderDto, RepairOrderDetails>(MemberList.Source)
            .ForSourceMember(dto => dto.CustomerId, o => o.DoNotValidate())
            .ForSourceMember(dto => dto.CreatedBy, o => o.DoNotValidate())
            .ForSourceMember(dto => dto.Items, o => o.DoNotValidate());

        CreateMap<UpdateRepairOrderDetailsDto, RepairOrderDetails>(MemberList.Source)
            .ForMember(details => details.RowVersion, o => o.Ignore())
            .ForSourceMember(dto => dto.RowVersion, o => o.DoNotValidate());
    }
}
