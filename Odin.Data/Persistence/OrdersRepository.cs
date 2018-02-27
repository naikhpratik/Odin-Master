using Odin.Data.Core.Models;
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

        //public IEnumerable<Order> GetOrdersFor(string userId)
        //{
        //    return _context.Orders

        //        .Where(o => (o.ConsultantId == userId || o.TransfereeId == userId)
        //                    && o.SeCustStatus != OrderStatus.Cancelled
        //                    && o.SeCustStatus != OrderStatus.Closed)
        //        .Include(o => o.Transferee)
        //        .Include(o => o.ProgramManager)
        //        .Include(o => o.Consultant)
        //        .Include(o => o.Services.Select(st => st.ServiceType))
        //        .ToList();

        //}

        public IEnumerable<Order> GetOrdersFor(string userId, string userRole)
        {
            if (UserRoles.Transferee == userRole)
            {
                return GetOrdersQueryable().Where(o => o.TransfereeId == userId).ToList();
            }
            else if (UserRoles.Consultant == userRole)
            {
                return GetOrdersQueryable().Where(o => o.ConsultantId == userId).ToList();
            }
            else if (UserRoles.ProgramManager == userRole || UserRoles.GlobalSupplyChain == userRole)
            {
                //Show closed/cancelled?
                return GetOrdersQueryable().Where(o => o.ProgramManagerId == userId).ToList();
            }

            return new List<Order>();
        }

        public Order GetOrderByTrackingId(string trackingId)
        {
            return _context.Orders
                .Where(o => o.TrackingId.Equals(trackingId))
                .Include(o => o.Services)
                .Include(o => o.HomeFinding)
                .SingleOrDefault<Order>();
        }

        public void Add(Order order)
        {
            _context.Orders.Add(order);
        }

        public Order GetOrderById(string orderId)
        {
            return GetOrderQueryable(orderId).SingleOrDefault<Order>();
        }

        //Removed because dangerous. What type of user does the order belong to?
        //public Order GetOrderFor(string userId, string orderId)
        //{
        //    return _context.Orders
        //        .Where(o => o.Id == orderId && (o.ConsultantId == userId || o.TransfereeId == userId))
        //        .Include(o => o.Services)
        //        .Include(o => o.HomeFinding)
        //        .Include(o => o.Services.Select(s => s.ServiceType))
        //        .Include(o => o.HomeFinding)
        //        .Include(o => o.HomeFinding.NumberOfBathrooms)
        //        .Include(o => o.HomeFinding.HousingType)
        //        .Include(o => o.HomeFinding.AreaType)
        //        .Include(o => o.HomeFinding.TransportationType)
        //        .Include(o => o.HomeFinding.HomeFindingProperties.Select(hfp => hfp.Property.Photos))
        //        .Include(o => o.DepositType)
        //        .Include(o => o.BrokerFeeType)
        //        .SingleOrDefault<Order>();

        //}

        public Order GetOrderFor(string userId, string orderId, string userRole)
        {
            if (userRole == UserRoles.Transferee)
            {
                return GetOrderQueryable(orderId).SingleOrDefault<Order>(o => o.TransfereeId == userId);
            }
            else if(userRole == UserRoles.ProgramManager) // for Program Manager
            {
                //PM can access any order.
                return GetOrderById(orderId);
            }
            else if (userRole == UserRoles.Consultant)
            {
                return GetOrderQueryable(orderId).SingleOrDefault<Order>(o => o.ConsultantId == userId);
            }

            return null;
        }

        private IQueryable<Order> GetOrderQueryable(string orderId)
        {
            return _context.Orders
                .Where(o => o.Id == orderId)
                .Include(o => o.Services)
                .Include(o => o.HomeFinding)
                .Include(o => o.Services.Select(s => s.ServiceType))
                .Include(o => o.HomeFinding)
                .Include(o => o.HomeFinding.NumberOfBathrooms)
                .Include(o => o.HomeFinding.HousingType)
                .Include(o => o.HomeFinding.AreaType)
                .Include(o => o.HomeFinding.TransportationType)
                .Include(o => o.HomeFinding.HomeFindingProperties.Select(hfp => hfp.Property.Photos))
                .Include(o => o.DepositType)
                .Include(o => o.BrokerFeeType)
                .Include(o => o.Notifications)
                .Include(o => o.Appointments);
        }

        private IQueryable<Order> GetOrdersQueryable()
        {
            return _context.Orders

                .Where(o => o.SeCustStatus != OrderStatus.Cancelled
                            && o.SeCustStatus != OrderStatus.Closed)
                .Include(o => o.Transferee)
                .Include(o => o.ProgramManager)
                .Include(o => o.Consultant)
                .Include(o => o.HomeFinding)
                .Include(o => o.Services.Select(st => st.ServiceType));
        }



        //public IEnumerable<UserNotification> GetOrderNotification(string userId, string orderid)
        //{
        //    return _context.Orders
        //        .Where(o => o.Id == orderid && o.ConsultantId == userId).ToList<UserNotification>();


        //}
    }
}
