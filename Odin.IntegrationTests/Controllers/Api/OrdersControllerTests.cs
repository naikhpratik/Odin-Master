using AutoMapper;
using FluentAssertions;
using NUnit.Framework;
using Odin.Controllers.Api;
using Odin.Data.Builders;
using Odin.Data.Core.Dtos;
using Odin.Data.Core.Models;
using Odin.Data.Helpers;
using Odin.Data.Persistence;
using Odin.Domain;
using Odin.Extensions;
using Odin.Helpers;
using Odin.IntegrationTests.Extensions;
using Odin.IntegrationTests.TestAttributes;
using Odin.ViewModels.Orders.Transferee;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http.Results;

namespace Odin.IntegrationTests.Controllers.Api
{
    [TestFixture]
    public class OrdersControllerTests : WebApiBaseTest
    {
        private OrdersController SetUpOrdersController()
        {
            var config = new MapperConfiguration(c => c.AddProfile(new MappingProfile()));
            var mapper = config.CreateMapper();
            var unitOfWork = new UnitOfWork(Context);
            return new OrdersController(unitOfWork, mapper, new QueueStore(), new AccountHelper(new EmailHelper()));
        }

        [Test, Isolated]
        public async Task GetOrders_ValidRequests_ShouldReturnOrders()
        {
            // Arrange
            var orders = OrderBuilder.New(2);
            orders.ForEach(o => o.ConsultantId = dsc.Id);
            orders.ForEach(o => o.TransfereeId = transferee.Id);
            orders.ForEach(o => o.ProgramManagerId = pm.Id);
            orders.ForEach(o => o.TrackingId = TokenHelper.NewToken());
            var testDateTime = new DateTime(1999, 6, 14);
            orders.ForEach(o => o.PreTripDate = testDateTime);

            Context.Orders.AddRange(orders);
            Context.SaveChanges();

            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            
            // Act
            var result = controller.GetOrders();
            var resultContent = result.GetContent<OrderIndexDto>();

            // Assert
            result.Should().BeOfType<OkNegotiatedContentResult<OrderIndexDto>>();
            resultContent.Transferees.Should().NotBeNull();
            var newTransferees = resultContent.Transferees.Where(t => t.PreTrip == testDateTime);
            newTransferees.Should().HaveCount(2);
        }

        [Test, CleanData]
        public async Task UpsertOrder_ValidUpdateRequest_ShouldNotUpdateNonDtoFields()
        {
            // Arrange
            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            Context.Orders.Add(order);
            Context.SaveChanges();
            var orderDto = OrderDtoBuilder.New();
            orderDto.TrackingId = order.TrackingId;
            orderDto.ProgramManager = ProgramManagerDtoBuilder.New();
            orderDto.ProgramManager.SeContactUid = pm.SeContactUid.Value;
            orderDto.Consultant = ConsultantDtoBuilder.New();
            orderDto.Consultant.SeContactUid = dsc.SeContactUid.Value;
            orderDto.Transferee = TransfereeDtoBuilder.New();
            orderDto.Transferee.Email = transferee.Email;

            // Act
            var request = CreateRequest("api/orders", "application/json", HttpMethod.Post, orderDto);
            request.Headers.Add("Token", ApiKey);
            var response = await Server.HttpClient.SendAsync(request);
            var errorResponse = await response.ReadContentAsyncSafe<ErrorResponse>();

            // Assert
            errorResponse?.errors.Should().BeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            Context.Entry(order).Reload();
            order.FamilyDetails.Should().NotBeNullOrEmpty();
        }

        [Test, CleanData]
        public async Task UpsertOrder_ValidInsertRequest_CreatesOrderRecordWithHomefinding()
        {
            // Arrange
            var orderDto = OrderDtoBuilder.New();
            orderDto.ProgramManager = ProgramManagerDtoBuilder.New();
            orderDto.ProgramManager.SeContactUid = pm.SeContactUid.Value;
            orderDto.Transferee = TransfereeDtoBuilder.New();
            orderDto.Transferee.Email = "integration@test.com";
            orderDto.Consultant = ConsultantDtoBuilder.New();
            orderDto.Consultant.SeContactUid = dsc.SeContactUid.Value;
            orderDto.ServiceFlag = (int) ServiceCategory.AccompaniedHomeFinding;

            // Act                
            var request = CreateRequest("api/orders", "application/json", HttpMethod.Post, orderDto);
            request.Headers.Add("Token", ApiKey);
            var response = await Server.HttpClient.SendAsync(request);
            var errorResponse = await response.ReadContentAsyncSafe<ErrorResponse>();

            // Assert
            errorResponse?.errors.Should().BeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var order = Context.Orders
                .Where(o => o.TrackingId.Equals(orderDto.TrackingId))
                .Include(o => o.HomeFinding)
                .FirstOrDefault();

            order.Should().NotBeNull();
            order?.HomeFinding.Should().NotBeNull();
        }

        [Test, CleanData]
        public async Task UpsertOrder_ValidInsertRequest_CreatesOrderRecordWithOutHomefinding()
        {
            // Arrange
            var orderDto = OrderDtoBuilder.New();
            orderDto.ProgramManager = ProgramManagerDtoBuilder.New();
            orderDto.ProgramManager.SeContactUid = pm.SeContactUid.Value;
            orderDto.Transferee = TransfereeDtoBuilder.New();
            orderDto.Transferee.Email = "integration@test.com";
            orderDto.Consultant = ConsultantDtoBuilder.New();
            orderDto.Consultant.SeContactUid = dsc.SeContactUid.Value;
            
            // Act                
            var request = CreateRequest("api/orders", "application/json", HttpMethod.Post, orderDto);
            request.Headers.Add("Token", ApiKey);
            var response = await Server.HttpClient.SendAsync(request);
            var errorResponse = await response.ReadContentAsyncSafe<ErrorResponse>();

            // Assert
            errorResponse?.errors.Should().BeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var order = Context.Orders
                .Where(o => o.TrackingId.Equals(orderDto.TrackingId))
                .Include(o => o.HomeFinding)
                .FirstOrDefault();

            order.Should().NotBeNull();
            order?.HomeFinding.Should().BeNull();
        }

        [Test, CleanData]
        public async Task UpsertOrder_InsertWithNewTransferee_CreatesTransfereeAndOrder()
        {
            // Arrange
            var orderDto = OrderDtoBuilder.New();
            orderDto.ProgramManager = ProgramManagerDtoBuilder.New();
            orderDto.ProgramManager.SeContactUid = pm.SeContactUid.Value;
            orderDto.Transferee = TransfereeDtoBuilder.New();
            orderDto.Transferee.Email = "integration@test.com";
            orderDto.Consultant = ConsultantDtoBuilder.New();
            orderDto.Consultant.SeContactUid = dsc.SeContactUid.Value;
            Context.Transferees.SingleOrDefault(t => t.Email.Equals("integration@test.com")).Should().BeNull();
            Context.Orders.SingleOrDefault(o => o.TrackingId.Equals(orderDto.TrackingId)).Should().BeNull();

            // Act
            var request = CreateRequest("api/orders", "application/json", HttpMethod.Post, orderDto);
            request.Headers.Add("Token", ApiKey);
            var response = await Server.HttpClient.SendAsync(request);
            var errorResponse = await response.ReadContentAsyncSafe<ErrorResponse>();

            // Assert
            errorResponse?.errors.Should().BeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var newTransferee = Context.Transferees.SingleOrDefault(t => t.Email.Equals("integration@test.com"));
            newTransferee.Should().NotBeNull();
            newTransferee?.InviteStatus.Should().Be(InviteStatus.NotYetInvited);
            var newOrder = Context.Orders.SingleOrDefault(o => o.TrackingId.Equals(orderDto.TrackingId));
            newOrder.Should().NotBeNull();
        }

