﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bogus;
using Odin.Data.Core.Models;

namespace Odin.Data.Builders
{
    public static class OrderBuilder
    {
        public static List<Order> New(int count = 1)
        {
            var relocationTypes = new[] {"International", "Domestic"};
            var rmcs = new[] {"AiReS", "Graebel", "Cartus", "Sirva"};

            var order = new Faker<Order>()
                .RuleFor(o => o.TrackingId, f => f.IndexFaker + 1.ToString())
                .RuleFor(o => o.RelocationType, f => f.PickRandom(relocationTypes))
                .RuleFor(o => o.DestinationCity, f => "integration city")
                .RuleFor(o => o.DestinationState, f => f.Address.State())
                .RuleFor(o => o.DestinationCountry, f => f.Address.CountryCode())
                .RuleFor(o => o.DestinationZip, f => f.Address.ZipCode())
                .RuleFor(o => o.OriginCity, f => f.Address.City())
                .RuleFor(o => o.OriginCountry, f => f.Address.CountryCode())
                .RuleFor(o => o.OriginState, f => f.Address.State())
                .RuleFor(o => o.EstimatedArrivalDate, f => f.Date.Future())
                .RuleFor(o => o.FamilyDetails, f => f.Lorem.Sentence())
                .RuleFor(o => o.PreTripDate, f => f.Date.Future(1))
                .RuleFor(o => o.Client, f => f.Company.CompanyName())
                .RuleFor(o => o.Rmc, f => f.PickRandom(rmcs))
                .RuleFor(o => o.Id, f=> Guid.NewGuid().ToString());

            var orders = order.Generate(count).ToList();

            return orders;
        }
    }
}
