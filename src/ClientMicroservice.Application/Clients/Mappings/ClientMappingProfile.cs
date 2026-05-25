using AutoMapper;
using ClientMicroservice.Application.Common.DTOs;
using ClientMicroservice.Domain.Entities;
using ClientMicroservice.Domain.ValueObjects;

namespace ClientMicroservice.Application.Clients.Mappings;

public sealed class ClientMappingProfile : Profile
{
    public ClientMappingProfile()
    {
        CreateMap<Address, AddressDto>();
        CreateMap<BankingDetails, BankingDetailsDto>();
        CreateMap<Client, ClientDto>();
    }
}
