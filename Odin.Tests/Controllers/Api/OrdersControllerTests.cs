using AutoMapper;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Odin.Data.Core;
using Odin.Data.Core.Dtos;
using Odin.Data.Core.Models;
using Odin.Data.Core.Repositories;
using Odin.Interfaces;
using Odin.Tests.Extensions;
using Odin.ViewModels.Orders.Transferee;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;

namespace Odin.Tests.Controllers.Api
{
    [TestClass]
    public class OrdersControllerTests
    {
        private Odin.Controllers.Api.OrdersController _controller;
        private Mock<IOrdersRepository> _mockRepository;
        private Mock<IChildrenRepository> _mockChildrenRepository;
        private Mock<IPetsRepository> _mockPetsRepository;
        private Mock<IAppointmentsRepository> _mockAppointmentsRepository;
        private Mock<IHomeFindingPropertyRepository> _mockHomeFindingPropertiesRepository;
        private Mock<IMapper> _mockMapper;
        private string _userId;
        private string _userName;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockRepository = new Mock<IOrdersRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockChildrenRepository = new Mock<IChildrenRepository>();
            _mockPetsRepository = new Mock<IPetsRepository>();
            _mockAppointmentsRepository = new Mock<IAppointmentsRepository>();
            _mockHomeFindingPropertiesRepository = new Mock<IHomeFindingPropertyRepository>();

            var mockUnitOfWork = new Mock<IUnitOfWork>();
            mockUnitOfWork.SetupGet(u => u.Orders).Returns(_mockRepository.Object);
            mockUnitOfWork.SetupGet(u => u.Children).Returns(_mockChildrenRepository.Object);
            mockUnitOfWork.SetupGet(u => u.Pets).Returns(_mockPetsRepository.Object);
            mockUnitOfWork.SetupGet(u => u.Appointments).Returns(_mockAppointmentsRepository.Object);
            mockUnitOfWork.SetupGet(u => u.HomeFindingProperties).Returns(_mockHomeFindingPropertiesRepository.Object);

            var mockQueueStore = new Mock<IQueueStore>();
            var mockAccountHelper = new Mock<IAccountHelper>();
            _controller = new Odin.Controllers.Api.OrdersController(mockUnitOfWork.Object, _mockMapper.Object, mockQueueStore.Object, mockAccountHelper.Object);

            _userId = "1";
            _userName = "TestUser";
            _controller.MockCurrentUserAndRole(_userId,_userName,UserRoles.Consultant);
        }

