﻿using Microsoft.AspNet.Identity;
using Odin.Data.Core.Models;
using Odin.Data.Core.Repositories;
using Odin.Data.Helpers;
using System.Linq;

namespace Odin.Data.Persistence
{
    public class ConsultantsRepository : IConsultantsRepository
    {
        private readonly ApplicationDbContext _context;

        public ConsultantsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Consultant GetConsultantBySeContactUid(int seContactUid)
        {
            return _context.Consultants.FirstOrDefault(c => c.SeContactUid == seContactUid);
        }
        public Consultant GetConsultantById(string id)
        {
            return _context.Consultants.FirstOrDefault(c => c.Id == id);
        }
        public void Add(Consultant consultant)
        {
            var userManager = UserHelper.GetUserManager<Consultant>(_context);
            consultant.UserName = consultant.Email;
            var result = userManager.Create(consultant, PasswordHelper.TemporaryPassword());
            userManager.AddToRole(consultant.Id, UserRoles.Consultant);
        }
    }
}
