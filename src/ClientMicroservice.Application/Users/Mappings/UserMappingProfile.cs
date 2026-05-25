using AutoMapper;
using ClientMicroservice.Application.Common.DTOs;
using ClientMicroservice.Domain.Entities;

namespace ClientMicroservice.Application.Users.Mappings;

public sealed class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>();
    }
}