        /// <summary>
        /// Test Not Clear
        /// </summary>
        [Test, CleanData]
        public async Task UpsertOrder_InsertWithExistingTransferee_CreatesOrder()
        {
            // Arrange
            var orderDto = OrderDtoBuilder.New();
            orderDto.ProgramManager = ProgramManagerDtoBuilder.New();
            orderDto.ProgramManager.SeContactUid = pm.SeContactUid.Value;
            orderDto.Transferee = TransfereeDtoBuilder.New();
            orderDto.Transferee.Email = transferee.Email;
            orderDto.Consultant = ConsultantDtoBuilder.New();
            orderDto.Consultant.SeContactUid = dsc.SeContactUid.Value;
            Context.Orders.SingleOrDefault(o => o.TrackingId.Equals(orderDto.TrackingId)).Should().BeNull();

            // Act
            var request = CreateRequest("api/orders", "application/json", HttpMethod.Post, orderDto);
            request.Headers.Add("Token", ApiKey);
            var response = await Server.HttpClient.SendAsync(request);
            var errorResponse = await response.ReadContentAsyncSafe<ErrorResponse>();

            // Assert
            errorResponse?.errors.Should().BeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            Context.Orders.SingleOrDefault(o => o.TrackingId.Equals(orderDto.TrackingId)).Should().NotBeNull();
        }

        [Test, CleanData]
        public async Task UpsertOrder_UpdateOrder_ShouldUpdateFields()
        {
            // Arrange
            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            order.DestinationCity = "test-before-insert";
            order.HomeFinding = new HomeFinding { Id = order.Id };
            Context.Orders.Add(order);
            Context.SaveChanges();
            var orderDto = OrderDtoBuilder.New();
            orderDto.TrackingId = order.TrackingId;
            orderDto.ProgramManager = ProgramManagerDtoBuilder.New();
            orderDto.ProgramManager.SeContactUid = pm.SeContactUid.Value;
            orderDto.Consultant = ConsultantDtoBuilder.New();
            orderDto.Consultant.SeContactUid = dsc.SeContactUid.Value;
            orderDto.Transferee = TransfereeDtoBuilder.New();
            orderDto.Transferee.Email = transferee.Email;
            orderDto.DestinationCity = "integration-test-city";

            // Act
            var request = CreateRequest("api/orders", "application/json", HttpMethod.Post, orderDto);
            request.Headers.Add("Token", ApiKey);
            var response = await Server.HttpClient.SendAsync(request);
            var errorResponse = await response.ReadContentAsyncSafe<ErrorResponse>();

            // Assert
            errorResponse?.errors.Should().BeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            Context.Entry(order).Reload();
            order.DestinationCity.Should().Be(orderDto.DestinationCity);
        }

        [Test, CleanData]
        public async Task UpsertOrder_UpdateOrderWithNewEeEmail_ShouldCreateNewTransferee()
        {
            // Arrange
            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            Context.Orders.Add(order);
            Context.SaveChanges();
            var orderDto = OrderDtoBuilder.New();
            orderDto.TrackingId = order.TrackingId;
            orderDto.ProgramManager = ProgramManagerDtoBuilder.New();
            orderDto.ProgramManager.SeContactUid = pm.SeContactUid.Value;
            orderDto.Consultant = ConsultantDtoBuilder.New();
            orderDto.Consultant.SeContactUid = dsc.SeContactUid.Value;
            orderDto.Transferee = TransfereeDtoBuilder.New();
            orderDto.Transferee.Email = "integration@test.com";
            
            // Act
            var request = CreateRequest("api/orders", "application/json", HttpMethod.Post, orderDto);
            request.Headers.Add("Token", ApiKey);
            var response = await Server.HttpClient.SendAsync(request);
            var errorResponse = await response.ReadContentAsyncSafe<ErrorResponse>();

            // Assert
            errorResponse?.errors.Should().BeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            Context.Transferees.SingleOrDefault(t => t.Email.Equals(orderDto.Transferee.Email)).Should().NotBeNull();
        }

        [Test, CleanData]
        public async Task UpsertOrder_DifferentPm_ShouldChangePmAssigned()
        {
            // Arrange
            var secondPm = Context.Managers.Add(new Manager
            {
                UserName = "integrationee@dwellworks.com",
                FirstName = "user2",
                Email = "integrationee@dwellworks.com",
                PasswordHash = "-",
                SeContactUid = 99999
            });
            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            Context.Orders.Add(order);
            Context.SaveChanges();
            var orderDto = OrderDtoBuilder.New();
            orderDto.TrackingId = order.TrackingId;
            orderDto.ProgramManager = ProgramManagerDtoBuilder.New();
            orderDto.ProgramManager.SeContactUid = secondPm.SeContactUid.Value;
            orderDto.Consultant = ConsultantDtoBuilder.New();
            orderDto.Consultant.SeContactUid = dsc.SeContactUid.Value;
            orderDto.Transferee = TransfereeDtoBuilder.New();
            orderDto.Transferee.Email = transferee.Email;
            Context.Entry(order).Reload();
            order.ProgramManager.SeContactUid.Value.Should().Be(pm.SeContactUid.Value);

            // Act
            var request = CreateRequest("api/orders", "application/json", HttpMethod.Post, orderDto);
            request.Headers.Add("Token", ApiKey);
            var response = await Server.HttpClient.SendAsync(request);
            var errorResponse = await response.ReadContentAsyncSafe<ErrorResponse>();

            // Assert
            errorResponse?.errors.Should().BeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            Context.Entry(order).Reload();
            order.ProgramManager.SeContactUid.Value.Should().Be(secondPm.SeContactUid.Value);
        }

