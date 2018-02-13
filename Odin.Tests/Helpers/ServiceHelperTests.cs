using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Odin.Data.Core.Models;
using Odin.Helpers;
using System.Linq;

namespace Odin.Tests.Helpers
{
    [TestClass]
    public class ServiceHelperTests
    {
        [TestMethod]
        public void GetCategoriesForServiceFlag_ValidServiceFlag_ReturnInitConsultAndWelcomePacket()
        {
            int serviceflag = 3;
            var result = ServiceHelper.GetCategoriesForServiceFlag(serviceflag);
            result.Count().Should().Be(2);
            result.Should().Contain(ServiceCategory.InitialConsultation);
            result.Should().Contain(ServiceCategory.WelcomePacket);
        }

        [TestMethod]
        public void GetCategoriesForServiceFlag_EmptyServiceFlag_ReturnNoCategories()
        {
            int serviceflag = 0;
            var result = ServiceHelper.GetCategoriesForServiceFlag(serviceflag);
            result.Count().Should().Be(0);
        }

    }
}
