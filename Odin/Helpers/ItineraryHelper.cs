using AutoMapper;
using Odin.Data.Core;
using Odin.Data.Core.Models;
using Odin.ViewModels.Orders.Transferee;
using Odin.ViewModels.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Odin.Helpers
{
    public class ItineraryHelper
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ItineraryHelper(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public OrdersTransfereeItineraryViewModel Build(Order order)
        {
            if (order == null)
            {
                return null;
            }

            var itinerary = GetItinerary(order);
            OrdersTransfereeItineraryViewModel vm = new OrdersTransfereeItineraryViewModel();
            vm.Itinerary = itinerary;
            return vm;
        }

        public IEnumerable<ItineraryEntryViewModel> GetItinerary(Order order)
        {
            var itinServices =
                order.Services.Where(s => s.CompletedDate.HasValue == false && s.ScheduledDate >= DateTime.Now);

            var itinAppointments = order.Appointments.Where(a => a.Deleted == false && a.ScheduledDate >= DateTime.Now);

            IEnumerable<HomeFindingProperty> itinViewings; 
            if (order.HasHomeFinding)
            {
                itinViewings =
                    order.HomeFinding.HomeFindingProperties.Where(hfp =>
                        hfp.Deleted == false && hfp.ViewingDate >= DateTime.Now);
            }
            else
            {
                itinViewings = new List<HomeFindingProperty>();
            }
            
            var itinerary1 = _mapper.Map<IEnumerable<Service>, IEnumerable<ItineraryEntryViewModel>>(itinServices);
            var itinerary2 = _mapper.Map<IEnumerable<Appointment>, IEnumerable<ItineraryEntryViewModel>>(itinAppointments);
            var itinerary3 = _mapper.Map<IEnumerable<HomeFindingProperty>, IEnumerable<ItineraryEntryViewModel>>(itinViewings);

            var itinerary = itinerary1.Concat(itinerary2).Concat(itinerary3).OrderBy(s => s.ScheduledDate);
            return itinerary;
        }
    }
}