        [Test, CleanData]
        public async Task UpsertOrder_DifferentDsc_ShouldChangeDscAssigned()
        {
            // Arrange
            var secondConsultant = Context.Consultants.Add(new Consultant()
            {
                UserName = "integrationc@dwellworks.com",
                FirstName = "user2",
                Email = "integrationc@dwellworks.com",
                PasswordHash = "-",
                SeContactUid = 99999
            });
            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            Context.Orders.Add(order);
            Context.SaveChanges();
            var orderDto = OrderDtoBuilder.New();
            orderDto.TrackingId = order.TrackingId;
            orderDto.ProgramManager = ProgramManagerDtoBuilder.New();
            orderDto.ProgramManager.SeContactUid = pm.SeContactUid.Value;
            orderDto.Consultant = ConsultantDtoBuilder.New();
            orderDto.Consultant.SeContactUid = secondConsultant.SeContactUid.Value;
            orderDto.Transferee = TransfereeDtoBuilder.New();
            orderDto.Transferee.Email = transferee.Email;
            Context.Entry(order).Reload();
            order.Consultant.SeContactUid.Value.Should().Be(dsc.SeContactUid.Value);

            // Act
            var request = CreateRequest("api/orders", "application/json", HttpMethod.Post, orderDto);
            request.Headers.Add("Token", ApiKey);
            var response = await Server.HttpClient.SendAsync(request);
            var errorResponse = await response.ReadContentAsyncSafe<ErrorResponse>();

            // Assert
            errorResponse?.errors.Should().BeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            Context.Entry(order).Reload();
            order.Consultant.SeContactUid.Value.Should().Be(secondConsultant.SeContactUid.Value);
        }


        [Test, CleanData]
        public async Task UpsertOrder_IncorrectToken_Returns401()
        {
            // Arrange
            var orderDto = OrderDtoBuilder.New();
            orderDto.TrackingId = TokenHelper.NewToken();
            orderDto.ProgramManager = ProgramManagerDtoBuilder.New();
            orderDto.ProgramManager.SeContactUid = pm.SeContactUid.Value;
            orderDto.Consultant = ConsultantDtoBuilder.New();
            orderDto.Consultant.SeContactUid = dsc.SeContactUid.Value;
            orderDto.Transferee = TransfereeDtoBuilder.New();
            orderDto.Transferee.Email = transferee.Email;

            // Act
            var request = CreateRequest("api/orders", "application/json", HttpMethod.Post, orderDto);
            request.Headers.Add("Token", "--IncorrectToken--");
            var response = await Server.HttpClient.SendAsync(request);
            var errorResponse = await response.ReadContentAsyncSafe<ErrorResponse>();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            //errorResponse.errors.Should().NotBeNull();
            //errorResponse?.errors?.First().Should().Contain("authorized");
        }

        //TODO: UpsertOrder_OnException_Returns501()
        //TODO: UpsertOrder_DtoMissingFields_ReturnsArrayOfErrors()

        [Test, Isolated]
        public async Task UpsertOrderDetails_UpdateExistingService_ShouldChangeDate()
        {
            // arrange
            ServiceType serviceType = Context.ServiceTypes.First();

            Service service = new Service()
            {
                ServiceType = serviceType,
                ServiceTypeId = serviceType.Id,
                ScheduledDate = DateTime.Now,
                CompletedDate = DateTime.Now,
            };

            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            order.Services.Add(service);
            order.DestinationCity = "integration city dsqd";
            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();

            //modify the service
           
            DateTime changedDate = DateTime.Now.AddDays(4);
            List<OrdersTransfereeDetailsServiceDto> oTranDetailServices = new List<OrdersTransfereeDetailsServiceDto>();
            OrdersTransfereeDetailsServiceDto oTranDetailService = new OrdersTransfereeDetailsServiceDto() { Id = service.Id, ScheduledDate = changedDate };
            oTranDetailServices.Add(oTranDetailService);
            OrdersTransfereeDetailsServicesDto svc = new OrdersTransfereeDetailsServicesDto();
            svc.Services = oTranDetailServices;
            svc.Id = order.Id;

            // Act
            var controller = SetUpOrdersController();
            //var order = controller.GetOrders
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.UpsertDetailsServices(svc);

            // Assert
            Context.Entry(order).Reload();
            Context.Entry(service).Reload();
            service.ScheduledDate.ToString().Should().Be(changedDate.ToString());
        }

        [Test, Isolated]
        public async Task UpsertOrderDetails_UpdateExistingService_ShouldChangeNotes()
        {
            // arrange
            ServiceType serviceType = Context.ServiceTypes.First();

            Service service = new Service()
            {
                ServiceType = serviceType,
                ServiceTypeId = serviceType.Id,
                ScheduledDate = DateTime.Now,
                CompletedDate = DateTime.Now,
                Notes = String.Empty
            };

            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            order.Services.Add(service);
            order.DestinationCity = "integration city In Mumbai";
            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();

            //modify the service
            string notes = "Added notes.";
            List<OrdersTransfereeDetailsServiceDto> oTranDetailServices = new List<OrdersTransfereeDetailsServiceDto>();
            OrdersTransfereeDetailsServiceDto oTranDetailService = new OrdersTransfereeDetailsServiceDto() { Id = service.Id, Notes = notes };
            oTranDetailServices.Add(oTranDetailService);
            OrdersTransfereeDetailsServicesDto svc = new OrdersTransfereeDetailsServicesDto();
            svc.Services = oTranDetailServices;
            svc.Id = order.Id;

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.UpsertDetailsServices(svc);

            // Assert
            Context.Entry(order).Reload();
            Context.Entry(service).Reload();
            service.Notes.Should().Be(notes);
        }
        
        //saved
        [Test, Isolated]
        public async Task UpsertOrderDetails_ServiceDoesNotExist_ShouldReturnNotFound()
        {
            // arrange
            ServiceType serviceType = Context.ServiceTypes.First();

            Service service = new Service()
            {
                ServiceType = serviceType,
                ServiceTypeId = serviceType.Id,
                ScheduledDate = DateTime.Now,
                CompletedDate = DateTime.Now,
            };

            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            order.Services.Add(service);
            order.DestinationCity = "integration city";
            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();

            //modify the service
            DateTime changedDate = DateTime.Now.AddDays(4);
            List<OrdersTransfereeDetailsServiceDto> oTranDetailServices = new List<OrdersTransfereeDetailsServiceDto>();
            OrdersTransfereeDetailsServiceDto oTranDetailService = new OrdersTransfereeDetailsServiceDto() { Id = "#FF", ScheduledDate = changedDate };
            //oTranDetailService.Id += service.Id;
            oTranDetailServices.Add(oTranDetailService);
            OrdersTransfereeDetailsServicesDto svc = new OrdersTransfereeDetailsServicesDto();
            svc.Services = oTranDetailServices;
            svc.Id = order.Id;

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);

