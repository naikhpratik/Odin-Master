using AutoMapper;
using Microsoft.AspNet.Identity;
using Odin.Data.Core;
using Odin.Data.Core.Models;
using Odin.Extensions;
using Odin.Filters;
using Odin.Interfaces;
using Odin.ViewModels.BookMarklet;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Odin.Controllers
{

    [AuthorizeBookMarklet]
    public class BookMarkletController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IQueueStore _queueStore;
        private readonly IBookMarkletHelper _bookMarkletHelper;

        public BookMarkletController(IUnitOfWork unitOfWork, IMapper mapper, IQueueStore queueStore, IBookMarkletHelper bookMarkletHelper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _queueStore = queueStore;
            _bookMarkletHelper = bookMarkletHelper;
        }

        [HttpGet]
        public ActionResult Index(string url)
        {
            var userId = User.Identity.GetUserId();
            IEnumerable<Order> orders = _unitOfWork.Orders.GetOrdersFor(userId,User.GetUserRole()).Where(o => o.HasHomeFinding);

            if (orders.Count() == 0)
            {
                BookMarkletErrorViewModel error = new BookMarkletErrorViewModel();
                error.Header = "Sorry!";
                error.Message = "It looks like you don't have any homefinding orders!";
                return View("Error", error);
            }

            if (_bookMarkletHelper.IsValidUrl(url))
            {
                BookMarkletViewModel bm = new BookMarkletViewModel();
                bm.Orders = _mapper.Map<IEnumerable<Order>, IEnumerable<BookMarkletOrderViewModel>>(orders);
                bm.PropertyUrl = url;
                return View(bm);
            }
            else
            {
                BookMarkletErrorViewModel error = new BookMarkletErrorViewModel();
                error.Header = "Sorry!";
                error.Message = "This page is not currently supported! Try another homefinding site for this property.";
                return View("Error",error);
            }
            
        }
    }
}