using AutoMapper;
using Odin.Data.Core;
using Odin.Data.Core.Dtos;
using Odin.Data.Core.Models;
using Odin.Helpers;
using Odin.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Odin.Domain
{
    public class OrderImporter : IOrderImporter
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;



        public OrderImporter(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public void ImportOrder(OrderDto orderDto)
        {
            var order = _unitOfWork.Orders.GetOrderByTrackingId(orderDto.TrackingId);

            // Will error if no consultant is found, expected behaivor, an order doesn't make sense without a consultant
            Consultant consultant = _unitOfWork.Consultants.GetConsultantBySeContactUid(orderDto.Consultant.SeContactUid);
            var consultantId = consultant.Id;

            var transferee = _unitOfWork.Transferees.GetTransfereeByEmail(orderDto.Transferee.Email);

            var programManagerId = _unitOfWork.Managers.GetManagerBySeContactUid(orderDto.ProgramManager.SeContactUid).Id;

            var IsNew = false;

            if (order == null)
            {
                order = _mapper.Map<OrderDto, Order>(orderDto);

                if (transferee == null)
                {
                    transferee = _mapper.Map<TransfereeDto, Transferee>(orderDto.Transferee);
                    transferee.InviteStatus = InviteStatus.NotYetInvited;
                    _unitOfWork.Transferees.Add(transferee);
                }

                _unitOfWork.Orders.Add(order);
                IsNew = true;
            }
            else
            {
                _mapper.Map<OrderDto, Order>(orderDto, order);

                if (transferee == null)
                {
                    transferee = _mapper.Map<TransfereeDto, Transferee>(orderDto.Transferee);
                    transferee.InviteStatus = InviteStatus.NotYetInvited;
                    _unitOfWork.Transferees.Add(transferee);
                }
            }

            //Add default services
            //Populate list of service categories available for this order.
            var cats = ServiceHelper.GetCategoriesForServiceFlag(order.ServiceFlag);

            //Get all service types that the order already has.
            var ids = order.Services.Select(s => s.ServiceTypeId).ToList();

            IEnumerable<ServiceType> defServTypes =
                _unitOfWork.ServiceTypes.GetDefaultServiceTypes(cats, ids, order.IsInternational);

            foreach (var servType in defServTypes)
            {
                order.Services.Add(new Service()
                {
                    Selected = true,
                    ServiceType = servType
                });
            }

            //Map type values
            if (!String.IsNullOrWhiteSpace(orderDto.BrokerFeeTypeSeValue))
            {
                order.BrokerFeeType = _unitOfWork.BrokerFeeTypes.GetBrokerFeeType(orderDto.BrokerFeeTypeSeValue);
            }

            if (!String.IsNullOrWhiteSpace(orderDto.DepositTypeSeValue))
            {
                order.DepositType = _unitOfWork.DepositTypes.GetDepositType(orderDto.DepositTypeSeValue);
            }

            order.TransfereeId = transferee.Id;
            order.ConsultantId = consultantId;
            order.ProgramManagerId = programManagerId;

            
            if (IsNew)
            {
                Notification notification = new Notification()
                {
                    NotificationType = NotificationType.OrderCreated,
                    Message = "Your manager has assigned you a new Transferee, their name is ",
                    Title = "New Order Creation",
                    OrderId = order.Id
                };

                consultant.Notify(notification);
                
            }

            if ( ((order.ServiceFlag & (int)ServiceCategory.AccompaniedHomeFinding) > 0 || (order.ServiceFlag & (int)ServiceCategory.UnAccompaniedHomeFinding) > 0) && order.HomeFinding == null)
            {
                var homeFinding = new HomeFinding
                {
                    Id = order.Id
                };
                order.HomeFinding = homeFinding;

            }
        
            _unitOfWork.Complete();

        }


    }
}
