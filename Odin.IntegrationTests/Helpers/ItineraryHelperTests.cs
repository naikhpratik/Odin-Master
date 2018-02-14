using AutoMapper;
using FluentAssertions;
using NUnit.Framework;
using Odin.Data.Builders;
using Odin.Data.Core.Models;
using Odin.Data.Persistence;
using Odin.Helpers;
using Odin.IntegrationTests.TestAttributes;
using Odin.ViewModels.Orders.Transferee;
using System;
using System.Linq;
using HomeFindingProperty = Odin.Data.Core.Models.HomeFindingProperty;

namespace Odin.IntegrationTests.Helpers
{
    [TestFixture]
    class ItineraryHelperTests : WebApiBaseTest
    {
        private ItineraryHelper _itineraryHelper;
        private MapperConfiguration config;
        private IMapper mapper;
        private UnitOfWork unitOfWork;
        private Service svc;
        private Appointment appt;

        [SetUp]
        public void Setup()
        {
            config = new MapperConfiguration(c => c.AddProfile(new MappingProfile()));
            mapper = config.CreateMapper();
            unitOfWork = new UnitOfWork(Context);
            _itineraryHelper = new ItineraryHelper(unitOfWork, mapper);           
        }
        [Test, Isolated]
        public void Build_WhenCalled_Should_Return_Two_Tasks()
        {
            // Arrange
            OrdersTransfereeItineraryViewModel itin = new OrdersTransfereeItineraryViewModel();
            var order = setUpOrder();

            svc = new Service()
            {
                OrderId = order.Id,
                ServiceTypeId = 1,
                Notes = "Integrated service test",
                Deleted = false,
                ScheduledDate = DateTime.Now,
                Selected = true
            };
            appt = new Appointment()
            {
                OrderId = order.Id,
                ScheduledDate = DateTime.Now.AddDays(1),
                Deleted = false,
                Description = "Integrated appointment test"
            };
            //act
            order.Services.Add(svc);
            order.Appointments.Add(appt);
            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();
            //assert
            var result = _itineraryHelper.Build(order);
            result.Itinerary.ToList().Count().Should().Equals(2);
        }
        [Test, Isolated]
        public void Build_WhenCalled_Should_Return_No_Tasks()
        {
            // Arrange
            OrdersTransfereeItineraryViewModel itin = new OrdersTransfereeItineraryViewModel();
            var order = setUpOrder();
            //act
            svc = new Service();
            order.Services.Add(svc);
            order.Services.Remove(svc);
            appt = new Appointment();
            order.Appointments.Add(appt);
            order.Appointments.Remove(appt);
            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();
            //assert
            var result = _itineraryHelper.Build(order);
            result.Itinerary.ToList().Count().Should().Equals(0);
        }
        [Test, Isolated]
        public void Build_WhenCalled_OneService_Should_Return_One_Task()
        {
            // Arrange
            OrdersTransfereeItineraryViewModel itin = new OrdersTransfereeItineraryViewModel();
            var order = setUpOrder();

            svc = new Service()
            {
                OrderId = order.Id,
                ServiceTypeId = 1,
                Notes = "Integrated service test",
                Deleted = false,
                ScheduledDate = DateTime.Now,
                Selected = true
            };
            appt = new Appointment();
            order.Appointments.Add(appt);
            order.Appointments.Remove(appt);
            //act
            order.Services.Add(svc);            
            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();
            //assert
            var result = _itineraryHelper.Build(order);
            result.Itinerary.ToList().Count().Should().Equals(1);
        }
        [Test, Isolated]
        public void Build_WhenCalled_OneAppointment_Should_Return_One_Task()
        {
            // Arrange
            OrdersTransfereeItineraryViewModel itin = new OrdersTransfereeItineraryViewModel();
            var order = setUpOrder();

            svc = new Service();
            order.Services.Add(svc);
            order.Services.Remove(svc);
            appt = new Appointment()
            {
                OrderId = order.Id,
                ScheduledDate = DateTime.Now.AddDays(1),
                Deleted = false,
                Description = "Integrated appointment test"
            };
            //act            
            order.Appointments.Add(appt);
            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();
            //assert
            var result = _itineraryHelper.Build(order);
            result.Itinerary.ToList().Count().Should().Equals(1);
        }
        [Test, Isolated]
        public void Build_WhenCalled_WrongOrderId_Should_Return_Empty_Itinerary()
        {
            // Arrange
            OrdersTransfereeItineraryViewModel itin = new OrdersTransfereeItineraryViewModel();
            var order = setUpOrder();

            svc = new Service()
            {
                OrderId = order.Id,
                ServiceTypeId = 1,
                Notes = "Integrated service test",
                Deleted = false,
                ScheduledDate = DateTime.Now,
                Selected = true
            };
            appt = new Appointment()
            {
                OrderId = order.Id,
                ScheduledDate = DateTime.Now.AddDays(1),
                Deleted = false,
                Description = "Integrated appointment test"
            };
            //act
            order.Services.Add(svc);
            order.Appointments.Add(appt);
            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();
            //assert
            var result = _itineraryHelper.Build(null);
            result.Should().BeNull();
        }

