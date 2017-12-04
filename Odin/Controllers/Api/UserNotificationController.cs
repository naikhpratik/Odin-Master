﻿using AutoMapper;
using Microsoft.ApplicationInsights;
using Microsoft.AspNet.Identity;
using Odin.Data.Core;
using Odin.Data.Core.Dtos;
using Odin.Data.Core.Models;
using Odin.Domain;
using Odin.Filters;
using Odin.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using WebGrease.Css.Extensions;



namespace Odin.Controllers.Api
{
    public class UserNotificationController : ApiController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UserNotificationController(IUnitOfWork unitOfWork, IMapper mapper, IAccountHelper accountHelper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }


        [Authorize]
        [HttpPost]
        [Route("Api/UserNotification/MarkAsRead")]
        public IHttpActionResult MarkAsRead()
        {
            var userId = User.Identity.GetUserId();
            var notifications = _unitOfWork.UserNotifications.GetUserNotification(userId);


            //notifications.ForEach(n => n.Read());

            _unitOfWork.Complete();

            return Ok();
        }

        [Authorize]
        [HttpPost]
        [Route("Api/UserNotification/NotificationMarkAsRead/{NotificationId}")]
        public IHttpActionResult NotificationMarkAsRead(string NotificationId)
        {
            var UserId = User.Identity.GetUserId();
            var notificataion = _unitOfWork.UserNotifications.GetUserNotificationByNotificationId(UserId, NotificationId);

            notificataion.Read();

            _unitOfWork.Complete();

            return Ok(NotificationId);
        }
    }

}