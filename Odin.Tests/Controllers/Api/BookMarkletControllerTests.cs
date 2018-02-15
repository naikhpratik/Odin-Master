using AutoMapper;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Odin.Controllers.Api;
using Odin.Data.Core;
using Odin.Data.Core.Dtos;
using Odin.Data.Core.Models;
using Odin.Data.Core.Repositories;
using Odin.Interfaces;
using Odin.Tests.Extensions;
using System.Web.Http;

namespace Odin.Tests.Controllers.Api
{
    [TestClass]
    public class BookMarkletControllerTests
    {
        private BookMarkletController _controller;
        private Mock<IOrdersRepository> _mockRepository;
        private Mock<IMapper> _mockMapper;
        private Mock<IBookMarkletHelper> _mockBookMarkletHelper;
        private Mock<IQueueStore> _mockQueueStore;
        private Mock<IUnitOfWork> _mockUnitOfWork;
        private string _userId;
        private string _userName;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockRepository = new Mock<IOrdersRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();

            _mockQueueStore = new Mock<IQueueStore>();
            _mockUnitOfWork.SetupGet(u => u.Orders).Returns(_mockRepository.Object);
            _mockBookMarkletHelper = new Mock<IBookMarkletHelper>();
            _controller = new BookMarkletController(_mockUnitOfWork.Object, _mockMapper.Object, _mockQueueStore.Object, _mockBookMarkletHelper.Object);

            _userId = "1";
            _userName = "TestUser";
            _controller.MockCurrentUserAndRole(_userId,_userName,UserRoles.Consultant);
        }

        [TestMethod]
        public void Add_ValidDto_ShouldReturnOk()
        {
            var dto = new BookMarkletDto()
            {
                PropertyUrl = "http://test.com",
                OrderId = "1"
            };

            var order = new Order()
            {
                Id = dto.OrderId,
                HomeFinding = new HomeFinding(),
                ServiceFlag =  (int)ServiceCategory.AccompaniedHomeFinding
            };

            _mockRepository.Setup(o => o.GetOrderFor(_userId, dto.OrderId, UserRoles.Consultant)).Returns(order);

            var result = _controller.Add(dto) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.OkResult>();
        }

        [TestMethod]
        public void Add_BadDto_ShouldReturnErrorView()
        {
            var dto = new BookMarkletDto()
            {
                OrderId = "1"
            };

            var result = _controller.Add(dto) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.BadRequestResult>();
        }

        [TestMethod]
        public void Add_NoHomeFindingOnOrder_ShouldReturnNotFound()
        {
            var dto = new BookMarkletDto()
            {
                PropertyUrl = "http://test.com",
                OrderId = "1"
            };

            var order = new Order()
            {
                Id = dto.OrderId
            };

            _mockRepository.Setup(o => o.GetOrderFor(_userId, dto.OrderId, UserRoles.Consultant)).Returns(order);

            var result = _controller.Add(dto) as IHttpActionResult;
            result.Should().BeOfType<System.Web.Http.Results.NotFoundResult>();
        }
    }
}