        [Test, Isolated]
        public void Build_Order_ShouldHaveOneServiceOnIntinerary()
        {
            Order order = new Order()
            {
                Id = "1",
                ServiceFlag = (int)ServiceCategory.AccompaniedHomeFinding
            };

            order.Services.Add(
                new Service()
                {
                    ServiceType = new ServiceType() { Name = "Unscheduled" },
                    ScheduledDate = null
                }
            );

            order.Services.Add(
                new Service()
                {
                    ServiceType = new ServiceType() { Name = "Completed" },
                    ScheduledDate = DateTime.Now,
                    CompletedDate = DateTime.Now
                }
            );

            order.Services.Add(
                new Service()
                {
                    ServiceType = new ServiceType() { Name = "Scheduled" },
                    ScheduledDate = DateTime.Now.AddDays(1)
                }
            );
            
            _itineraryHelper = new ItineraryHelper(unitOfWork,mapper);

            var result = _itineraryHelper.Build(order);
            result.Itinerary.Count().Should().Be(1);
        }

        [Test, Isolated]
        public void Build_Order_ShouldHaveOneAppointmentOnIntinerary()
        {
            Order order = new Order()
            {
                Id = "1",
                ServiceFlag = (int)ServiceCategory.AccompaniedHomeFinding
            };

            order.Appointments.Add(
                new Appointment(){
                    Description = "Deleted",
                    ScheduledDate = DateTime.Now.AddDays(1),
                    Deleted = true
                }
            );

            order.Appointments.Add(
                new Appointment()
                {
                    Description = "Past",
                    ScheduledDate = DateTime.Now.AddDays(-1)
                }
            );

            order.Appointments.Add(
                new Appointment()
                {
                    Description = "Scheduled",
                    ScheduledDate = DateTime.Now.AddDays(1)
                }
            );

            _itineraryHelper = new ItineraryHelper(unitOfWork, mapper);

            var result = _itineraryHelper.Build(order);
            result.Itinerary.Count().Should().Be(1);
        }

        [Test, Isolated]
        public void Build_HomeFindingOrder_ShouldHaveOneViewingOnIntinerary()
        {
            Order order = new Order()
            {
                Id = "1",
                ServiceFlag = (int)ServiceCategory.AccompaniedHomeFinding,
                HomeFinding = new HomeFinding()
            };

            order.HomeFinding.HomeFindingProperties.Add(
                new HomeFindingProperty()
                {
                    ViewingDate = DateTime.Now.AddDays(1),
                    Deleted = true
                }
            );

            order.HomeFinding.HomeFindingProperties.Add(
                new HomeFindingProperty()
                {
                    ViewingDate = null
                }
            );

            order.HomeFinding.HomeFindingProperties.Add(
                new HomeFindingProperty()
                {
                    ViewingDate = DateTime.Now.AddDays(1)
                }
            );

            _itineraryHelper = new ItineraryHelper(unitOfWork, mapper);

            var result = _itineraryHelper.Build(order);
            result.Itinerary.Count().Should().Be(1);
        }

        [Test, Isolated]
        public void Build_NotHomeFindingOrder_ShouldHaveNoViewingsOnIntinerary()
        {
            Order order = new Order()
            {
                Id = "1",
                HomeFinding = new HomeFinding()
            };

            order.HomeFinding.HomeFindingProperties.Add(
                new HomeFindingProperty()
                {
                    ViewingDate = DateTime.Now.AddDays(1),
                    Deleted = true
                }
            );

            order.HomeFinding.HomeFindingProperties.Add(
                new HomeFindingProperty()
                {
                    ViewingDate = null
                }
            );

            order.HomeFinding.HomeFindingProperties.Add(
                new HomeFindingProperty()
                {
                    ViewingDate = DateTime.Now.AddDays(1)
                }
            );

            _itineraryHelper = new ItineraryHelper(unitOfWork, mapper);

            var result = _itineraryHelper.Build(order);
            result.Itinerary.Count().Should().Be(0);
        }

        [Test, Isolated]
        public void Build_HomeFindingOrder_ShouldHaveViewingServiceAppointmentOnIntinerary()
        {
            Order order = new Order()
            {
                Id = "1",
                HomeFinding = new HomeFinding(),
                ServiceFlag = (int)ServiceCategory.AccompaniedHomeFinding
            };

            order.HomeFinding.HomeFindingProperties.Add(
                new HomeFindingProperty()
                {
                    ViewingDate = DateTime.Now.AddDays(1)
                }
            );

            order.Appointments.Add(
                new Appointment()
                {
                    Description = "Scheduled",
                    ScheduledDate = DateTime.Now.AddDays(1)
                }
            );

            order.Services.Add(
                new Service()
                {
                    ServiceType = new ServiceType() { Name = "Scheduled" },
                    ScheduledDate = DateTime.Now.AddDays(1)
                }
            );

            _itineraryHelper = new ItineraryHelper(unitOfWork, mapper);

            var result = _itineraryHelper.Build(order);
            result.Itinerary.Count().Should().Be(3);
        }

        private Order setUpOrder()
        {
            var order = OrderBuilder.New().First();
            order.Transferee = transferee;
            order.ProgramManager = pm;
            order.Consultant = dsc;
            return order;
        }
    }
}
