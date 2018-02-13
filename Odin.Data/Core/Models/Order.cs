using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Odin.Data.Core.Models
{
    public class Order : MobileTable 
    {

        public Order()
        {
            Services = new Collection<Service>();
            Children = new Collection<Child>();
            Pets = new Collection<Pet>();
            Appointments =  new Collection<Appointment>();
            Notifications = new Collection<Notification>();
        }

        public string ProgramName { get; set; }
        public string TrackingId { get; set; }
        public string RelocationType { get; set; }
        public string DestinationCity { get; set; }
        public string DestinationState { get; set; }
        public string DestinationZip { get; set; }
        public string DestinationCountry { get; set; }
        public string OriginCity { get; set; }
        public string OriginState { get; set; }
        public string OriginCountry { get; set; }
        public DateTime? EstimatedArrivalDate { get; set; }
        public DateTime? PreTripDate { get; set; }
        public string PreTripNotes { get; set; }
        public DateTime? HomeFindingDate { get; set; }
        public DateTime? WorkStartDate { get; set; }
        public int TempHousingDays { get; set; }
        public DateTime? TempHousingEndDate { get; set; }
        public string SchoolDistrict { get; set; }
        public string FamilyDetails { get; set; }

        public string SeCustStatus { get; set; }

        public float? DaysAuthorized { get; set; }

        public bool TransfereeInviteEnabled { get; set; }
        public string SeCustNumb { get; set; }
        public string Rmc { get; set; }
        public string RmcContact { get; set; }
        public string RmcContactEmail { get; set; }
        public string Client { get; set; }
        public string ClientFileNumber { get; set; }

        public DateTime? LastContactedDate {get; set; }

        public bool IsRush { get; set; }
        public bool IsVip { get; set; }
        public bool IsInternational { get; set; }
        public bool IsAssignment { get; set; }
        public DateTime? EstimatedDepartureDate { get; set; }

        public string SpouseName { get; set; }
        public string SpouseVisaType { get; set; }
        public virtual ICollection<Child> Children { get; private set; }
        public string ChildrenEducationPreferences { get; set; }

        public virtual ICollection<Pet> Pets { get; private set; }
        public string PetNotes { get; set; }

        public string TransfereeId { get; set; }
        public virtual Transferee Transferee { get; set; }

        public string ProgramManagerId { get; set; }
        public virtual Manager ProgramManager { get; set; }

        public string ConsultantId { get; set; }
        public virtual Consultant Consultant { get; set; }

        public DepositType DepositType { get; set; }

        public byte? DepositTypeId { get; set; }

        public int? LeaseTerm { get; set; }

        public BrokerFeeType BrokerFeeType { get; set; }

        public byte? BrokerFeeTypeId { get; set; }

        public int? LengthOfAssignment { get; set; }

        public HomeFinding HomeFinding { get; set; }

        public virtual ICollection<Appointment> Appointments { get; set; }
        public int ServiceFlag { get; set; }

        public virtual ICollection<Service> Services { get; private set; }

        public virtual ICollection<Notification> Notifications { get; private set; }

        //public virtual ICollection<UserNotification> UserNotifications { get; private set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public string ProgramNotes { get; set; }

        public bool HasService(int serviceTypeId)
        {
            return Services.Any<Service>(s => s.ServiceTypeId == serviceTypeId);
        }

        public Service GetService(int serviceTypeId)
        {
            return Services.FirstOrDefault<Service>(s => s.ServiceTypeId == serviceTypeId);
        }

        public bool HasHomeFinding
        {
            get { return HomeFinding != null && ((ServiceFlag & (int) ServiceCategory.AccompaniedHomeFinding) > 0 || (ServiceFlag & (int)ServiceCategory.UnAccompaniedHomeFinding) > 0); }
        }

        public IEnumerable<Service> HomeFindingServices
        {
            get
            {
                List<Service> hfServices;
                if (HasHomeFinding)
                {
                    hfServices = new List<Service>()
                    {
                        new Service()
                        {
                            Selected = true,
                            CompletedDate = HomeFinding.HomeFindingProperties.Count > 0
                                ? DateTime.Now
                                : (DateTime?) null,
                            ServiceType = new ServiceType()
                            {
                                Category = ServiceCategory.AccompaniedHomeFinding,
                                Name = "FindHomes",
                                ActionLabel = "Find Potential Homes to View"
                            }
                        },
                        new Service()
                        {
                            Selected = true,
                            CompletedDate = HomeFinding.HomeFindingProperties.Any(hf => hf.ViewingDate.HasValue || (hf.selected.HasValue && hf.selected.Value))
                                ? DateTime.Now
                                : (DateTime?) null,
                            ServiceType = new ServiceType()
                            {
                                Category = ServiceCategory.AccompaniedHomeFinding,
                                Name = "ScheduleViewings",
                                ActionLabel = "Schedule Viewings of \"Liked\" Homes"
                            }
                        },
                        new Service()
                        {
                            Selected = true,
                            CompletedDate = HomeFinding.HomeFindingProperties.Any(hf => hf.selected.HasValue && hf.selected.Value)
                                    ? DateTime.Now
                                    : (DateTime?) null,
                            ServiceType = new ServiceType()
                            {
                                Category = ServiceCategory.AccompaniedHomeFinding,
                                Name = "SelectHome",
                                ActionLabel = "Select a Home"
                            }
                        }
                    };
                }
                else
                {
                    hfServices = new List<Service>();
                }

                return hfServices;
            }
        }

    }
}
