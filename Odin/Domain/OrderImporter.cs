﻿using AutoMapper;
using Odin.Data.Core;
using Odin.Data.Core.Dtos;
using Odin.Data.Core.Models;
using Odin.Interfaces;
using System;

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
            var consultantId = _unitOfWork.Consultants.GetConsultantBySeContactUid(orderDto.Consultant.SeContactUid).Id;
            var transferee = _unitOfWork.Transferees.GetTransfereeByEmail(orderDto.Transferee.Email);
            
            var programManagerId = _unitOfWork.Managers.GetManagerBySeContactUid(orderDto.ProgramManager.SeContactUid).Id;

            if (order == null)
            {
                order = _mapper.Map<OrderDto, Order>(orderDto);

                if (transferee == null)
                {
                    transferee = _mapper.Map<TransfereeDto, Transferee>(orderDto.Transferee);
                    _unitOfWork.Transferees.Add(transferee);
                }


                //Map type values
                if (!String.IsNullOrEmpty(orderDto.BrokerFeeTypeSeValue))
                {
                    order.BrokerFeeType = _unitOfWork.BrokerFeeTypes.GetBrokerFeeType(orderDto.BrokerFeeTypeSeValue);
                }

                if (!String.IsNullOrEmpty(orderDto.DepositTypeSeValue))
                {
                    order.DepositType = _unitOfWork.DepositTypes.GetDepositType(orderDto.DepositTypeSeValue);
                }

                _unitOfWork.Orders.Add(order);
            }
            else
            {
                _mapper.Map<OrderDto, Order>(orderDto, order);

                if (transferee == null)
                {
                    transferee = _mapper.Map<TransfereeDto, Transferee>(orderDto.Transferee);
                    _unitOfWork.Transferees.Add(transferee);
                }
            }

            order.TransfereeId = transferee.Id;
            order.ConsultantId = consultantId;
            order.ProgramManagerId = programManagerId;

            _unitOfWork.Complete();
            
        }
        
        
    }
}
