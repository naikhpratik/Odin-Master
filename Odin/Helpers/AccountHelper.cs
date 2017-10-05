﻿
using Microsoft.AspNet.Identity.Owin;
using Odin.Interfaces;
using RazorEngine;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Odin.Helpers
{
    public class AccountHelper : IAccountHelper
    {
        private readonly IEmailHelper _emailHelper;
        public AccountHelper(IEmailHelper emailHelper)
        {
            _emailHelper = emailHelper;
        }
       
        private ApplicationUserManager _userManager;

        public ApplicationUserManager UserManager
        {
            get
            {
                // return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
                return _userManager ?? System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }
        public async Task<string> SendEmailResetTokenAsync(string userID) //, string subject, string bdy)
        {
            var url = new UrlHelper(HttpContext.Current.Request.RequestContext);
            
            //Forgery check
            string code = await UserManager.GeneratePasswordResetTokenAsync(userID);

            //The link sent to the Transferees for access to reset password
            var callbackUrl = url.Action("ResetPassword", "Account", new { userID, code = code }, protocol: HttpContext.Current.Request.Url.Scheme);

            var user = await UserManager.FindByIdAsync(userID);

            //transferee's email address
            var eml = user.Email;            
            var name = user.FirstName + " " + user.LastName;

            var subject = "Reset Password";
            var templateFolderPath = HttpContext.Current.Server.MapPath(@"~\Views\Mailers\");
            string template = System.IO.File.ReadAllText(templateFolderPath + "ResetPassword.cshtml");
            var body = Razor.Parse(template, new { Name = name, Link = callbackUrl });

            //send the email, specify the content mime type
            var response = _emailHelper.SendEmail_SG(eml, subject, body, SendGrid.MimeType.Html);
            return callbackUrl;
        }

        public async Task<string> SendEmailCreateTokenAsync(string userID) //, string subject, string bdy)
        {
            var url = new UrlHelper(HttpContext.Current.Request.RequestContext);

            //Forgery check
            string code = await UserManager.GeneratePasswordResetTokenAsync(userID);
            //The link sent to the Transferees for access to reset password
            var callbackUrl = url.Action("CreatePassword", "Account", new { userID, code = code }, protocol: HttpContext.Current.Request.Url.Scheme);

            var user = await UserManager.FindByIdAsync(userID);

            //transferee's email address
            var eml = user.Email;
            var name = user.FirstName + " " + user.LastName;

            var subject = "Create Password";
            var templateFolderPath = HttpContext.Current.Server.MapPath(@"~\Views\Mailers\");
            string template = System.IO.File.ReadAllText(templateFolderPath + "CreatePassword.cshtml");
            var body = Razor.Parse(template, new { Name = name, Link = callbackUrl });

            //send the email, specify the content mime type
            var response = _emailHelper.SendEmail_SG(eml, subject, body, SendGrid.MimeType.Html);
            return callbackUrl;
        }
    }
}