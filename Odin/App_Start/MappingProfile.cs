﻿using AutoMapper;
using Odin.Data.Core.Dtos;
using Odin.Data.Core.Models;
using Odin.ViewModels;

namespace Odin
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<ApplicationUser, UserDto>()
                .ForMember(m => m.Phone, opt => opt.MapFrom(src => src.PhoneNumber));

            CreateMap<Order, OrderIndexViewModel>();
            CreateMap<Transferee, TransfereeViewModel>();
            CreateMap<Consultant, ConsultantViewModel>();
            CreateMap<Manager, ProgramManagerViewModel>();

            CreateMap<OrderDto, Order>()
                .ForMember(m => m.Transferee, opt => opt.Ignore())
                .ForMember(m => m.Consultant, opt => opt.Ignore())
                .ForMember(m => m.ProgramManager, opt => opt.Ignore());
            CreateMap<TransfereeDto, Transferee>();
            CreateMap<ProgramManagerDto, Manager>();
            CreateMap<ConsultantDto, Consultant>();
            CreateMap<Transferee, TransfereeDto>();
            CreateMap<Manager, ProgramManagerDto>();
            CreateMap<Consultant, ConsultantDto>();
            CreateMap<ProgramManagerImportDto, Manager>();
        }
    }
}