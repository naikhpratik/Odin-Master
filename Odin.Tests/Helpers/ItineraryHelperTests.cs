using AutoMapper;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Odin.Data.Core;
using Odin.Data.Core.Models;
using Odin.Data.Core.Repositories;
using Odin.Helpers;


namespace Odin.Tests.Helpers
{
    [TestClass]
    public class ItineraryHelperTests
    {
        private ItineraryHelper _itineraryHelper;
        private Mock<IServicesRepository> _mockServicesRepository;
        private Mock<IAppointmentsRepository> _mockAppointmentsRepository;
        private Mock<IHomeFindingPropertyRepository> _mockHomeFindingPropertiesRepository;

        [TestInitialize]
        public void TestInitialize()
        {            
           
        }
        [TestMethod]
        public void Build_WhenCalled_Should_NotReturn_Null()
        {
            var mockMapper = new Mock<IMapper>();
            _mockServicesRepository = new Mock<IServicesRepository>();
            _mockAppointmentsRepository = new Mock<IAppointmentsRepository>();
            _mockHomeFindingPropertiesRepository = new Mock<IHomeFindingPropertyRepository>();

            var mockUnitOfWork = new Mock<IUnitOfWork>();
           

            _itineraryHelper = new ItineraryHelper(mockUnitOfWork.Object, mockMapper.Object);
            string orderId = "1";

            var result = _itineraryHelper.Build(new Order());
            result.Should().NotBeNull();
        }
        [TestMethod]
        public void Build_WhenCalled_NullOrder_ShouldReturnNull()
        {
            var mockMapper = new Mock<IMapper>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            _itineraryHelper = new ItineraryHelper(mockUnitOfWork.Object, mockMapper.Object);

            var result = _itineraryHelper.Build(null);
            result.Should().BeNull();
        }

        
    }
}
