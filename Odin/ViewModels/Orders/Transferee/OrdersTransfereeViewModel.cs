using Odin.Data.Core.Models;
using Odin.Helpers;
using Odin.ViewModels.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using PetViewModel = Odin.ViewModels.Shared.PetViewModel;


namespace Odin.ViewModels.Orders.Transferee
{
    public class OrdersTransfereeViewModel
    {
        public OrdersTransfereeViewModel()
        {
            Children = new List<ChildViewModel>();
            Services = new List<ServiceViewModel>();
            Pets = new List<PetViewModel>();
            PossibleServices = new List<ServiceTypeViewModel>();
            HomeFinding = new HomeFindingViewModel();
        }

        public string Id { get; set; }

        public bool TransfereeInviteEnabled { get; set; }

        [DisplayName("City:")]
        public string DestinationCity { get; set; }

        [DisplayName("State:")]
        public string DestinationState { get; set; }

        [DisplayName("Country:")]
        public string DestinationCountry { get; set; }

        [DisplayName("City:")]
        public string OriginCity { get; set; }

        [DisplayName("State:")]
        public string OriginState { get; set; }

        [DisplayName("Country:")]
        public string OriginCountry { get; set; }

        public bool IsRush { private get; set; }

        public string RelocationType { get; set; }

        [DisplayName("Rush:")]
        public string IsRushDisplay {
            get
            {
                return IsRush ? "Yes" : "No";
            }
        }

        public bool IsVip { private get; set; }

        [DisplayName("VIP:")]
        public string IsVipDisplay
        {
            get
            {
                return IsVip ? "Yes" : "No";
            }
        }

        [DisplayName("Length(Days):")]
        public int TempHousingDays { get; set; }


        public DateTime? FinalArrivalDate { get; set; }
        public string FinalArrivalDateDisplay => DateHelper.GetViewFormat(FinalArrivalDate);


        public DateTime? TempHousingEndDate { get; set; }

        [DisplayName("Last Day:")]
        public string TempHousingEndDateDisplay {
            get { return DateHelper.GetViewFormat(TempHousingEndDate); }
        }

        public DateTime? PreTripDate {get; set; }
        [DisplayName("Familiarization Trip:")]
        public string PreTripDateDisplay
        {
            get { return DateHelper.GetViewFormat(PreTripDate); }
        }

        [DisplayName("Trip Notes:")]
        public string PreTripNotes { get; set; }

        
        public DateTime? HomeFindingDate { get; set; }

        [DisplayName("Home Finding:")]
        public string HomeFindingDateDisplay
        {
            get { return DateHelper.GetViewFormat(HomeFindingDate); }
        }


        public bool IsAssignment {get; set; }
        [DisplayName("Move Type:")]
        public string IsAssignmentDisplay
        {
            get { return IsAssignment ? "Temporary" : "Permanent"; }
            
        }

        public bool IsInternational { get; set; }
        [DisplayName("Relocation Type:")]
        public string IsInternationalDisplay
        {
            get { return IsInternational ? "International" : "Domestic"; }

        }

        public DateTime? EstimatedArrivalDate {get; set; }

        [DisplayName("Final Arrival:")]
        public string EstimatedArrivalDateDisplay
        {
            get { return DateHelper.GetViewFormat(EstimatedArrivalDate); }
        }
        
        public DateTime? WorkStartDate { get; set; }

        [DisplayName("Work Start:")]
        public string WorkStartDateDisplay {
            get { return DateHelper.GetViewFormat(WorkStartDate); }
            
        }

        public DateTime? EstimatedDepartureDate {get; set; }

        [DisplayName("Estimated Departure:")]
        public string EstimatedDepartureDateDisplay
        {
            get { return DateHelper.GetViewFormat(EstimatedDepartureDate); }
        }

        [DisplayName("Spouse/Partner:")]
        public string SpouseName { get; set; }

        [DisplayName("Visa Type:")]
        public string SpouseVisaType { get; set; }

        [DisplayName("RMC:")]
        public string Rmc { get; set; }

        [DisplayName("Contact:")]
        public string RmcContact { get; set; }

        [DisplayName("Email:")]
        public string RmcContactEmail { get; set; }

