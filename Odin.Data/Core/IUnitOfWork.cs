﻿using Odin.Data.Core.Repositories;

namespace Odin.Data.Core
{
    public interface IUnitOfWork
    {
        IUsersRepository Users { get; }
        IOrdersRepository Orders { get; }
        ITransfereesRepository Transferees { get; }
        IConsultantsRepository Consultants { get; }
        IManagersRepository Managers { get; }
        void Complete();
    }
}
