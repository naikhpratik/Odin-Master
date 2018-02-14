using Odin.Data.Core.Models;
using Odin.Data.Core.Repositories;
using System.Linq;

namespace Odin.Data.Persistence
{
    public class PetsRepository : IPetsRepository
    {
        private readonly IApplicationDbContext _context;

        public PetsRepository(IApplicationDbContext context)
        {
            _context = context;
        }

        public Pet GetPetFor(string userId, string id, string userRole)
        {
            if (UserRoles.Consultant == userRole)
            {
                return _context.Pets.SingleOrDefault(p => p.Id == id && p.Order.ConsultantId == userId);
            }
            else if(UserRoles.ProgramManager == userRole)
            {
                return _context.Pets.SingleOrDefault(p => p.Id == id && p.Order.ProgramManagerId == userId);
            }
            else if (UserRoles.Transferee == userRole)
            {
                return _context.Pets.SingleOrDefault(p => p.Id == id && p.Order.TransfereeId == userId);
            }

            return null;
        }
    }
}
