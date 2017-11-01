﻿using Odin.Data.Core.Models;
using Odin.Data.Core.Repositories;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace Odin.Data.Persistence
{
    public class OrdersRepository : IOrdersRepository
    {
        private readonly IApplicationDbContext _context;

        public OrdersRepository(IApplicationDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Order> GetOrdersFor(string userId)
        {
            return _context.Orders

                .Where(o => o.ConsultantId == userId)
                .Include(o => o.Transferee)
                .Include(o => o.ProgramManager)
                .Include(o => o.Consultant)
                .Include(o => o.Services.Select(st => st.ServiceType))
                .ToList();

        }

        public Order GetOrderByTrackingId(string trackingId)
        {
            return _context.Orders
                .Where(o => o.TrackingId.Equals(trackingId))
                .Include(o => o.Services)
                .SingleOrDefault<Order>();
        }

        public void Add(Order order)
        {
            _context.Orders.Add(order);
        }

        public Order GetOrderById(string orderId)
        {

            return _context.Orders
                .Where(o => o.Id == orderId)
                .Include(o => o.Services)
                .Include(o => o.Rent)
                .Include(o => o.Services.Select(s => s.ServiceType))
                .Include(o => o.Rent)
                .Include(o => o.Rent.NumberOfBathrooms)
                .Include(o => o.Rent.HousingType)
                .Include(o => o.Rent.AreaType)
                .Include(o => o.Rent.TransportationType)
                .Include(o => o.DepositType)
                .Include(o => o.BrokerFeeType)
                .SingleOrDefault<Order>();
        }

        public Order GetOrderFor(string userId, string orderId)
        {
            return _context.Orders
                .Where(o => o.Id == orderId && o.ConsultantId == userId)
                .Include(o => o.Services)
                .Include(o => o.Rent)
                .Include(o => o.Services.Select(s => s.ServiceType))
                .Include(o => o.Rent)
                .Include(o => o.Rent.NumberOfBathrooms)
                .Include(o => o.Rent.HousingType)
                .Include(o => o.Rent.AreaType)
                .Include(o => o.Rent.TransportationType)
                .Include(o => o.DepositType)
                .Include(o => o.BrokerFeeType)
                .SingleOrDefault<Order>();
        }
    }
}
