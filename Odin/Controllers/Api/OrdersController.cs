using AutoMapper;
using Microsoft.ApplicationInsights;
using Microsoft.AspNet.Identity;
using Odin.Data.Core;
using Odin.Data.Core.Dtos;
using Odin.Data.Core.Models;
using Odin.Domain;
using Odin.Extensions;
using Odin.Filters;
using Odin.Interfaces;
using Odin.ViewModels.Orders.Transferee;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace Odin.Controllers.Api
{
    public class OrdersController : ApiController
    {
        private readonly IOrderImporter _orderImporter;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IQueueStore _queueStore;
        private readonly IAccountHelper _accountHelper;

        public OrdersController(IUnitOfWork unitOfWork, IMapper mapper, IQueueStore queueStore, IAccountHelper accountHelper)
        {
            _orderImporter = new OrderImporter(unitOfWork, mapper);
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _queueStore = queueStore;
            _accountHelper = accountHelper;
        }

        // POST /api/orders
        [HttpPost]
        [ApiAuthorize]
        [ValidationActionFilter]
        public IHttpActionResult UpsertOrder(OrderDto orderDto)
        {
            try
            {
                _orderImporter.ImportOrder(orderDto);
            }
            catch (Exception e)
            {
                var ai = new TelemetryClient();
                ai.TrackException(e);
                return InternalServerError(e);
            }
            

            return Ok();
        }

        // GET /api/orders
        [HttpGet]
        [Authorize]
        public IHttpActionResult GetOrders()
        {
            var userId = User.Identity.GetUserId();
            var orders = _unitOfWork.Orders.GetOrdersFor(userId,User.GetUserRole());

            var transfereeIndexDtos = _mapper.Map<IEnumerable<Order>, IEnumerable<TransfereeIndexDto>>(orders);

            return Ok(new OrderIndexDto {Transferees = transfereeIndexDtos});
        }

        [HttpPost]
        [Authorize]
        [Route("api/orders/transferee/invite")]
        public async Task<IHttpActionResult> InviteTransferee(InviteTransfereeDto dto)
        {
            var userId = User.Identity.GetUserId();

            var order = _unitOfWork.Orders.GetOrderFor(userId, dto.OrderId, User.GetUserRole());

            if (order == null)
                return NotFound();

            var response = await _accountHelper.SendEmailCreateTokenAsync(order.TransfereeId);

            if (response == "Message not sent")
                return BadRequest();

            order.Transferee.InviteStatus = InviteStatus.Invited;
            _unitOfWork.Complete();

            return Ok();
        }

        [HttpPost]
        [Authorize]
        [RoleAuthorize(UserRoles.ProgramManager,UserRoles.Consultant)]
        [Route("api/orders/transferee/intake/destination")]
        public IHttpActionResult UpdateIntakeDestination(OrdersTransfereeIntakeDestinationDto dto)
        {

            var userId = User.Identity.GetUserId();
            Order order = _unitOfWork.Orders.GetOrderFor(userId, dto.Id, User.GetUserRole());

            if (order == null)
            {
                return NotFound();
            }

            _mapper.Map<OrdersTransfereeIntakeDestinationDto, Order>(dto, order);
            _unitOfWork.Complete();
            
            return Ok();
        }

        [HttpPost]
        [RoleAuthorize(UserRoles.ProgramManager, UserRoles.Consultant)]
        [Route("api/orders/transferee/intake/origin")]
        public IHttpActionResult UpdateIntakeOrigin(OrdersTransfereeIntakeOriginDto dto)
        {

            var userId = User.Identity.GetUserId();
            Order order = _unitOfWork.Orders.GetOrderFor(userId, dto.Id, User.GetUserRole());

            if (order == null)
            {
                return NotFound();
            }

            _mapper.Map<OrdersTransfereeIntakeOriginDto, Order>(dto, order);
            _unitOfWork.Complete();

            return Ok();
        }

        [HttpPost]
        [RoleAuthorize(UserRoles.ProgramManager, UserRoles.Consultant)]
        [Route("api/orders/transferee/intake/family")]
        public IHttpActionResult UpsertIntakeFamily(OrdersTransfereeIntakeFamilyDto dto)
        {
            var userId = User.Identity.GetUserId();
            Order order = _unitOfWork.Orders.GetOrderFor(userId, dto.Id, User.GetUserRole());

            if (order == null)
            {
                return NotFound();
            }

            foreach (var childDto in dto.Children)
            {
                var child = order.Children.FirstOrDefault(c => !String.IsNullOrEmpty(childDto.Id) && c.Id == childDto.Id);
                if (child == null)
                {
                    child = _mapper.Map<ChildDto, Child>(childDto);
                    child.Id = Guid.NewGuid().ToString();
                    order.Children.Add(child);
                }
                else
                {
                    _mapper.Map<ChildDto, Child>(childDto, child);
                }
            }

            foreach (var petDto in dto.Pets)
            {
                var pet = order.Pets.FirstOrDefault(p => !String.IsNullOrEmpty(petDto.Id) && p.Id == petDto.Id);
                if (pet == null)
                {
                    pet = _mapper.Map<PetDto, Pet>(petDto);
                    pet.Id = Guid.NewGuid().ToString();
                    order.Pets.Add(pet);
                }
                else
                {
                    _mapper.Map<PetDto, Pet>(petDto, pet);
                }
            }

            _mapper.Map<OrdersTransfereeIntakeFamilyDto, Order>(dto, order);
            _unitOfWork.Complete();

            //On success, return the newly created id's with the associated temp id's so that the client can be updated.
            return Ok();
        }

        [HttpPost]
        [RoleAuthorize(UserRoles.ProgramManager, UserRoles.Consultant)]
        [Route("api/orders/transferee/children/{orderId}")]
        public IHttpActionResult InsertChild(string orderId)
        {
            var userId = User.Identity.GetUserId();
            Order order = _unitOfWork.Orders.GetOrderFor(userId, orderId, User.GetUserRole());

            if (order == null)
            {
                return NotFound();
            }

            var child = new Child { Id = Guid.NewGuid().ToString() };
            order.Children.Add(child);
            _unitOfWork.Complete();

            //On success, return the newly created id.
            return Ok(child.Id);
        }

        [HttpDelete]
        [RoleAuthorize(UserRoles.ProgramManager, UserRoles.Consultant)]
        [Route("api/orders/transferee/children/{id}")]
        public IHttpActionResult DeleteChild(string id)
        {
            var userId = User.Identity.GetUserId();
            var child = _unitOfWork.Children.GetChildFor(userId,id,User.GetUserRole());

            if (child == null)
            {
                return NotFound();
            }

            child.Delete();
            _unitOfWork.Complete();
            return Ok();
        }

        [HttpPost]
        [RoleAuthorize(UserRoles.ProgramManager, UserRoles.Consultant)]
        [Route("api/orders/transferee/services/{orderId}/{id}")]
        public IHttpActionResult InsertService(string orderId, int id)
        {

            var userId = User.Identity.GetUserId();
            Order order = _unitOfWork.Orders.GetOrderFor(userId, orderId, User.GetUserRole());

            if (order == null)
            {
                return NotFound();
            }

            var newService = new Service(){ServiceTypeId = id, Selected = true};

            order.Services.Add(newService);

            _unitOfWork.Complete();
            return Ok(newService.Id);
        }

        [HttpPost]
        [RoleAuthorize(UserRoles.ProgramManager, UserRoles.Consultant)]
        [Route("api/orders/transferee/intake/services")]
        public IHttpActionResult UpdateIntakeServices(OrdersTransfereeIntakeServicesDto dto)
        {

            var userId = User.Identity.GetUserId();
            Order order = _unitOfWork.Orders.GetOrderFor(userId, dto.Id, User.GetUserRole());

            if (order == null)
            {
                return NotFound();
            }

            foreach (var serviceDto in dto.Services)
            {
                var service = order.Services.FirstOrDefault(s => !String.IsNullOrEmpty(serviceDto.Id) && s.Id == serviceDto.Id);
                if (service == null)
                {
                    return NotFound();
                }
                else
                {
                    _mapper.Map<OrdersTransfereeIntakeServiceDto, Service>(serviceDto, service);
                }
            }

            _unitOfWork.Complete();
            return Ok();
        }

        [HttpPost]
        [RoleAuthorize(UserRoles.ProgramManager, UserRoles.Consultant)]
        [Route("api/orders/transferee/details/services")]
        public IHttpActionResult UpsertDetailsServices(OrdersTransfereeDetailsServicesDto dto)
        {
            var userId = User.Identity.GetUserId();
            Order order = _unitOfWork.Orders.GetOrderFor(userId, dto.Id, User.GetUserRole());

            if (order == null)
            {
                return NotFound();
            }
            if (dto.Services != null)
               {
                foreach (var serviceDto in dto.Services)
                {
                    var service = order.Services.FirstOrDefault(s => !String.IsNullOrEmpty(serviceDto.Id) && s.Id == serviceDto.Id);
                    if (service == null)
                    {
                        return NotFound();
                    }
                    else
                    {
                        var originalServiceDate = service.CompletedDate;
                        _mapper.Map<OrdersTransfereeDetailsServiceDto, Service>(serviceDto, service);
                        if (service.CompletedDate != originalServiceDate)
                        {
                            _queueStore.Add(new OdinToSeQueueEntry(service.Id, QueueType.Service));
                        }
                    }
                }
            }
            _unitOfWork.Complete();
            return Ok();
        }

        [HttpPost]
        [RoleAuthorize(UserRoles.ProgramManager, UserRoles.Consultant)]
        [Route("api/orders/transferee/intake/rmc")]
        public IHttpActionResult UpdateIntakeRmc(OrdersTransfereeIntakeRmcDto dto)
        {

            var userId = User.Identity.GetUserId();
            Order order = _unitOfWork.Orders.GetOrderFor(userId, dto.Id, User.GetUserRole());

            if (order == null)
            {
                return NotFound();
            }

            _mapper.Map<OrdersTransfereeIntakeRmcDto, Order>(dto, order);
            _unitOfWork.Complete();

            return Ok();
        }

        [HttpPost]
        [RoleAuthorize(UserRoles.ProgramManager, UserRoles.Consultant)]
        [Route("api/orders/transferee/pets/{orderId}")]
        public IHttpActionResult InsertPet(string orderId)
        {
            var userId = User.Identity.GetUserId();
            Order order = _unitOfWork.Orders.GetOrderFor(userId, orderId, User.GetUserRole());

            if (order == null)
            {
                return NotFound();
            }

            var pet = new Pet { Id = Guid.NewGuid().ToString() };
            order.Pets.Add(pet);
            _unitOfWork.Complete();

            //On success, return the newly created id.
            return Ok(pet.Id);
        }

        [HttpDelete]
        [RoleAuthorize(UserRoles.ProgramManager, UserRoles.Consultant)]
        [Route("api/orders/transferee/pets/{id}")]
        public IHttpActionResult DeletePet(string id)
        {
            var userId = User.Identity.GetUserId();
            var pet = _unitOfWork.Pets.GetPetFor(userId, id, User.GetUserRole());

            if (pet == null)
            {
                return NotFound();
            }
            pet.Delete();
            _unitOfWork.Complete();
            return Ok();
        }

        [HttpPost]
        [RoleAuthorize(UserRoles.ProgramManager, UserRoles.Consultant)]
        [Route("api/orders/transferee/intake/temphousing")]
        public IHttpActionResult UpdateIntakeTempHousing(OrdersTransfereeIntakeTempHousingDto dto)
        {
            var userId = User.Identity.GetUserId();
            Order order = _unitOfWork.Orders.GetOrderFor(userId, dto.Id, User.GetUserRole());

            if (order == null)
            {
                return NotFound();
            }

            _mapper.Map<OrdersTransfereeIntakeTempHousingDto, Order>(dto, order);
            _unitOfWork.Complete();

            return Ok();
        }

        [HttpPost]
        [RoleAuthorize(UserRoles.ProgramManager, UserRoles.Consultant)]
        [Route("api/orders/transferee/intake/homefinding")]
        public IHttpActionResult UpsertIntakeHomeFinding(OrdersTransfereeIntakeHomeFindingDto dto)
        {
            var userId = User.Identity.GetUserId();
            Order order = _unitOfWork.Orders.GetOrderFor(userId, dto.Id, User.GetUserRole());

            if (order == null)
            {
                return NotFound();
            }

            if (order.HomeFinding == null)
            {
                order.HomeFinding = _mapper.Map<OrdersTransfereeIntakeHomeFindingDto, HomeFinding>(dto);
            }
            else
            {
                _mapper.Map<OrdersTransfereeIntakeHomeFindingDto, HomeFinding>(dto, order.HomeFinding);
            }

            _unitOfWork.Complete();

            return Ok();
        }

        [HttpPost]
        [RoleAuthorize(UserRoles.ProgramManager, UserRoles.Consultant)]
        [Route("api/orders/transferee/intake/lease")]
        public IHttpActionResult UpdateIntakeLease(OrdersTransfereeIntakeLeaseDto dto)
        {
            var userId = User.Identity.GetUserId();
            Order order = _unitOfWork.Orders.GetOrderFor(userId, dto.Id, User.GetUserRole());

            if (order == null)
            {
                return NotFound();
            }

            _mapper.Map<OrdersTransfereeIntakeLeaseDto, Order>(dto, order);
            _unitOfWork.Complete();

            return Ok();
        }

        [HttpPost]
        [RoleAuthorize(UserRoles.ProgramManager, UserRoles.Consultant)]
        [Route("api/orders/transferee/intake/relocation")]
        public IHttpActionResult UpdateIntakeRelocation(OrdersTransfereeIntakeRelocationDto dto)
        {

            var userId = User.Identity.GetUserId();
            Order order = _unitOfWork.Orders.GetOrderFor(userId, dto.Id, User.GetUserRole());

            if (order == null)
            {
                return NotFound();
            }

            _mapper.Map<OrdersTransfereeIntakeRelocationDto, Order>(dto, order);
            _unitOfWork.Complete();

            return Ok();
        }

        [HttpPost]
        [RoleAuthorize(UserRoles.ProgramManager, UserRoles.Consultant)]
        [Route("api/orders/transferee/housing/update")]
        public IHttpActionResult UpdateHousingProperty(HousingPropertyViewModel propertyVM)
        {
            HomeFindingProperty homeFindingProperty;
            homeFindingProperty = _unitOfWork.HomeFindingProperties.GetHomeFindingPropertyById(propertyVM.Id);

            var prop = homeFindingProperty.Property;
 
            prop.Amount = propertyVM.PropertyAmount;
            prop.NumberOfBathrooms = propertyVM.PropertyNumberOfBathrooms;
            prop.NumberOfBedrooms = propertyVM.PropertyNumberOfBedrooms;
            prop.SquareFootage = propertyVM.PropertySquareFootage;
           
            _unitOfWork.Complete();

            return Ok();
        }
    }
}