        private IEnumerable<ChildViewModel> _children;
        public virtual IEnumerable<ChildViewModel> Children
        {
            get
            {
                return _children.Where(c => !c.Deleted).ToList();
            }
            set
            {
                _children = value;
            }
        }

        [DisplayName("Education Preferences:")]
        public string ChildrenEducationPreferences { get; set; }

        [DisplayName("School District:")]
        public string SchoolDistrict { get; set; }
        public string SchoolDistrictDisplay => (SchoolDistrict != null ? SchoolDistrict : string.Empty);

        private IEnumerable<PetViewModel> _pets;
        public virtual IEnumerable<PetViewModel> Pets
        {
            get
            {
                return _pets.Where(p => !p.Deleted).ToList();
            }
            set
            {
                _pets = value;
            }
        }

        [DisplayName("Pet Notes:")]
        public string PetNotes { get; set; }

        public IEnumerable<ServiceViewModel> Services { get; set; }
        public IEnumerable<ServiceTypeViewModel> PossibleServices { get; set; }

        public TransfereeViewModel Transferee { get; set; }

        public string ProgramManagerId { get; set; }
        public virtual Manager ProgramManager { get; set; }

        public string PhoneNumberDisplay => (ProgramManager != null && ProgramManager.PhoneNumber != null)
           ? DateHelper.GetViewFormat(ProgramManager.PhoneNumber)
           : String.Empty;
        public string SeContactUidDisplay => (ProgramManager != null && ProgramManager.SeContactUid != null)
           ? ProgramManager.SeContactUid.ToString()
           : String.Empty;

        public string SeCustNumb { get; set; }
        public string Client { get; set; }
        public string ClientFileNumber { get; set; }
        public string ClientFileNumberDisplay => ClientFileNumber ?? ClientFileNumber;
           

        public string RentId { get; set; }
        public HomeFindingViewModel HomeFinding { get; set; }

        [DisplayName("Budget:")]
        public string HousingBudgetDisplay
        {
            get
            {
                return (HomeFinding != null && HomeFinding.HousingBudget.HasValue)
                    ? HomeFinding.HousingBudget.Value.ToString("C")
                    : String.Empty;
            }
        }

        [DisplayName("Number of Bathrooms:")]
        public string NumberOfBathroomsDisplay
        {
            get
            {
                return (HomeFinding != null && HomeFinding.NumberOfBathrooms != null)
                    ? HomeFinding.NumberOfBathrooms.Name
                    : String.Empty;
            }
        }

        [DisplayName("Number of Bedrooms:")]
        public string NumberOfBedroomsDisplay
        {
            get
            {
                return (HomeFinding != null && HomeFinding.NumberOfBedrooms.HasValue)
                    ? HomeFinding.NumberOfBedrooms.Value.ToString()
                    : String.Empty;
            }
        }

        [DisplayName("Square Footage:")]
        public string SquareFootageDisplay
        {
            get
            {
                return (HomeFinding != null && HomeFinding.SquareFootage.HasValue)
                    ? HomeFinding.SquareFootage.Value.ToString()
                    : String.Empty;
            }
        }

        [DisplayName("Housing Type:")]
        public string HousingTypeDisplay
        {
            get
            {
                return (HomeFinding != null && HomeFinding.HousingType != null)
                    ? HomeFinding.HousingType.Name
                    : String.Empty;
            }
        }

        [DisplayName("Preferred Transportation:")]
        public string TransportationTypeDisplay
        {
            get
            {
                return (HomeFinding != null && HomeFinding.TransportationType != null)
                    ? HomeFinding.TransportationType.Name
                    : String.Empty;
            }
        }

        [DisplayName("Furnished:")]
        public string IsFurnishedDisplay
        {
            get
            {
                return (HomeFinding != null && HomeFinding.IsFurnished.HasValue)
                    ? HomeFinding.IsFurnished.Value ? "Yes" : "No"
                    : String.Empty;
            }
        }

        [DisplayName("Max Commute (Minutes):")]
        public string MaxCommuteDisplay
        {
            get
            {
                return (HomeFinding != null && HomeFinding.MaxCommute.HasValue)
                    ? HomeFinding.MaxCommute.Value.ToString()
                    : String.Empty;
            }
        }

