using AutoMapper;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Odin.Controllers;
using Odin.Data.Core;
using Odin.Data.Core.Models;
using Odin.Data.Core.Repositories;
using Odin.Interfaces;
using Odin.Tests.Extensions;
using System.Collections.Generic;
using System.Web.Mvc;

namespace Odin.Tests.Controllers
{
    [TestClass]
    public class BookMarkletControllerTests
    {
        private Odin.Controllers.BookMarkletController _controller;
        private Mock<IOrdersRepository> _mockRepository;
        private Mock<IMapper> _mockMapper;
        private Mock<IBookMarkletHelper> _mockBookMarkletHelper;
        private Mock<IQueueStore> _mockQueueStore;
        private string _userId;
        private string _userName;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockRepository = new Mock<IOrdersRepository>();
            _mockMapper = new Mock<IMapper>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            _mockQueueStore = new Mock<IQueueStore>();
            mockUnitOfWork.SetupGet(u => u.Orders).Returns(_mockRepository.Object);
            _mockBookMarkletHelper = new Mock<IBookMarkletHelper>();
            _controller = new BookMarkletController(mockUnitOfWork.Object, _mockMapper.Object, _mockQueueStore.Object, _mockBookMarkletHelper.Object);

            _userId = "1";
            _userName = "TestUser";
            _controller.MockControllerContextForUserAndRole(_userId,UserRoles.Consultant);
        }

        [TestMethod]
        public void Index_ValidUrl_ShouldReturnDefaultView()
        {
            var url = "http://test.com";

            var orders = new List<Order> {new Order() {Id = "1", HomeFinding = new HomeFinding(), ServiceFlag = (int)ServiceCategory.AccompaniedHomeFinding}};
            _mockRepository.Setup(r => r.GetOrdersFor(_userId,UserRoles.Consultant)).Returns(orders);
            _mockBookMarkletHelper.Setup(r => r.IsValidUrl(url)).Returns(true);

            var result = _controller.Index(url) as ViewResult;
            result.Should().NotBeNull();
            result.ViewName.Should().BeEmpty();
        }

        [TestMethod]
        public void Index_NoHomeFinding_ShouldReturnErrorView()
        {
            var url = "http://test.com";

            var orders = new List<Order> { new Order() { Id = "1" } };
            _mockRepository.Setup(r => r.GetOrdersFor(_userId, UserRoles.Consultant)).Returns(orders);
            _mockBookMarkletHelper.Setup(r => r.IsValidUrl(url)).Returns(true);

            var result = _controller.Index(url) as ViewResult;
            result.Should().NotBeNull();
            result.ViewName.Should().Be("Error");
        }

        [TestMethod]
        public void Index_BadUrl_ShouldReturnErrorView()
        {
            var url = "http://test.com";

            var orders = new List<Order> { new Order() { Id = "1" } };
            _mockRepository.Setup(r => r.GetOrdersFor(_userId,UserRoles.Consultant)).Returns(orders);
            _mockBookMarkletHelper.Setup(r => r.IsValidUrl(url)).Returns(false);

            var result = _controller.Index(url) as ViewResult;
            result.Should().NotBeNull();
            result.ViewName.Should().Be("Error");
        }

        [TestMethod]
        public void Index_NoOrders_ShouldReturnErrorView()
        {
            var url = "http://test.com";

            var orders = new List<Order>();
            _mockRepository.Setup(r => r.GetOrdersFor(_userId,UserRoles.Consultant)).Returns(orders);
            _mockBookMarkletHelper.Setup(r => r.IsValidUrl(url)).Returns(true);

            var result = _controller.Index(url) as ViewResult;
            result.Should().NotBeNull();
            result.ViewName.Should().Be("Error");
        }
    }
}
