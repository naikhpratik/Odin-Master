using Odin.Data.Core.Models;
using Odin.Data.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace Odin.Data.Persistence
{
    public class HomeFindingPropertyRepository : IHomeFindingPropertyRepository
    {
        private readonly IApplicationDbContext _context;

        public HomeFindingPropertyRepository(IApplicationDbContext context)
        {
            _context = context;
        }

        public HomeFindingProperty GetHomeFindingPropertyById(string homeFindingPropertyId)
        {
            return _context.HomeFindingProperties
                .Where(hfp => hfp.Id.Equals(homeFindingPropertyId))
                .Include(hfp => hfp.Property.Photos)
                .Include(hfp => hfp.HomeFinding)
                .Include(hfp => hfp.HomeFinding.Order)
                .SingleOrDefault<HomeFindingProperty>();
        }

        public HomeFindingProperty GetHomeFindingPropertyByPropertyId(string propertyId)
        {
            return _context.HomeFindingProperties
                .Where(hfp => hfp.Id.Equals(propertyId))
                .Include(hfp => hfp.Property.Photos)
                .SingleOrDefault<HomeFindingProperty>();
        }

        public IEnumerable<HomeFindingProperty> GetUpcomingHomeFindingPropertiesByHomeFindingId(string homeFindingId)
        {
            return _context.HomeFindingProperties
                .Where(hfp => hfp.HomeFinding.Id.Equals(homeFindingId) && hfp.Deleted == false && hfp.ViewingDate >= DateTime.Now)
                .Include(hfp => hfp.Property);
        }
    }
}
