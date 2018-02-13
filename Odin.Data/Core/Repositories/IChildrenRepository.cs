using Odin.Data.Core.Models;

namespace Odin.Data.Core.Repositories
{
    public interface IChildrenRepository
    {
        Child GetChildFor(string userId, string id, string userRole);
    }
}