            // Assert
            var result = controller.UpsertDetailsServices(svc);
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [Test, Isolated]
        public async Task UpsertOrderDetails_OrderDoesNotExist_ShouldReturnNotFound()
        {
            // arrange
            ServiceType serviceType = Context.ServiceTypes.First();

            Service service = new Service()
            {
                ServiceType = serviceType,
                ServiceTypeId = serviceType.Id,
                ScheduledDate = DateTime.Now,
                CompletedDate = DateTime.Now,
            };

            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            order.Services.Add(service);
            order.DestinationCity = "integration city";
            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();

            //modify the service
            DateTime changedDate = DateTime.Now.AddDays(4);
            List<OrdersTransfereeDetailsServiceDto> oTranDetailServices = new List<OrdersTransfereeDetailsServiceDto>();
            OrdersTransfereeDetailsServiceDto oTranDetailService = new OrdersTransfereeDetailsServiceDto() { Id = service.Id, ScheduledDate = changedDate };
            oTranDetailServices.Add(oTranDetailService);
            OrdersTransfereeDetailsServicesDto svc = new OrdersTransfereeDetailsServicesDto();
            svc.Services = oTranDetailServices;
            svc.Id = order.Id + "FF";

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.UpsertDetailsServices(svc);

            // Assert
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [Test, Isolated]
        public async Task UpdateIntakeDestination_ValidOrder_ShouldChangeDestination()
        {
            // arrange

            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            order.DestinationCity = "integration city";
            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();

            //modify the order
            var dto = new OrdersTransfereeIntakeDestinationDto()
            {
                Id = order.Id,
                DestinationCountry = order.DestinationCountry + " 2",
                DestinationCity = order.DestinationCity + " 2",
                DestinationState = order.DestinationState + " 2"
            };

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.UpdateIntakeDestination(dto);

            // Assert
            Context.Entry(order).Reload();
            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
            order.DestinationCountry.Should().Be(dto.DestinationCountry);
            order.DestinationState.Should().Be(dto.DestinationState);
            order.DestinationCity.Should().Be(dto.DestinationCity);
        }

        [Test, Isolated]
        public async Task UpdateIntakeDestination_OrderDoesNotExist_ShouldReturnNotFound()
        {
            // arrange
            var dto = new OrdersTransfereeIntakeDestinationDto()
            {
                Id = "-1",
                DestinationCountry = "BadOrderCounty",
                DestinationCity = "BadOrderCity",
                DestinationState = "BadOrderState"
            };

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.UpdateIntakeDestination(dto);

            // Assert
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [Test, Isolated]
        public async Task UpdateIntakeOrigin_ValidOrder_ShouldChangeOrigin()
        {
            // arrange

            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            order.DestinationCity = "integration city";
            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();

            //modify the order
            var dto = new OrdersTransfereeIntakeOriginDto()
            {
                Id = order.Id,
                OriginCountry = order.OriginCountry + " 2",
                OriginCity = order.OriginCity + " 2",
                OriginState = order.OriginState + " 2"
            };

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.UpdateIntakeOrigin(dto);

            // Assert
            Context.Entry(order).Reload();
            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
            order.OriginCountry.Should().Be(dto.OriginCountry);
            order.OriginState.Should().Be(dto.OriginState);
            order.OriginCity.Should().Be(dto.OriginCity);
        }

        [Test, Isolated]
        public async Task UpdateIntakeOrigin_OrderDoesNotExist_ShouldReturnNotFound()
        {
            // arrange
            var dto = new OrdersTransfereeIntakeOriginDto()
            {
                Id = "-1",
                OriginCountry = "BadOrderCounty",
                OriginCity = "BadOrderCity",
                OriginState = "BadOrderState"
            };

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.UpdateIntakeOrigin(dto);

            // Assert
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [Test, Isolated]
        public async Task UpsertIntakeFamily_ValidOrder_ShouldChangeFamily()
        {
            // arrange

            var child = ChildBuilder.New();
            var pet = PetBuilder.New();

            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            order.DestinationCity = "integration city";
            order.Children.Add(child);
            order.Pets.Add(pet);
            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();

            //modify the order
            var dto = new OrdersTransfereeIntakeFamilyDto()
            {
                Id = order.Id,
                SpouseName = order.SpouseName + " 2",
                SpouseVisaType = order.SpouseVisaType + " 2",
                ChildrenEducationPreferences = order.ChildrenEducationPreferences + " 2",
                PetNotes = order.PetNotes + " 2",
                Children = new List<ChildDto>()
                {
                    new ChildDto()
                    {
                        Id = child.Id,
                        Name = child.Name + "2",
                        Age = child.Age + 2,
                        Grade = child.Grade + 2
                    }
                },
                Pets = new List<PetDto>()
                {
                    new PetDto
                    {
                        Id = pet.Id,
                        Breed = pet.Breed + " 2",
                        Type = pet.Type + " 2"
                    }
                }
            };

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.UpsertIntakeFamily(dto);

            // Assert
            Context.Entry(order).Reload();
            Context.Entry(child).Reload();
            Context.Entry(pet).Reload();

            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
            order.SpouseName.Should().Be(dto.SpouseName);
            order.SpouseVisaType.Should().Be(dto.SpouseVisaType);
            order.ChildrenEducationPreferences.Should().Be(dto.ChildrenEducationPreferences);
            order.PetNotes.Should().Be(dto.PetNotes);
            child.Age.Should().Be(dto.Children.FirstOrDefault().Age);
            child.Name.Should().Be(dto.Children.FirstOrDefault().Name);
            child.Grade.Should().Be(dto.Children.FirstOrDefault().Grade);
            pet.Breed.Should().Be(dto.Pets.FirstOrDefault().Breed);
            pet.Type.Should().Be(dto.Pets.FirstOrDefault().Type);
            pet.Size.Should().Be(dto.Pets.FirstOrDefault().Size);
        }

        [Test, Isolated]
        public async Task UpsertIntakeFamily_OrderDoesNotExist_ShouldReturnNotFound()
        {
            // arrange
            var dto = new OrdersTransfereeIntakeFamilyDto()
            {
                Id = "-1",
                SpouseName = "BadSpouseName"
            };

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.UpsertIntakeFamily(dto);

            // Assert
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [Test, Isolated]
        public async Task UpsertIntakeFamily_NewChildNewPet_ShouldAddBoth()
        {
            // arrange
            var child = ChildBuilder.New();
            var pet = PetBuilder.New();

            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            order.DestinationCity = "integration city";
            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();

            //modify the order
            var dto = new OrdersTransfereeIntakeFamilyDto()
            {
                Id = order.Id,
                SpouseName = order.SpouseName + " 2",
                SpouseVisaType = order.SpouseVisaType + " 2",
                ChildrenEducationPreferences = order.ChildrenEducationPreferences + " 2",
                PetNotes = order.PetNotes + " 2",
                Children = new List<ChildDto>()
                {
                    new ChildDto()
                    {
                        Id = child.Id,
                        Name = child.Name,
                        Age = child.Age,
                        Grade = child.Grade
                    }
                },
                Pets = new List<PetDto>()
                {
                    new PetDto
                    {
                        Id = pet.Id,
                        Breed = pet.Breed,
                        Type = pet.Type
                    }
                }
            };

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.UpsertIntakeFamily(dto);

            // Assert
            Context.Entry(order).Reload();
            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
            order.SpouseName.Should().Be(dto.SpouseName);
            order.SpouseVisaType.Should().Be(dto.SpouseVisaType);
            order.ChildrenEducationPreferences.Should().Be(dto.ChildrenEducationPreferences);
            order.PetNotes.Should().Be(dto.PetNotes);
            order.Children.Count.Should().Be(1);
            order.Children.FirstOrDefault().Age.Should().Be(dto.Children.FirstOrDefault().Age);
            order.Children.FirstOrDefault().Name.Should().Be(dto.Children.FirstOrDefault().Name);
            order.Children.FirstOrDefault().Grade.Should().Be(dto.Children.FirstOrDefault().Grade);
            order.Pets.Count.Should().Be(1);
            order.Pets.First().Breed.Should().Be(dto.Pets.FirstOrDefault().Breed);
            order.Pets.First().Type.Should().Be(dto.Pets.FirstOrDefault().Type);
            order.Pets.First().Size.Should().Be(dto.Pets.FirstOrDefault().Size);
        }

        [Test, Isolated]
        public async Task InsertChild_ValidOrder_ShouldAddChild()
        {
            // arrange
            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            order.DestinationCity = "integration city";

            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.InsertChild(order.Id);

            // Assert
            Context.Entry(order).Reload();

            result.Should().BeOfType<System.Web.Http.Results.OkNegotiatedContentResult<string>>();
            order.Children.Count.Should().Be(1);
            ((OkNegotiatedContentResult<string>)result).Content.Should().Be(order.Children.FirstOrDefault().Id);
        }

        [Test, Isolated]
        public async Task InsertChild_NoOrder_ShouldReturnNotFound()
        {
            // arrange


            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.InsertChild("-1");

            // Assert
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [Test, Isolated]
        public async Task DeleteChild_ValidChild_ShouldDeleteChild()
        {
            // arrange
            var child = ChildBuilder.New();

            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            order.DestinationCity = "integration city";
            order.Children.Add(child);
            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();


            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.DeleteChild(child.Id);

            // Assert
            Context.Entry(order).Reload();

            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
            order.Children.FirstOrDefault().Deleted.Should().BeTrue();
        }

        [Test, Isolated]
        public async Task DeleteChild_NoOrder_ShouldReturnNotFound()
        {
            // arrange

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.DeleteChild("-1");

            // Assert
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [Test, Isolated]
        public async Task InsertService_ValidOrder_ShouldAddService()
        {
            // arrange

            int serviceTypeId = Context.ServiceTypes.FirstOrDefault().Id;

            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            order.DestinationCity = "integration city";

            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.InsertService(order.Id, serviceTypeId);

            // Assert
            Context.Entry(order).Reload();

            result.Should().BeOfType<System.Web.Http.Results.OkNegotiatedContentResult<string>>();
            order.Services.Count.Should().Be(1);
            ((OkNegotiatedContentResult<string>)result).Content.Should().Be(order.Services.FirstOrDefault().Id);
        }

        [Test, Isolated]
        public async Task InsertService_NoOrder_ShouldReturnNotFound()
        {
            // arrange
            int serviceTypeId = Context.ServiceTypes.FirstOrDefault().Id;

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.InsertService("42ac62ab-0a44-4afe-b01a-4ebec93506e8RemoveMeToFailTest", serviceTypeId);

            // Assert
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [Test, Isolated]
        public async Task UpdateIntakeServices_ValidOrder_ShouldChangeServices()
        {
            // arrange
            int serviceTypeId = Context.ServiceTypes.FirstOrDefault().Id;
            Service service = new Odin.Data.Core.Models.Service() { ServiceTypeId = serviceTypeId, Notes = "Some Notes", Selected = true };

            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            order.DestinationCity = "integration city";
            order.Services.Add(service);
            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();

            //modify the order
            var dto = new OrdersTransfereeIntakeServicesDto()
            {
                Id = order.Id,
                Services = new List<OrdersTransfereeIntakeServiceDto>()
                {
                    new OrdersTransfereeIntakeServiceDto()
                    {
                        Id = service.Id,
                        Notes = service.Notes + " 2",
                        Selected = !service.Selected
                    }
                }
            };

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.UpdateIntakeServices(dto);

            // Assert
            Context.Entry(order).Reload();

            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
            service.Notes.Should().Be(dto.Services.FirstOrDefault().Notes);
            service.Selected.Should().Be(dto.Services.FirstOrDefault().Selected);
        }

        [Test, Isolated]
        public async Task UpdateIntakeServices_OrderDoesNotExist_ShouldReturnNotFound()
        {
            // arrange
            var dto = new OrdersTransfereeIntakeServicesDto()
            {
                Id = "-1",
            };

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUser(dsc.Id, dsc.UserName);
            var result = controller.UpdateIntakeServices(dto);

            // Assert
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [Test, Isolated]
        public async Task UpdateIntakeServices_InvalidService_ShouldReturnNotFound()
        {

            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            order.DestinationCity = "integration city";
            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();

            //modify the order

            //modify the order
            var dto = new OrdersTransfereeIntakeServicesDto()
            {
                Id = order.Id,
                Services = new List<OrdersTransfereeIntakeServiceDto>()
                {
                    new OrdersTransfereeIntakeServiceDto()
                    {
                        Id = "-1",
                        Notes = "Notes",
                        Selected = true
                    }
                }
            };

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUser(dsc.Id, dsc.UserName);
            var result = controller.UpdateIntakeServices(dto);

            // Assert
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [Test, Isolated]
        public async Task UpdateIntakeRmc_ValidOrder_ShouldChangeDestination()
        {
            // arrange

            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            order.DestinationCity = "integration city";
            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();

            //modify the order
            var dto = new OrdersTransfereeIntakeRmcDto()
            {
                Id = order.Id,
                Rmc = order.Rmc + " 2",
                RmcContact = order.RmcContact + " 2",
                RmcContactEmail = order.RmcContactEmail + " 2"
            };

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.UpdateIntakeRmc(dto);

            // Assert
            Context.Entry(order).Reload();
            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
            order.Rmc.Should().Be(dto.Rmc);
            order.RmcContact.Should().Be(dto.RmcContact);
            order.RmcContactEmail.Should().Be(dto.RmcContactEmail);
        }

        [Test, Isolated]
        public async Task UpdateIntakeRmc_OrderDoesNotExist_ShouldReturnNotFound()
        {
            // arrange
            var dto = new OrdersTransfereeIntakeRmcDto()
            {
                Id = "42ac62ab-0a44-4afe-b01a-4ebec93506e8RemoveMeToFail",
                Rmc = "BadRmc",
                RmcContact = "BadRmcContact",
                RmcContactEmail = "BadRmcContactEmail"
            };

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.UpdateIntakeRmc(dto);

            // Assert
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [Test, Isolated]
        public async Task InsertPet_ValidOrder_ShouldAddPet()
        {
            // arrange
            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            order.DestinationCity = "integration city";

            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.InsertPet(order.Id);

            // Assert
            Context.Entry(order).Reload();

            result.Should().BeOfType<System.Web.Http.Results.OkNegotiatedContentResult<string>>();
            order.Pets.Count.Should().Be(1);
            ((OkNegotiatedContentResult<string>)result).Content.Should().Be(order.Pets.FirstOrDefault().Id);
        }

        [Test, Isolated]
        public async Task InsertPet_NoOrder_ShouldReturnNotFound()
        {
            // arrange


            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.InsertPet("-1");

            // Assert
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [Test, Isolated]
        public async Task UpdateIntakeTempHousing_ValidOrder_ShouldChangeTempHousing()
        {
            // arrange

            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            order.DestinationCity = "integration city";
            order.TempHousingEndDate = DateTime.Now;
            order.TempHousingDays = 12;
            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();


            //modify the order
            var dto = new OrdersTransfereeIntakeTempHousingDto()
            {
                Id = order.Id,
                TempHousingEndDate = order.TempHousingEndDate.Value.AddDays(5),
                TempHousingDays = order.TempHousingDays + 1
            };

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.UpdateIntakeTempHousing(dto);

            // Assert
            Context.Entry(order).Reload();
            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
            order.TempHousingEndDate.ToString().Should().Be(dto.TempHousingEndDate.ToString());
            order.TempHousingDays.Should().Be(dto.TempHousingDays);
        }

        [Test, Isolated]
        public async Task UpdateIntakeTempHousing_OrderDoesNotExist_ShouldReturnNotFound()
        {
            // arrange
            var dto = new OrdersTransfereeIntakeTempHousingDto()
            {
                Id = "-1",
                TempHousingEndDate = DateTime.Now,
                TempHousingDays = 25
            };

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.UpdateIntakeTempHousing(dto);

            // Assert
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [Test, Isolated]
        public async Task UpdateIntakeLease_ValidOrder_ShouldChangeLease()
        {
            // arrange

            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            order.DestinationCity = "integration city";
            order.LeaseTerm = 12;
            order.DepositType = Context.DepositTypes.FirstOrDefault();
            order.BrokerFeeType = Context.BrokerFeeTypes.FirstOrDefault();
            order.LengthOfAssignment = 12;

            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();


            //modify the order
            var dto = new OrdersTransfereeIntakeLeaseDto()
            {
                Id = order.Id,
                LeaseTerm = order.LeaseTerm + 1,
                DepositTypeId = Context.DepositTypes.OrderByDescending(d => d.Id).FirstOrDefault().Id,
                LengthOfAssignment = order.LengthOfAssignment,
                BrokerFeeTypeId = Context.BrokerFeeTypes.OrderByDescending(d => d.Id).FirstOrDefault().Id
            };

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.UpdateIntakeLease(dto);

            // Assert
            Context.Entry(order).Reload();
            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
            order.LeaseTerm.Should().Be(dto.LeaseTerm);
            order.DepositTypeId.Should().Be(dto.DepositTypeId);
            order.LengthOfAssignment.Should().Be(dto.LengthOfAssignment);
            order.BrokerFeeTypeId.Should().Be(dto.DepositTypeId);
        }

        [Test, Isolated]
        public async Task UpdateIntakeLease_OrderDoesNotExist_ShouldReturnNotFound()
        {
            // arrange
            var dto = new OrdersTransfereeIntakeLeaseDto()
            {
                Id = "42ac62ab-0a44-4afe-b01a-4ebec93506e8RemoveMeToFail",
                LeaseTerm = 1,
                DepositTypeId = Context.DepositTypes.OrderByDescending(d => d.Id).FirstOrDefault().Id,
                LengthOfAssignment = 12,
                BrokerFeeTypeId = Context.BrokerFeeTypes.OrderByDescending(d => d.Id).FirstOrDefault().Id
            };

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.UpdateIntakeLease(dto);

            // Assert
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }
         
        [Test, Isolated]
        public async Task UpdateIntakeRelocation_ValidOrder_ShouldChangeRelocation()
        {
            // arrange

            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            order.DestinationCity = "integration city";
            order.LeaseTerm = 12;
            order.DepositType = Context.DepositTypes.FirstOrDefault();
            order.BrokerFeeType = Context.BrokerFeeTypes.FirstOrDefault();
            order.LengthOfAssignment = 12;

            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();


            //modify the order
            var dto = new OrdersTransfereeIntakeRelocationDto()
            {
                Id = order.Id,
                PreTripDate = order.PreTripDate.HasValue ? order.PreTripDate.Value.AddDays(5) : DateTime.Now,
                PreTripNotes = order.PreTripNotes + "1",
                EstimatedArrivalDate = order.EstimatedArrivalDate.HasValue ? order.EstimatedArrivalDate.Value.AddDays(5) : DateTime.Now,
                WorkStartDate = order.WorkStartDate.HasValue ? order.WorkStartDate.Value.AddDays(5) : DateTime.Now,
                EstimatedDepartureDate = order.EstimatedDepartureDate.HasValue ? order.EstimatedDepartureDate.Value.AddDays(5) : DateTime.Now
            };

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.UpdateIntakeRelocation(dto);

            // Assert
            Context.Entry(order).Reload();
            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
            order.PreTripDate.ToString().Should().Be(dto.PreTripDate.ToString());
            order.PreTripNotes.Should().Be(dto.PreTripNotes);
            order.EstimatedArrivalDate.ToString().Should().Be(dto.EstimatedArrivalDate.ToString());
            order.WorkStartDate.ToString().Should().Be(dto.WorkStartDate.ToString());
            order.EstimatedDepartureDate.ToString().Should().Be(dto.EstimatedDepartureDate.ToString());
        }

        [Test, Isolated]
        public async Task UpdateIntakeRelocation_OrderDoesNotExist_ShouldReturnNotFound()
        {
            // arrange
            var dto = new OrdersTransfereeIntakeRelocationDto()
            {
                Id = "42ac62ab-0a44-4afe-b01a-4ebec93506e8RemoveMeToFail",
                PreTripDate = DateTime.Now,
                PreTripNotes = "Notes",
                EstimatedArrivalDate = DateTime.Now,
                WorkStartDate = DateTime.Now,
                EstimatedDepartureDate = DateTime.Now
            };

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName,UserRoles.Consultant);
            var result = controller.UpdateIntakeRelocation(dto);

            // Assert
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [Test, Isolated]
        public async Task UpdateIntakeHomeFinding_ValidOrderNoHomeFinding_ShouldAddHomeFinding()
        {
            // arrange

            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            order.DestinationCity = "integration city";
            order.LeaseTerm = 12;
            order.DepositType = Context.DepositTypes.FirstOrDefault();
            order.BrokerFeeType = Context.BrokerFeeTypes.FirstOrDefault();
            order.LengthOfAssignment = 12;

            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();


            //modify the order
            var dto = new OrdersTransfereeIntakeHomeFindingDto()
            {
                Id = order.Id,
                NumberOfBedrooms = 5
            };

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.UpsertIntakeHomeFinding(dto);

            // Assert
            Context.Entry(order).Reload();
            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
            order.HomeFinding.Should().NotBeNull();
            order.HomeFinding.NumberOfBedrooms.Should().Be(dto.NumberOfBedrooms);
        }

        [Test, Isolated]
        public async Task UpdateIntakeHomeFinding_ValidOrderWithHomeFinding_ShouldUpdateHomeFinding()
        {
            // arrange

            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            order.DestinationCity = "integration city";

            order.HomeFinding = HomeFindingBuilder.New();
            order.HomeFinding.NumberOfBathrooms = Context.NumberOfBathrooms.FirstOrDefault();
            order.HomeFinding.HousingType = Context.HousingTypes.FirstOrDefault();
            order.HomeFinding.AreaType = Context.AreaTypes.FirstOrDefault();
            order.HomeFinding.TransportationType = Context.TransportationTypes.FirstOrDefault();


            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();


            //modify the order
            var dto = new OrdersTransfereeIntakeHomeFindingDto()
            {
                Id = order.Id,
                NumberOfBedrooms = order.HomeFinding.NumberOfBedrooms + 1,
                NumberOfBathroomsTypeId = Context.NumberOfBathrooms.OrderByDescending(n => n.Id).FirstOrDefault().Id,
                HousingBudget = order.HomeFinding.HousingBudget + 1000,
                SquareFootage = order.HomeFinding.SquareFootage + 100,
                MaxCommute = order.HomeFinding.MaxCommute + 10,
                Comments = order.HomeFinding.Comments + "1",
                NumberOfCarsOwned = order.HomeFinding.NumberOfCarsOwned + 2,
                IsFurnished = !order.HomeFinding.IsFurnished.Value,
                HasParking = !order.HomeFinding.HasParking,
                HasLaundry = !order.HomeFinding.HasLaundry,
                HasAC = !order.HomeFinding.HasAC,
                HasExerciseRoom = !order.HomeFinding.HasExerciseRoom,
                HousingTypeId = Context.HousingTypes.OrderByDescending(n => n.Id).FirstOrDefault().Id,
                AreaTypeId = Context.AreaTypes.OrderByDescending(n => n.Id).FirstOrDefault().Id,
                TransportationTypeId = Context.TransportationTypes.OrderByDescending(n => n.Id).FirstOrDefault().Id
            };

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.UpsertIntakeHomeFinding(dto);

            // Assert
            Context.Entry(order).Reload();
            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
            order.HomeFinding.Should().NotBeNull();
            order.HomeFinding.NumberOfBedrooms.Should().Be(dto.NumberOfBedrooms);
            order.HomeFinding.NumberOfBathroomsTypeId.Should().Be(dto.NumberOfBathroomsTypeId);
            order.HomeFinding.HousingBudget.Should().Be(dto.HousingBudget);
            order.HomeFinding.SquareFootage.Should().Be(dto.SquareFootage);
            order.HomeFinding.MaxCommute.Should().Be(dto.MaxCommute);
            order.HomeFinding.Comments.Should().Be(dto.Comments);
            order.HomeFinding.NumberOfCarsOwned.Should().Be(dto.NumberOfCarsOwned);
            order.HomeFinding.IsFurnished.Should().Be(dto.IsFurnished);
            order.HomeFinding.HasParking.Should().Be(dto.HasParking);
            order.HomeFinding.HasLaundry.Should().Be(dto.HasLaundry);
            order.HomeFinding.HasAC.Should().Be(dto.HasAC);
            order.HomeFinding.HasExerciseRoom.Should().Be(dto.HasExerciseRoom);
            order.HomeFinding.HousingTypeId.Should().Be(dto.HousingTypeId);
            order.HomeFinding.AreaTypeId.Should().Be(dto.AreaTypeId);
            order.HomeFinding.TransportationTypeId.Should().Be(dto.TransportationTypeId);
        }

        [Test, Isolated]
        public async Task UpdateIntakeHomeFinding_OrderDoesNotExist_ShouldReturnNotFound()
        {
            // arrange
            var dto = new OrdersTransfereeIntakeHomeFindingDto()
            {
                Id = "42ac62ab-0a44-4afe-b01a-4ebec93506e8/*RemoveMeToFail*/",
                NumberOfBedrooms = 1
            };

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName,UserRoles.Consultant);
            var result = controller.UpsertIntakeHomeFinding(dto);

            // Assert
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }

        [Test, CleanData]
        public async Task UpsertOrder_NewOrderServiceFlag_CreatesOrderWithServices()
        {
            // Arrange
            var orderDto = OrderDtoBuilder.New();
            orderDto.ProgramManager = ProgramManagerDtoBuilder.New();
            orderDto.ProgramManager.SeContactUid = pm.SeContactUid.Value;
            orderDto.Transferee = TransfereeDtoBuilder.New();
            orderDto.Transferee.Email = transferee.Email;
            orderDto.Consultant = ConsultantDtoBuilder.New();
            orderDto.Consultant.SeContactUid = dsc.SeContactUid.Value;
            orderDto.ServiceFlag = 3;
            orderDto.IsInternational = false;
            Context.Orders.SingleOrDefault(o => o.TrackingId.Equals(orderDto.TrackingId)).Should().BeNull();

            // Act
            var request = CreateRequest("api/orders", "application/json", HttpMethod.Post, orderDto);
            request.Headers.Add("Token", ApiKey);
            var response = await Server.HttpClient.SendAsync(request);
            var errorResponse = await response.ReadContentAsyncSafe<ErrorResponse>();

            // Assert
            errorResponse?.errors.Should().BeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var order = Context.Orders.Where(o => o.TrackingId.Equals(orderDto.TrackingId))
                .Include(o => o.Services).
                Include(o => o.Services.Select(s => s.ServiceType))
                .SingleOrDefault();

            order.Should().NotBeNull();
            order.Services.Count.Should().Be(2);
            order.Services.SingleOrDefault(st => st.ServiceType.Category == ServiceCategory.InitialConsultation)
                .Should().NotBeNull();
            order.Services.SingleOrDefault(st => st.ServiceType.Category == ServiceCategory.WelcomePacket)
                .Should().NotBeNull();
        }

        [Test, CleanData]
        public async Task UpsertOrder_ValidUpdateWithServiceFlag_ShouldAddServices()
        {
            // Arrange
            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            Context.Orders.Add(order);
            Context.SaveChanges();
            var orderDto = OrderDtoBuilder.New();
            orderDto.TrackingId = order.TrackingId;
            orderDto.ProgramManager = ProgramManagerDtoBuilder.New();
            orderDto.ProgramManager.SeContactUid = pm.SeContactUid.Value;
            orderDto.Consultant = ConsultantDtoBuilder.New();
            orderDto.Consultant.SeContactUid = dsc.SeContactUid.Value;
            orderDto.Transferee = TransfereeDtoBuilder.New();
            orderDto.Transferee.Email = transferee.Email;
            orderDto.ServiceFlag = 3;
            orderDto.IsInternational = false;

            // Act
            var request = CreateRequest("api/orders", "application/json", HttpMethod.Post, orderDto);
            request.Headers.Add("Token", ApiKey);
            var response = await Server.HttpClient.SendAsync(request);
            var errorResponse = await response.ReadContentAsyncSafe<ErrorResponse>();

            // Assert
            errorResponse?.errors.Should().BeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            Context.Entry(order).Reload();
            Context.Entry(order).Collection(o => o.Services).Load();
            foreach (var service in order.Services)
            {
                Context.Entry(service).Reference(s => s.ServiceType).Load();
            }

            order.Services.Count.Should().Be(2);
            order.Services.SingleOrDefault(st => st.ServiceType.Category == ServiceCategory.InitialConsultation)
                .Should().NotBeNull();
            order.Services.SingleOrDefault(st => st.ServiceType.Category == ServiceCategory.WelcomePacket)
                .Should().NotBeNull();
        }

        [Test, CleanData]
        public async Task UpsertOrder_ValidUpdateServicesAlreadyExist_ShouldNotAddServices()
        {
            var wel = new Service()
            {
                ServiceType = Context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Initial/Pre-Arrival Consultation".ToUpper()),
                Selected = true
            };

            var initCons = new Service()
            {
                ServiceType = Context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Welcome Packet".ToUpper()),
                Selected = true
            };

            // Arrange
            var order = OrderBuilder.New().First();
            order.Services.Add(wel);
            order.Services.Add(initCons);
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            Context.Orders.Add(order);
            Context.SaveChanges();
            var orderDto = OrderDtoBuilder.New();
            orderDto.TrackingId = order.TrackingId;
            orderDto.ProgramManager = ProgramManagerDtoBuilder.New();
            orderDto.ProgramManager.SeContactUid = pm.SeContactUid.Value;
            orderDto.Consultant = ConsultantDtoBuilder.New();
            orderDto.Consultant.SeContactUid = dsc.SeContactUid.Value;
            orderDto.Transferee = TransfereeDtoBuilder.New();
            orderDto.Transferee.Email = transferee.Email;
            orderDto.ServiceFlag = 3;
            orderDto.IsInternational = false;

            // Act
            var request = CreateRequest("api/orders", "application/json", HttpMethod.Post, orderDto);
            request.Headers.Add("Token", ApiKey);
            var response = await Server.HttpClient.SendAsync(request);
            var errorResponse = await response.ReadContentAsyncSafe<ErrorResponse>();

            // Assert
            errorResponse?.errors.Should().BeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            Context.Entry(order).Reload();
            Context.Entry(order).Collection(o => o.Services).Load();
            foreach (var service in order.Services)
            {
                Context.Entry(service).Reference(s => s.ServiceType).Load();
            }

            order.Services.Count.Should().Be(2);
            order.Services.SingleOrDefault(st => st.ServiceType.Category == ServiceCategory.InitialConsultation)
                .Should().NotBeNull();
            order.Services.SingleOrDefault(st => st.ServiceType.Category == ServiceCategory.WelcomePacket)
                .Should().NotBeNull();
        }

        [Test, CleanData]
        public async Task UpsertOrder_ValidUpdateNewServiceFlagWithServicesThatAlreadyExist_ShouldAddServices()
        {
            var wel = new Service()
            {
                ServiceType = Context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Initial/Pre-Arrival Consultation".ToUpper()),
                Selected = true
            };

            var initCons = new Service()
            {
                ServiceType = Context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Welcome Packet".ToUpper()),
                Selected = true
            };

            // Arrange
            var order = OrderBuilder.New().First();
            order.Services.Add(wel);
            order.Services.Add(initCons);
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            Context.Orders.Add(order);
            Context.SaveChanges();
            var orderDto = OrderDtoBuilder.New();
            orderDto.TrackingId = order.TrackingId;
            orderDto.ProgramManager = ProgramManagerDtoBuilder.New();
            orderDto.ProgramManager.SeContactUid = pm.SeContactUid.Value;
            orderDto.Consultant = ConsultantDtoBuilder.New();
            orderDto.Consultant.SeContactUid = dsc.SeContactUid.Value;
            orderDto.Transferee = TransfereeDtoBuilder.New();
            orderDto.Transferee.Email = transferee.Email;
            orderDto.ServiceFlag = 7;
            orderDto.IsInternational = false;

            // Act
            var request = CreateRequest("api/orders", "application/json", HttpMethod.Post, orderDto);
            request.Headers.Add("Token", ApiKey);
            var response = await Server.HttpClient.SendAsync(request);
            var errorResponse = await response.ReadContentAsyncSafe<ErrorResponse>();

            // Assert
            errorResponse?.errors.Should().BeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            Context.Entry(order).Reload();
            Context.Entry(order).Collection(o => o.Services).Load();

            order.Services.Count.Should().BeGreaterThan(2);
        }

        /// <summary>
        /// Check again Not correct 
        /// </summary>
        /// <returns></returns>
        [Test, Isolated]
        public async Task UpdateHomeFindingProperties_ModifyOrder_ShouldChangePropertyValues()
        {
            // arrange
            var order = OrderBuilder.New().First();
            order.TrackingId = TokenHelper.NewToken();
            order.Transferee = transferee;
            order.Consultant = dsc;
            order.ProgramManager = pm;
            Context.Orders.Add(order);
            Context.SaveChanges();
            var orderDto = OrderDtoBuilder.New();
            orderDto.TrackingId = order.TrackingId;
            orderDto.ProgramManager = ProgramManagerDtoBuilder.New();
            orderDto.ProgramManager.SeContactUid = pm.SeContactUid.Value;
            orderDto.Consultant = ConsultantDtoBuilder.New();
            orderDto.Consultant.SeContactUid = dsc.SeContactUid.Value;
            orderDto.Transferee = TransfereeDtoBuilder.New();
            orderDto.Transferee.Email = transferee.Email;
            orderDto.ServiceFlag = 7;
            orderDto.IsInternational = false;

            HomeFinding h1 = new HomeFinding() { Id = "1" };

            HomeFindingProperty p1 = new HomeFindingProperty() { Id = "1" };
            p1.Deleted = false;
            p1.Property = new Property() { Id = "1", Amount = 10, NumberOfBathrooms = 5, NumberOfBedrooms = 2, SquareFootage = 2343 };
            p1.ViewingDate = DateTime.Now.AddDays(10);
            h1.HomeFindingProperties.Add(p1);
            order.HomeFinding = h1;
            Context.SaveChanges();
            Context.Entry(h1).Reload();

            //modify the order
            HousingPropertyViewModel propertyVM = new HousingPropertyViewModel() { Id = "1", PropertyNumberOfBathrooms = 10, PropertyNumberOfBedrooms = 12, PropertyAmount = 2002, PropertySquareFootage = 1023 };

            // Act
            var controller = SetUpOrdersController();
            controller.MockCurrentUserAndRole(dsc.Id, dsc.UserName, UserRoles.Consultant);
            var result = controller.UpdateHousingProperty(propertyVM);

            // Assert
            //Context.Entry(p1).Reload();
            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
            p1.Property.NumberOfBedrooms.Equals(propertyVM.PropertyNumberOfBedrooms);
            p1.Property.NumberOfBathrooms.Equals(propertyVM.PropertyNumberOfBathrooms);
            p1.Property.Amount.Should().Equals(propertyVM.PropertyAmount);
            p1.Property.SquareFootage.Equals(propertyVM.PropertySquareFootage);
        }

    }
}
