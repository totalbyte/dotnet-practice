using AutoMapper;
using Repairs.Bll.Dtos.Customers;
using Repairs.Domain.Models;

namespace Repairs.Bll.Mapping;

public sealed class CustomerProfile : Profile
{
    public CustomerProfile()
    {
        CreateMap<Customer, CustomerDto>();
        CreateMap<CreateCustomerDto, Customer>(MemberList.Source);
        CreateMap<UpdateCustomerDto, Customer>(MemberList.Source);
    }
}
