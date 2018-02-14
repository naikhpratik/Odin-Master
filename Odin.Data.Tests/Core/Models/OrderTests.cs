using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Odin.Data.Core.Models;
using System;
using System.Linq;

namespace Odin.Data.Tests.Core.Models
{
    [TestClass]
    public class OrderTests
    {
        [TestMethod]
        public void HomeFindingServices_HasHomeFindingProperty_ShouldHaveCompletedService()
        {
            var order = new Order();
            order.HomeFinding = new HomeFinding();
            order.HomeFinding.HomeFindingProperties.Add(new HomeFindingProperty());
            order.ServiceFlag = (int) ServiceCategory.AccompaniedHomeFinding;

            order.HomeFindingServices.Where(hfs => hfs.CompletedDate.HasValue).Count().Should().Be(1);
        }

        [TestMethod]
        public void HomeFindingServices_HasHomeFindingPropertyWithViewingDate_ShouldHaveCompletedServices()
        {
            var order = new Order();
            order.HomeFinding = new HomeFinding();
            order.HomeFinding.HomeFindingProperties.Add(new HomeFindingProperty(){ViewingDate = DateTime.Now});
            order.ServiceFlag = (int)ServiceCategory.AccompaniedHomeFinding;

            order.HomeFindingServices.Where(hfs => hfs.CompletedDate.HasValue).Count().Should().Be(2);
        }

        [TestMethod]
        public void HomeFindingServices_HasSelectedHomeFindingProperty_ShouldHaveCompletedServices()
        {
            var order = new Order();
            order.HomeFinding = new HomeFinding();
            order.HomeFinding.HomeFindingProperties.Add(new HomeFindingProperty() { selected = true});
            order.ServiceFlag = (int)ServiceCategory.AccompaniedHomeFinding;

            order.HomeFindingServices.Where(hfs => hfs.CompletedDate.HasValue).Count().Should().Be(3);
        }

        [TestMethod]
        public void HomeFindingServices_HasHomeFindingPropertyButNoHomefindingService_ShouldHaveNoHomeFindingServices()
        {
            var order = new Order();
            order.HomeFinding = new HomeFinding();
            order.HomeFinding.HomeFindingProperties.Add(new HomeFindingProperty());

            order.HomeFindingServices.Where(hfs => hfs.CompletedDate.HasValue).Count().Should().Be(0);
        }
    }
}
