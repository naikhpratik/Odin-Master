using Odin.Data.Core.Models;

namespace Odin.Data.Core.Repositories
{
    public interface IPetsRepository
    {
        Pet GetPetFor(string userId, string id, string userRole);
    }
}