        [DisplayName("Parking:")]
        public string HasParkingDisplay
        {
            get
            {
                return (HomeFinding != null && HomeFinding.HasParking.HasValue)
                    ? HomeFinding.HasParking.Value ? "Yes" : "No"
                    : String.Empty;
            }
        }

        [DisplayName("AC/Central Air:")]
        public string HasACDisplay
        {
            get
            {
                return (HomeFinding != null && HomeFinding.HasAC.HasValue)
                    ? HomeFinding.HasAC.Value ? "Yes" : "No"
                    : String.Empty;
            }
        }

        [DisplayName("Exercise Room:")]
        public string HasExerciseRoomDisplay
        {
            get
            {
                return (HomeFinding != null && HomeFinding.HasExerciseRoom.HasValue)
                    ? HomeFinding.HasExerciseRoom.Value ? "Yes": "No"
                    : String.Empty;
            }
        }

        [DisplayName("Comments:")]
        public string CommentsDisplay
        {
            get
            {
                return (HomeFinding != null && !String.IsNullOrEmpty(HomeFinding.Comments))
                    ? HomeFinding.Comments
                    : String.Empty;
            }
        }

        [DisplayName("Preferred Area:")]
        public string AreaTypeDisplay
        {
            get
            {
                return (HomeFinding != null && HomeFinding.AreaType != null)
                    ? HomeFinding.AreaType.Name
                    : String.Empty;
            }
        }

        [DisplayName("Laundry:")]
        public string HasLaundryDisplay
        {
            get
            {
                return (HomeFinding != null && HomeFinding.HasLaundry.HasValue)
                    ? HomeFinding.HasLaundry.Value ? "Yes" : "No"
                    : String.Empty;
            }
        }

        [DisplayName("Cars Owned:")]
        public string NumberOfCarsOwnedDisplay
        {
            get
            {
                return (HomeFinding != null && HomeFinding.NumberOfCarsOwned.HasValue)
                    ? HomeFinding.NumberOfCarsOwned.Value.ToString()
                    : String.Empty;
            }
        }


        public IEnumerable<NumberOfBathroomsType> NumberOfBathrooms { get; set; }

        public IEnumerable YesNoDropDown
        {
            get
            {
                return new[]
                {
                    new {Value = "True", Text = "Yes"},
                    new {Value = "False", Text = "No"},
                };
            }
        }

        public IEnumerable<HousingType> HousingTypes { get; set; }

        public IEnumerable<AreaType> AreaTypes { get; set; }

        public IEnumerable<TransportationType> TransportationTypes { get; set; }

        [DisplayName("Lease Term(Months):")]
        public int? LeaseTerm { get; set; }

        public IEnumerable<BrokerFeeType> BrokerFeeTypes { get; set; }

        public BrokerFeeType BrokerFeeType { private get; set; }

        public byte? BrokerFeeTypeId { get; set; }

        [DisplayName("Broker Fee Paid By:")]
        public string BrokerFeeTypeDisplay
        {
            get
            {
                return (BrokerFeeType != null)
                    ? BrokerFeeType.Name
                    : String.Empty;
            }
        }

        [DisplayName("Length Of Assignment (Months):")]
        public int? LengthOfAssignment { get; set; }

        public IEnumerable<DepositType> DepositTypes { get; set; }

        public DepositType DepositType { private get; set; }

        public byte? DepositTypeId { get; set; }

        [DisplayName("Rent/Deposit Paid By:")]
        public string DepositTypeDisplay
        {
            get
            {
                return (DepositType != null)
                    ? DepositType.Name
                    : String.Empty;
            }
        }

        [DisplayName("Program Manager")]
        public string ProgramManagerFullName { get; set; }

        [DisplayName("Email")]
        public string ProgramManagerEmail { get; set; }

        [DisplayName("Phone")]
        public string ProgramManagerPhoneNumber { get; set; }

        [DisplayName("Consultant")]
        public string ConsultantFullName { get; set; }

        [DisplayName("Email")]
        public string ConsultantEmail { get; set; }

        [DisplayName("Phone")]
        public string ConsultantPhoneNumber { get; set; }

        [DisplayName("Program Notes")]
        [DisplayFormat(NullDisplayText = "There are currently no notes.")]
        public string ProgramNotes { get; set; }

        public bool HasHomeFinding { get; set; }

    }
}