        [TestMethod]
        public async Task InviteTransferee_OrderIsNull_ReturnsNotFound()
        {
            var InviteTransfereeDto = new InviteTransfereeDto {OrderId = "asdf"};
            var result = await _controller.InviteTransferee(InviteTransfereeDto);
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public void UpsertDetailsServicesTest_NoServices()
        {
            var orderId = "1";
            
            Order order = new Order() { Id = orderId};
            _mockRepository.Setup(r => r.GetOrderFor(_userId,orderId,UserRoles.Consultant)).Returns(order);
            OrdersTransfereeDetailsServiceDto dto = new OrdersTransfereeDetailsServiceDto() { Id = "1", ScheduledDate = DateTime.Now,  CompletedDate = DateTime.Now};                
            OrdersTransfereeDetailsServicesDto dtos = new OrdersTransfereeDetailsServicesDto() { Id = "1" };
            List<OrdersTransfereeDetailsServiceDto> svc = new List<OrdersTransfereeDetailsServiceDto>();
            svc.Add(dto);
            dtos.Services= svc;
            order.Services.Add(new Service() {Id="1", ScheduledDate=DateTime.Now, CompletedDate=DateTime.Now });
            var result = _controller.UpsertDetailsServices(dtos) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
        }

        [TestMethod]
        public void UpsertDetailsServicesTest_OrderNotFound()
        {
            var orderId = "1";
            Order order = new Order() { Id = orderId };
            _mockRepository.Setup(r => r.GetOrderById(orderId)).Returns(order);
            OrdersTransfereeDetailsServiceDto dto = new OrdersTransfereeDetailsServiceDto() { Id = "1", ScheduledDate = DateTime.Now, CompletedDate = DateTime.Now };
            OrdersTransfereeDetailsServicesDto dtos = new OrdersTransfereeDetailsServicesDto() { Id = "2" };
            List<OrdersTransfereeDetailsServiceDto> svc = new List<OrdersTransfereeDetailsServiceDto>();
            svc.Add(dto);
            dtos.Services = svc;
            order.Services.Add(new Service() { Id = "1", ScheduledDate = DateTime.Now, CompletedDate = DateTime.Now });
            var result = _controller.UpsertDetailsServices(dtos) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [TestMethod]
        public void UpsertDetailsServicesTest_ServiceNotFound()
        {
            var orderId = "1";
            Order order = new Order() { Id = orderId };
            _mockRepository.Setup(r => r.GetOrderById(orderId)).Returns(order);
            OrdersTransfereeDetailsServiceDto dto = new OrdersTransfereeDetailsServiceDto() { Id = "1", ScheduledDate = DateTime.Now, CompletedDate = DateTime.Now };
            OrdersTransfereeDetailsServicesDto dtos = new OrdersTransfereeDetailsServicesDto() { Id = "1" };
            List<OrdersTransfereeDetailsServiceDto> svc = new List<OrdersTransfereeDetailsServiceDto>();
            svc.Add(dto);
            dtos.Services = svc;
            order.Services.Add(new Service() { Id = "2", ScheduledDate = DateTime.Now, CompletedDate = DateTime.Now });
            var result = _controller.UpsertDetailsServices(dtos) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [TestMethod]
        public void UpdateIntakeDestination_ValidDto_ReturnOk()
        {
            var orderId = "1";

            Order order = new Order() { Id = orderId, DestinationCity = "Houston", DestinationCountry = "USA", DestinationState = "Texas"};
            _mockRepository.Setup(r => r.GetOrderFor(_userId,orderId,UserRoles.Consultant)).Returns(order);

            var dto = new OrdersTransfereeIntakeDestinationDto() { Id = orderId, DestinationCountry = "Canada", DestinationCity = "Toronto", DestinationState = "Alberta"};         
            
            var result = _controller.UpdateIntakeDestination(dto) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
        }

        [TestMethod]
        public void UpdateIntakeDestination_NoOrder_ReturnNotFound()
        {
            var orderId = "1";
            Order order = null;

            _mockRepository.Setup(r => r.GetOrderFor(_userId, orderId, UserRoles.Consultant)).Returns(order);

            var dto = new OrdersTransfereeIntakeDestinationDto() { Id = orderId, DestinationCountry = "Canada", DestinationCity = "Toronto", DestinationState = "Alberta" };

            var result = _controller.UpdateIntakeDestination(dto) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [TestMethod]
        public void UpdateIntakeOrigin_ValidDto_ReturnOk()
        {
            var orderId = "1";

            Order order = new Order() { Id = orderId};
            _mockRepository.Setup(r => r.GetOrderFor(_userId, orderId, UserRoles.Consultant)).Returns(order);

            var dto = new OrdersTransfereeIntakeOriginDto() { Id = orderId, OriginCountry = "Canada", OriginCity = "Toronto", OriginState = "Alberta" };

            var result = _controller.UpdateIntakeOrigin(dto) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
        }

        [TestMethod]
        public void UpdateIntakeOrigin_NoOrder_ReturnNotFound()
        {
            var orderId = "1";
            Order order = null;

            _mockRepository.Setup(r => r.GetOrderFor(_userId, orderId, UserRoles.Consultant)).Returns(order);

            var dto = new OrdersTransfereeIntakeOriginDto() { Id = orderId, OriginCountry = "Canada", OriginCity = "Toronto", OriginState = "Alberta" };

            var result = _controller.UpdateIntakeOrigin(dto) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [TestMethod]
        public void UpdateIntakeFamily_ValidDto_ReturnOk()
        {
            var orderId = "1";

            Order order = new Order() { Id = orderId };
            _mockRepository.Setup(r => r.GetOrderFor(_userId, orderId, UserRoles.Consultant)).Returns(order);

            var dto = new OrdersTransfereeIntakeFamilyDto() { Id = orderId, SpouseName = "Test Name"};

            var result = _controller.UpsertIntakeFamily(dto) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
        }

        [TestMethod]
        public void UpsertIntakeFamily_NoOrder_ReturnNotFound()
        {
            var orderId = "1";
            Order order = null;

            _mockRepository.Setup(r => r.GetOrderFor(_userId, orderId, UserRoles.Consultant)).Returns(order);

            var dto = new OrdersTransfereeIntakeFamilyDto() { Id = orderId, SpouseName = "Test Name" };

            var result = _controller.UpsertIntakeFamily(dto) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [TestMethod]
        public void InsertChild_ValidOrder_ReturnOkWithNewChild()
        {
            var orderId = "1";

            Order order = new Order() { Id = orderId };
            _mockRepository.Setup(r => r.GetOrderFor(_userId, orderId, UserRoles.Consultant)).Returns(order);


            var result = _controller.InsertChild(orderId) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.OkNegotiatedContentResult<string>>();
            order.Children.Count.Should().Be(1);
            ((OkNegotiatedContentResult<string>)result).Content.Should().NotBeNullOrEmpty();

        }

        [TestMethod]
        public void InsertChild_NoOrder_ReturnNotFound()
        {
            var orderId = "1";
            Order order = null;
            _mockRepository.Setup(r => r.GetOrderFor(_userId, orderId, UserRoles.Consultant)).Returns(order);

            var result = _controller.InsertChild(orderId) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [TestMethod]
        public void DeleteChild_ValidChild_ReturnOkWithChildDeleted()
        {
            var childId = "1";

            Child child = new Child() { Id = childId };
            _mockChildrenRepository.Setup(r => r.GetChildFor(_userId, childId, UserRoles.Consultant)).Returns(child);

            var result = _controller.DeleteChild(child.Id) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
            child.Deleted.Should().BeTrue();
        }

        [TestMethod]
        public void DeleteChild_NoChild_ReturnNotFound()
        {
            var childId = "1";

            Child child = null;
            _mockChildrenRepository.Setup(r => r.GetChildFor(_userId, childId, UserRoles.Consultant)).Returns(child);

            var result = _controller.DeleteChild(childId) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [TestMethod]
        public void InsertService_ValidOrder_ReturnOkWithNewServiceId()
        {
            var orderId = "1";
            var serviceTypeId = 1;

            Order order = new Order() { Id = orderId };
            _mockRepository.Setup(r => r.GetOrderFor(_userId, orderId, UserRoles.Consultant)).Returns(order);

            var result = _controller.InsertService(order.Id,serviceTypeId) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.OkNegotiatedContentResult<string>>();
            order.Services.Count.Should().Be(1);
            order.Services.FirstOrDefault().ServiceTypeId.Should().Be(serviceTypeId);
            ((OkNegotiatedContentResult<string>)result).Content.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public void InsertService_NoOrder_ReturnNotFound()
        {
            var orderId = "1";
            var serviceTypeId = 1;

            Order order = null;
            _mockRepository.Setup(r => r.GetOrderFor(_userId, orderId, UserRoles.Consultant)).Returns(order);

            var result = _controller.InsertService(orderId, serviceTypeId) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [TestMethod]
        public void UpsertIntakeServices_ValidDto_ReturnOk()
        {
            var orderId = "1";
           
            Order order = new Order(){Id = orderId};
            _mockRepository.Setup(r => r.GetOrderFor(_userId, orderId, UserRoles.Consultant)).Returns(order);

            var dto = new OrdersTransfereeIntakeServicesDto() {Id = orderId};

            var result = _controller.UpdateIntakeServices(dto) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
        }

        [TestMethod]
        public void UpsertIntakeServices_NoOrder_ReturnNotFound()
        {
            var orderId = "1";

            Order order = null;
            _mockRepository.Setup(r => r.GetOrderFor(_userId, orderId, UserRoles.Consultant)).Returns(order);

            var dto = new OrdersTransfereeIntakeServicesDto() { Id = orderId };

            var result = _controller.UpdateIntakeServices(dto) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [TestMethod]
        public void UpdateIntakeRmc_ValidDto_ReturnOk()
        {
            var orderId = "1";

            Order order = new Order() { Id = orderId };
            _mockRepository.Setup(r => r.GetOrderFor(_userId, orderId, UserRoles.Consultant)).Returns(order);

            var dto = new OrdersTransfereeIntakeRmcDto() { Id = orderId };

            var result = _controller.UpdateIntakeRmc(dto) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
        }

        [TestMethod]
        public void UpdateIntakeRmc_NoOrder_ReturnNotFound()
        {
            var orderId = "1";

            Order order = null;
            _mockRepository.Setup(r => r.GetOrderFor(_userId, orderId, UserRoles.Consultant)).Returns(order);

            var dto = new OrdersTransfereeIntakeRmcDto() { Id = orderId };

            var result = _controller.UpdateIntakeRmc(dto) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }


        [TestMethod]
        public void InsertPet_ValidOrder_ReturnOkWithNewPetId()
        {
            var orderId = "1";

            Order order = new Order() { Id = orderId };
            _mockRepository.Setup(r => r.GetOrderFor(_userId, orderId, UserRoles.Consultant)).Returns(order);

            var result = _controller.InsertPet(orderId) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.OkNegotiatedContentResult<string>>();
            order.Pets.Count.Should().Be(1);
            ((OkNegotiatedContentResult<string>)result).Content.Should().NotBeNullOrEmpty();

        }

        [TestMethod]
        public void InsertPet_NoOrder_ReturnNotFound()
        {
            var orderId = "1";
            Order order = null;
            _mockRepository.Setup(r => r.GetOrderFor(_userId, orderId, UserRoles.Consultant)).Returns(order);

            var result = _controller.InsertPet(orderId) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [TestMethod]
        public void DeletePet_ValidPet_ReturnOkWithPetDeleted()
        {
            var petId = "1";

            Pet pet = new Pet() { Id = petId };
            _mockPetsRepository.Setup(r => r.GetPetFor(_userId, petId, UserRoles.Consultant)).Returns(pet);

            var result = _controller.DeletePet(pet.Id) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
            pet.Deleted.Should().BeTrue();
        }

        [TestMethod]
        public void DeletePet_NoPet_ReturnNotFound()
        {
            var petId = "1";

            Pet pet = null;
            _mockPetsRepository.Setup(r => r.GetPetFor(_userId, petId, UserRoles.Consultant)).Returns(pet);

            var result = _controller.DeletePet(petId) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [TestMethod]
        public void UpdateIntakeTempHousing_ValidDto_ReturnOk()
        {
            var orderId = "1";

            Order order = new Order() { Id = orderId };
            _mockRepository.Setup(r => r.GetOrderFor(_userId, orderId, UserRoles.Consultant)).Returns(order);

            var dto = new OrdersTransfereeIntakeTempHousingDto() { Id = orderId };

            var result = _controller.UpdateIntakeTempHousing(dto) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
        }

        [TestMethod]
        public void UpdateIntakeTempHousing_ValidOrder_ReturnNotFound()
        {
            var orderId = "1";

            Order order = null;
            _mockRepository.Setup(r => r.GetOrderFor(_userId, orderId, UserRoles.Consultant)).Returns(order);

            var dto = new OrdersTransfereeIntakeTempHousingDto() { Id = orderId };

            var result = _controller.UpdateIntakeTempHousing(dto) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [TestMethod]
        public void UpsertIntakeHomeFinding_ValidDto_ReturnOk()
        {
            var orderId = "1";

            Order order = new Order() { Id = orderId };
            _mockRepository.Setup(r => r.GetOrderFor(_userId, orderId, UserRoles.Consultant)).Returns(order);

            var dto = new OrdersTransfereeIntakeHomeFindingDto() { Id = orderId };

            var result = _controller.UpsertIntakeHomeFinding(dto) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
        }

        [TestMethod]
        public void UpsertIntakeHomeFinding_ValidOrder_ReturnNotFound()
        {
            var orderId = "1";

            Order order = null;
            _mockRepository.Setup(r => r.GetOrderFor(_userId, orderId, UserRoles.Consultant)).Returns(order);

            var dto = new OrdersTransfereeIntakeHomeFindingDto() { Id = orderId };

            var result = _controller.UpsertIntakeHomeFinding(dto) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [TestMethod]
        public void UpdateIntakeLease_ValidDto_ReturnOk()
        {
            var orderId = "1";

            Order order = new Order() { Id = orderId };
            _mockRepository.Setup(r => r.GetOrderFor(_userId, orderId, UserRoles.Consultant)).Returns(order);

            var dto = new OrdersTransfereeIntakeLeaseDto() { Id = orderId };

            var result = _controller.UpdateIntakeLease(dto) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
        }

        [TestMethod]
        public void UpdateIntakeLease_ValidOrder_ReturnNotFound()
        {
            var orderId = "1";

            Order order = null;
            _mockRepository.Setup(r => r.GetOrderFor(_userId, orderId, UserRoles.Consultant)).Returns(order);

            var dto = new OrdersTransfereeIntakeLeaseDto() { Id = orderId };

            var result = _controller.UpdateIntakeLease(dto) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [TestMethod]
        public void UpdateIntakeRelocation_ValidDto_ReturnOk()
        {
            var orderId = "1";

            Order order = new Order() { Id = orderId };
            _mockRepository.Setup(r => r.GetOrderFor(_userId, orderId, UserRoles.Consultant)).Returns(order);

            var dto = new OrdersTransfereeIntakeRelocationDto() { Id = orderId };

            var result = _controller.UpdateIntakeRelocation(dto) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
        }

        [TestMethod]
        public void UpdateIntakeRelocation_ValidOrder_ReturnNotFound()
        {
            var orderId = "1";

            Order order = null;
            _mockRepository.Setup(r => r.GetOrderFor(_userId, orderId, UserRoles.Consultant)).Returns(order);

            var dto = new OrdersTransfereeIntakeRelocationDto() { Id = orderId };

            var result = _controller.UpdateIntakeRelocation(dto) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void UpdateHousingProperty_ValidOrder_ReturnFound()
        {
            var propertyId = "1";
            
            HomeFindingProperty p1 = new HomeFindingProperty();
            p1.Deleted = false;
            p1.Property = new Property() { Id = propertyId, Amount = 10, NumberOfBathrooms = 5, NumberOfBedrooms = 2, SquareFootage = 2343 };
            p1.ViewingDate = DateTime.Now.AddDays(10);

            _mockHomeFindingPropertiesRepository.Setup(r => r.GetHomeFindingPropertyById(propertyId)).Returns(p1);
            
            
            HousingPropertyViewModel propertyVM = new HousingPropertyViewModel() { Id = propertyId, PropertyNumberOfBathrooms = 10, PropertyNumberOfBedrooms = 12 , PropertyAmount = 2002, PropertySquareFootage = 1023 };
            
            var result = _controller.UpdateHousingProperty(propertyVM) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
            p1.Property.NumberOfBedrooms.Equals(propertyVM.PropertyNumberOfBedrooms);
            p1.Property.NumberOfBathrooms.Equals(propertyVM.PropertyNumberOfBathrooms);
            p1.Property.Amount.Should().Equals(propertyVM.PropertyAmount);
            p1.Property.SquareFootage.Equals(propertyVM.PropertySquareFootage);
        }
    }
}
