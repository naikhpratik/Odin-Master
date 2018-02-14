using Odin.Data.Core.Models;
using Odin.Data.Core.Repositories;
using System.Linq;

namespace Odin.Data.Persistence
{
    public class ChildrenRepository : IChildrenRepository
    {
        private readonly IApplicationDbContext _context;

        public ChildrenRepository(IApplicationDbContext context)
        {
            _context = context;
        }

        public Child GetChildFor(string userId, string id, string userRole)
        {
            if (UserRoles.Consultant == userRole)
            {
                return _context.Children.SingleOrDefault(c => c.Id == id && c.Order.ConsultantId == userId);
            }
            else if (UserRoles.ProgramManager == userRole)
            {
                return _context.Children.SingleOrDefault(c => c.Id == id && c.Order.ProgramManagerId == userId);
            }
            else if (UserRoles.Transferee == userRole)
            {
                return _context.Children.SingleOrDefault(c => c.Id == id && c.Order.TransfereeId == userId);
            }

            return null;
        }

    }
}
