using AutoMapper;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Odin.Controllers;
using Odin.Data.Builders;
using Odin.Data.Core.Models;
using Odin.Data.Persistence;
using Odin.Domain;
using Odin.IntegrationTests.Extensions;
using Odin.IntegrationTests.TestAttributes;
using Odin.Interfaces;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Odin.IntegrationTests.Controllers
{
    [TestFixture]
    public class BookMarkletControllerTests
    {

        private BookMarkletController _controller;
        private ApplicationDbContext _context;
        private Manager _pm;
        private Consultant _dsc;
        private Transferee _transferee;
        private Mock<IBookMarkletHelper> _mockBookMarkletHelper;

        [SetUp]
        public void SetUp()
        {
            var config = new MapperConfiguration(c => c.AddProfile(new MappingProfile()));

            var mapper = config.CreateMapper();

            _context = new ApplicationDbContext();

            _transferee = _context.Transferees.SingleOrDefault(u => u.Email.Equals("integrationee@dwellworks.com"));
            if (_transferee == null)
            {
                _transferee = new Transferee() { Email = "integrationee@dwellworks.com", UserName = "bm-ee-test" };
                _context.Transferees.Add(_transferee);
                _context.SaveChanges();
                _context.Entry(_transferee).Reload();
            }

            _pm = _context.Managers.First(u => u.UserName.Equals("odinpm@dwellworks.com"));

            _dsc = _context.Consultants.SingleOrDefault(u => u.Email.Equals("bm-integration@test.com"));
            if ( _dsc == null)
            {
                _dsc = new Consultant() { Email = "bm-integration@test.com", UserName = "bm-test" };
                _context.Consultants.Add(_dsc);
                _context.SaveChanges();
                _context.Entry(_dsc).Reload();
            }

            var queueStore = new QueueStore();
            _mockBookMarkletHelper = new Mock<IBookMarkletHelper>();

            _controller = new BookMarkletController(new UnitOfWork(_context), mapper, queueStore, _mockBookMarkletHelper.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        // Tests
        [Test, Isolated]
        public void Index_ValidUrlHasOrders_ShowBmViewWithOrders()
        {
            // Arrange
            Order order = OrderBuilder.New().First();
            order.Transferee = _transferee;
            order.ProgramManager = _pm;
            order.Consultant = _dsc;
            order.HomeFinding = new HomeFinding();
            order.ServiceFlag = (int) ServiceCategory.AccompaniedHomeFinding;

            _context.Orders.Add(order);
            _context.SaveChanges();
            _context.Entry(order).Reload();

            string url = "http://test.com";
            _mockBookMarkletHelper.Setup(r => r.IsValidUrl(url)).Returns(true);
            _controller.MockCurrentUserAndRole(_dsc.Id, _dsc.UserName, UserRoles.Consultant);

            // Act
            var result = _controller.Index(url) as ViewResult;

            // Assert
            result.Should().NotBeNull();
            result.ViewName.Should().Be(String.Empty);
        }

        // Tests
        [Test, Isolated]
        public void Index_ValidUrlNoOrdersWithHomefinding_ShowErrorView()
        {
            // Arrange
            Order order = OrderBuilder.New().First();
            order.Transferee = _transferee;
            order.ProgramManager = _pm;
            order.Consultant = _dsc;
            _context.Orders.Add(order);
            _context.SaveChanges();
            _context.Entry(order).Reload();

            string url = "http://test.com";
            _mockBookMarkletHelper.Setup(r => r.IsValidUrl(url)).Returns(true);
            _controller.MockCurrentUserAndRole(_dsc.Id, _dsc.UserName, UserRoles.Consultant);

            // Act
            var result = _controller.Index(url) as ViewResult;

            // Assert
            result.Should().NotBeNull();
            result.ViewName.Should().Be("Error");
        }

        [Test, Isolated]
        public void TransfereeIndex_ValidUrlHasOrder_ShowBmViewWithOrder()
        {
            // Arrange
            Order order = OrderBuilder.New().First();
            order.Transferee = _transferee;
            order.ProgramManager = _pm;
            order.Consultant = _dsc;
            order.HomeFinding = new HomeFinding();
            order.ServiceFlag = (int) ServiceCategory.AccompaniedHomeFinding;
            _context.Orders.Add(order);
            _context.SaveChanges();
            _context.Entry(order).Reload();

            string url = "http://test.com";
            _mockBookMarkletHelper.Setup(r => r.IsValidUrl(url)).Returns(true);
            _controller.MockCurrentUserAndRole(_transferee.Id, _transferee.UserName, UserRoles.Transferee);

            // Act
            var result = _controller.Index(url) as ViewResult;

            // Assert
            result.Should().NotBeNull();
            result.ViewName.Should().Be(String.Empty);
        }

        [Test, Isolated]
        public void TransfereeIndex_ValidUrlNoHomeFinding_ShowErrorView()
        {
            // Arrange
            Order order = OrderBuilder.New().First();
            order.Transferee = _transferee;
            order.ProgramManager = _pm;
            order.Consultant = _dsc;
            _context.Orders.Add(order);
            _context.SaveChanges();
            _context.Entry(order).Reload();

            string url = "http://test.com";
            _mockBookMarkletHelper.Setup(r => r.IsValidUrl(url)).Returns(true);
            _controller.MockCurrentUserAndRole(_transferee.Id, _transferee.UserName, UserRoles.Transferee);

            // Act
            var result = _controller.Index(url) as ViewResult;

            // Assert
            result.Should().NotBeNull();
            result.ViewName.Should().Be("Error");
        }

        [Test, Isolated]
        public void Index_ValidUrlHasNoOrders_ShowError()
        {
            // Arrange
            string url = "http://test.com";
            _mockBookMarkletHelper.Setup(r => r.IsValidUrl(url)).Returns(true);
            _controller.MockCurrentUser(_dsc.Id, _dsc.UserName);

            // Act
            var result = _controller.Index(url) as ViewResult;

            // Assert
            result.Should().NotBeNull();
            result.ViewName.Should().Be("Error");
        }

        [Test, Isolated]
        public void TransfereeIndex_ValidUrlHasNoOrders_ShowError()
        {
            // Arrange
            string url = "http://test.com";
            _mockBookMarkletHelper.Setup(r => r.IsValidUrl(url)).Returns(true);
            _controller.MockCurrentUserAndRole(_transferee.Id, _transferee.UserName, UserRoles.Transferee);

            // Act
            var result = _controller.Index(url) as ViewResult;

            // Assert
            result.Should().NotBeNull();
            result.ViewName.Should().Be("Error");
        }

        [Test, Isolated]
        public void Index_InValidUrlHasNoOrders_ShowError()
        {
            // Arrange
            string url = "http://test.com";
            _mockBookMarkletHelper.Setup(r => r.IsValidUrl(url)).Returns(false);
            _controller.MockCurrentUser(_dsc.Id, _dsc.UserName);

            // Act
            var result = _controller.Index(url) as ViewResult;

            // Assert
            result.Should().NotBeNull();
            result.ViewName.Should().Be("Error");
        }

        [Test, Isolated]
        public void TransfereeIndex_InValidUrlHasNoOrders_ShowError()
        {
            // Arrange
            string url = "http://test.com";
            _mockBookMarkletHelper.Setup(r => r.IsValidUrl(url)).Returns(false);
            _controller.MockCurrentUserAndRole(_transferee.Id, _transferee.UserName, UserRoles.Transferee);

            // Act
            var result = _controller.Index(url) as ViewResult;

            // Assert
            result.Should().NotBeNull();
            result.ViewName.Should().Be("Error");
        }

        [Test, Isolated]
        public void Index_InValidUrlHasOrders_ShowError()
        {
            // Arrange
            Order order = OrderBuilder.New().First();
            order.Transferee = _transferee;
            order.ProgramManager = _pm;
            order.Consultant = _dsc;
            _context.Orders.Add(order);
            _context.SaveChanges();
            _context.Entry(order).Reload();

            string url = "http://test.com";
            _mockBookMarkletHelper.Setup(r => r.IsValidUrl(url)).Returns(false);
            _controller.MockCurrentUser(_dsc.Id, _dsc.UserName);

            // Act
            var result = _controller.Index(url) as ViewResult;

            // Assert
            result.Should().NotBeNull();
            result.ViewName.Should().Be("Error");
        }

        [Test, Isolated]
        public void TransfereeIndex_InValidUrlHasOrders_ShowError()
        {
            // Arrange
            Order order = OrderBuilder.New().First();
            order.Transferee = _transferee;
            order.ProgramManager = _pm;
            order.Consultant = _dsc;
            _context.Orders.Add(order);
            _context.SaveChanges();
            _context.Entry(order).Reload();

            string url = "http://test.com";
            _mockBookMarkletHelper.Setup(r => r.IsValidUrl(url)).Returns(false);
            _controller.MockCurrentUserAndRole(_transferee.Id, _transferee.UserName, UserRoles.Transferee);

            // Act
            var result = _controller.Index(url) as ViewResult;

            // Assert
            result.Should().NotBeNull();
            result.ViewName.Should().Be("Error");
        }
    }
}
