﻿using AutoMapper;
using Microsoft.AspNet.Identity;
using Odin.Data.Core;
using Odin.Data.Core.Models;
using Odin.Interfaces;
using Odin.ViewModels.Orders.Transferee;
using Odin.ViewModels.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace Odin.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public OrdersController(IUnitOfWork unitOfWork, IMapper mapper,IAccountHelper accountHelper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;           
        }

        // GET: Orders
        public ViewResult Index()
        {
            var userId = User.Identity.GetUserId();

            var orders = _unitOfWork.Orders.GetOrdersFor(userId);

            //var orderVms = _mapper.Map<IEnumerable<Order>, IEnumerable<OrdersIndexViewModel>>(orders);

            return View();
        }

        public ActionResult HousingPartial(string id)
        {
            var order = _unitOfWork.Orders.GetOrderById(id);

            HousingViewModel viewModel = _mapper.Map<HomeFinding, HousingViewModel>(order.HomeFinding);
            viewModel.NumberOfPets = order.Pets.Count();
            int numKids = order.Children == null ? 0 : order.Children.Count();
            if (numKids == 0 && order.SpouseName == "")
                viewModel.SpouceAndKids = null;
            else
                viewModel.SpouceAndKids = (order.SpouseName == "" ? "No" : "Yes") + " / " + numKids.ToString();
            return PartialView("~/views/orders/partials/_Housing.cshtml", viewModel);
        }

        // GET Partials
        public ActionResult DetailsPartial(string id)
        {
            OrdersTransfereeViewModel viewModel = GetViewModelForOrder(id);
            return PartialView("~/views/orders/partials/_Details.cshtml",viewModel); 
        }

        public ActionResult IntakePartial(string id)
        {
            OrdersTransfereeViewModel viewModel = GetViewModelForOrder(id);
            return PartialView("~/views/orders/partials/_Intake.cshtml", viewModel);
        }

        public ActionResult Details(string orderId)
        {
            var userId = User.Identity.GetUserId();

            var order = _unitOfWork.Orders.GetOrderById(orderId);

            if (order == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound, "Not found");
            }

            if (order.ConsultantId != userId)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized, "Unauthorized Order");
            }

            return View();            
        }

        // GET: Transferee
        public ActionResult Transferee(string id)
        {
            var userId = User.Identity.GetUserId();
            var order = _unitOfWork.Orders.GetOrderFor(userId, id);

            if (order == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound, "Not found");
            }
            ViewBag.Id = id;
            return View();
        }

        private OrdersTransfereeViewModel GetViewModelForOrder(string id)
        {
            var userId = User.Identity.GetUserId();
            var order = _unitOfWork.Orders.GetOrderFor(userId,id);

            OrdersTransfereeViewModel vm = _mapper.Map<Order, OrdersTransfereeViewModel>(order);
            vm.Services = vm.Services.OrderBy(s => s.Category);

            //Get list of all service categories
            var catEnums = Enum.GetValues(typeof(ServiceCategory)).Cast<ServiceCategory>();

            //Populate list of service categories available for this order.
            var cats = new List<ServiceCategory>();
            foreach (var cat in catEnums)
            {
                //Use service bit flag on order to determine what categories have been selected in SE.
                if ((order.ServiceFlag & (int)cat) > 0)
                {
                    cats.Add(cat);
                }
            }

            //Get all service types that the order already has.
            var ids = order.Services.Select(s => s.ServiceType.Id).ToList();

            //Remove service types that already have services.
            var filtPossible = _unitOfWork.ServiceTypes.GetPossibleServiceTypes(cats, ids);

            vm.PossibleServices =
                _mapper.Map<IEnumerable<ServiceType>, IEnumerable<ServiceTypeViewModel>>(filtPossible).OrderBy(s => s.Category);

            vm.NumberOfBathrooms = _unitOfWork.NumberOfBathrooms.GetNumberOfBathroomsList();
            vm.HousingTypes = _unitOfWork.HousingTypes.GetHousingTypesList();
            vm.AreaTypes = _unitOfWork.AreaTypes.GetAreaTypesList();
            vm.TransportationTypes = _unitOfWork.TransportationTypes.GetTransportationTypes();
            vm.DepositTypes = _unitOfWork.DepositTypes.GetDepositTypesList();
            vm.BrokerFeeTypes = _unitOfWork.BrokerFeeTypes.GetBrokerFeeTypes();

            return vm;
        }
    }
    
}