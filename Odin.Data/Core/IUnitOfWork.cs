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
        IServicesRepository Services { get; }
        IServiceTypesRepository ServiceTypes { get; }
        IChildrenRepository Children { get; }
        IPetsRepository Pets { get; }
        INumberOfBathroomsTypesRepository NumberOfBathrooms { get; }
        IHousingTypesRepository HousingTypes { get; }
        IAreaTypesRepository AreaTypes { get; }
        ITransportationTypesRepository TransportationTypes { get; }
        IDepositTypesRepository DepositTypes { get; }
        IBrokerFeeTypesRepository BrokerFeeTypes { get; }
        void Complete();
    }
}
