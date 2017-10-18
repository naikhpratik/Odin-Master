﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Azure.Mobile.Server;

namespace Odin.Data.Core.Models
{
    public class Order : EntityData 
    {

        public Order()
        {
            Services = new Collection<Service>();
        }

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
        public int TempHousingDays { get; set; }
        public DateTime? TempHousingEndDate { get; set; }

        public string FamilyDetails { get; set; }

        public bool TransfereeInviteEnabled { get; set; }
        public string SeCustNumb { get; set; }
        public string Rmc { get; set; }
        public string Client { get; set; }

        public int? RentId { get; set; }
        public Rent Rent { get; set; }
        
        public string TransfereeId { get; set; }
        public virtual Transferee Transferee { get; set; }

        public string ProgramManagerId { get; set; }
        public virtual Manager ProgramManager { get; set; }

        public string ConsultantId { get; set; }
        public virtual Consultant Consultant { get; set; }

        public virtual ICollection<Service> Services { get; private set; }

        public DateTime? LastContactedDate{get; set; }

        public bool IsRush { get; set; }

        public bool IsVip { get; set; }

        public bool HasService(int serviceTypeId)
        {
            return Services.Any<Service>(s => s.ServiceTypeId == serviceTypeId);
        }

        public Service GetService(int serviceTypeId)
        {
            return Services.FirstOrDefault<Service>(s => s.ServiceTypeId == serviceTypeId);
        }

    }
}
