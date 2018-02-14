using AutoMapper;
using Microsoft.AspNet.Identity;
using Odin.Data.Core;
using Odin.Data.Core.Models;
using Odin.Extensions;
using Odin.Interfaces;
using Odin.ViewModels.Orders.Transferee;
using System;
using System.Net;
using System.Web.Mvc;

namespace Odin.Controllers
{
    [Authorize]
    public class HomeFindingPropertiesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IImageStore _imageStore;

        public HomeFindingPropertiesController(IUnitOfWork unitOfWork, IMapper mapper, IImageStore imageStore)
        {
            _mapper = mapper;
            _imageStore = imageStore;
            _unitOfWork = unitOfWork;
        }

        // POST /homefindingproperties/create
        [HttpPost]
        public ActionResult Create(HousingPropertyViewModel propertyVM)
        {
            var userId = User.Identity.GetUserId();

            Order order = _unitOfWork.Orders.GetOrderFor(userId, propertyVM.OrderId, User.GetUserRole());
            if (order == null || !order.HasHomeFinding)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            HomeFindingProperty homeFindingProperty = new HomeFindingProperty();
            // mapping wipes out the Id - this is hack to resolve that
            propertyVM.Id = homeFindingProperty.Id;
            propertyVM.CurrUser = User;
            _mapper.Map<HousingPropertyViewModel, HomeFindingProperty>(propertyVM, homeFindingProperty);
            
            HomeFinding homeFinding = order.HomeFinding;
            homeFinding.HomeFindingProperties.Add(homeFindingProperty);

            foreach (var postedFile in propertyVM.UploadedPhotos)
            {
                if (postedFile != null) {
                    try
                    {
                        var storageId = _imageStore.SaveImage(postedFile.InputStream);
                        var urlStr = _imageStore.UriFor(storageId).AbsoluteUri;
                        var photo = new Photo(storageId, urlStr);
                        homeFindingProperty.Property.Photos.Add(photo);
                    }
                    catch (Exception e)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.ServiceUnavailable);
                    }
                }
            }

            try
            {
                _unitOfWork.Complete();
            }
            catch (Exception e)
            {
                return new HttpStatusCodeResult(HttpStatusCode.ServiceUnavailable);
            }

            return new HttpStatusCodeResult(HttpStatusCode.Created);
        }

        [HttpPut]
        public ActionResult UpdateLiked(HousingPropertyViewModel propertyVM)
        {
            HomeFindingProperty homeFindingProperty;
            homeFindingProperty = _unitOfWork.HomeFindingProperties.GetHomeFindingPropertyById(propertyVM.Id);

            if (homeFindingProperty == null || !homeFindingProperty.HomeFinding.Order.HasHomeFinding)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            // !!!: Do NOT use automapper here. There are issues with mapping back from the VM and in the essence of time I couldn't find a good solution
            // AutoMapper might not be the best best tool for two way mapping
            // https://lostechies.com/jimmybogard/2009/09/18/the-case-for-two-way-mapping-in-automapper/

            // for now only support a subset of updated values
            homeFindingProperty.Liked = propertyVM.Liked;

            _unitOfWork.Complete();

            return new HttpStatusCodeResult(HttpStatusCode.NoContent);
        }

        [HttpPut]
        public ActionResult UpdateViewingDate(HousingPropertyViewModel propertyVM)
        {
            HomeFindingProperty homeFindingProperty;
            homeFindingProperty = _unitOfWork.HomeFindingProperties.GetHomeFindingPropertyById(propertyVM.Id);

            if (homeFindingProperty == null || !homeFindingProperty.HomeFinding.Order.HasHomeFinding)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            // !!!: Do NOT use automapper here. There are issues with mapping back from the VM and in the essence of time I couldn't find a good solution
            // AutoMapper might not be the best best tool for two way mapping
            // https://lostechies.com/jimmybogard/2009/09/18/the-case-for-two-way-mapping-in-automapper/
            homeFindingProperty.ViewingDate = propertyVM.ViewingDate;

            _unitOfWork.Complete();

            return new HttpStatusCodeResult(HttpStatusCode.NoContent);
        }

        [HttpPut]
        public ActionResult Select(string id)
        {
            HomeFindingProperty homeFindingProperty;
            homeFindingProperty = _unitOfWork.HomeFindingProperties.GetHomeFindingPropertyById(id);

            if (homeFindingProperty == null || !homeFindingProperty.HomeFinding.Order.HasHomeFinding)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            if (homeFindingProperty.selected.HasValue)
                homeFindingProperty.selected = !homeFindingProperty.selected;
            else
                homeFindingProperty.selected = true;

            _unitOfWork.Complete();
            return new HttpStatusCodeResult(HttpStatusCode.NoContent);
        }

        [HttpPut]
        public ActionResult SelectProperty(string id)
        {
            HomeFindingProperty homeFindingProperty;
            homeFindingProperty = _unitOfWork.HomeFindingProperties.GetHomeFindingPropertyById(id);

            if (homeFindingProperty == null || !homeFindingProperty.HomeFinding.Order.HasHomeFinding)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            if (homeFindingProperty.selected.HasValue)
            {
                homeFindingProperty.selected = !homeFindingProperty.selected;
            }
                
            _unitOfWork.Complete();
            return new HttpStatusCodeResult(HttpStatusCode.NoContent);
        }

        // DELETE /homefindingproperties/delete/[hfpId]
        [HttpDelete]
        public ActionResult Delete(string id)
        {
            HomeFindingProperty homeFindingProperty;
            homeFindingProperty = _unitOfWork.HomeFindingProperties.GetHomeFindingPropertyById(id);

            if (homeFindingProperty == null || !homeFindingProperty.HomeFinding.Order.HasHomeFinding)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            homeFindingProperty.Deleted = true;
            _unitOfWork.Complete();
            return new HttpStatusCodeResult(HttpStatusCode.NoContent);
        }

        // GET /homefindingproperties/propertypartial/[propertyId]
        [HttpGet]
        public ActionResult PropertyPartial(string id)
        {
            HomeFindingProperty homeFindingProperty;
            homeFindingProperty = _unitOfWork.HomeFindingProperties.GetHomeFindingPropertyById(id);

            if (homeFindingProperty == null || !homeFindingProperty.HomeFinding.Order.HasHomeFinding)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            HousingPropertyViewModel viewModel = _mapper.Map<HomeFindingProperty, HousingPropertyViewModel>(homeFindingProperty);

            return PartialView("~/views/orders/partials/_PropertyDetails.cshtml", viewModel);
        }

        public ActionResult RadioButtonList(string title)
        {
            ViewBag.Title = title;
            return PartialView("~/Views/Shared/Partials/multipleChoiceRadioButtons.cshtml");
        }
    }
}