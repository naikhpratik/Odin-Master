using AutoMapper;
using Microsoft.AspNet.Identity;
using Odin.Data.Core;
using Odin.Data.Core.Models;
using Odin.Extensions;
using Odin.Helpers;
using Odin.ViewModels.Mailers;
using Odin.ViewModels.Orders.Transferee;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web.Mvc;

namespace Odin.Controllers
{
    [Authorize]
    public class EmailController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        
        public EmailController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpGet]
        public ActionResult EmailForm(string id)
        {
            Transferee ee = GetTransfereeByOrderId(id);
            if (ee == null)
                return new HttpStatusCodeResult(HttpStatusCode.NotFound, "Not found");

            var viewModel = new EmailViewModel();
            viewModel.id = id;
            viewModel.Email = ee.Email;
            viewModel.Name = ee.FullName;
            viewModel.Subject = "Your Dwellworks Itinerary";
            viewModel.Message = "Please find attached your itinerary for the upcoming move";
            return PartialView("~/views/Mailers/Partials/Email.cshtml", viewModel);
        }

        [HttpPost]
        public ActionResult SendEmail(EmailViewModel vm)
        {
            try
            {
                var to = ParseAddress(vm.Email);
                if (to == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing 'to' field!");

                var userId = User.Identity.GetUserId();
                var order = _unitOfWork.Orders.GetOrderFor(userId, vm.id, User.GetUserRole());

                if (order == null || order.Transferee == null) 
                {
                    return new HttpStatusCodeResult(HttpStatusCode.NotFound, "Not found");
                }

                OrdersTransfereeItineraryViewModel viewModel = BuildItineraryFromOrder(order);
                viewModel.Id = vm.id;
                viewModel.IsPdf = true;
                
                viewModel.TransfereeName = order.Transferee.FullName;
                string filename = "Itinerary" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".pdf";
                var pdf = new Rotativa.ViewAsPdf("~/Views/PDF/PDFItinerary.cshtml", viewModel);
                byte[] pdfBytes = pdf.BuildFile(ControllerContext);
                MemoryStream stream = new MemoryStream(pdfBytes);                
                EmailHelper EH = new EmailHelper();                    
                EH.SendEmail_FS(to, vm.Subject, vm.Message, SendGrid.MimeType.Html, filename, pdfBytes);
                viewModel.IsPdf = false;
                return PartialView("~/views/PDF/PDFItinerary.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ModelState.Clear();
                return null;
            }
        }
        private Transferee GetTransfereeByOrderId(string id)
        {
            return _unitOfWork.Transferees.GetTransfereeByOrderId(id);
        }

        private OrdersTransfereeItineraryViewModel BuildItineraryFromOrder(Order order)
        {
            ItineraryHelper itinHelper = new ItineraryHelper(_unitOfWork, _mapper);
            return itinHelper.Build(order);            
        }

        private static IEnumerable<string> ParseAddress(string addresses)
        {
            if (string.IsNullOrEmpty(addresses) == true)
                return null;
            addresses = addresses.Replace(",", ";").Replace(" ", ";");
            Char delim = ';';
            string[] newAdds = addresses.Split(delim);
            foreach (string add in newAdds)
            {
                if (add.Contains("@") == false)
                    return null;
            }
            return newAdds;
        }

        public ActionResult Error()
        {
            return View();
        }
    }
}
