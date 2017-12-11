﻿

using System.Collections.Generic;
using AutoMapper;
using FluentAssertions;
using NUnit.Framework;
using Odin.Controllers;
using Odin.Data.Builders;
using Odin.Data.Core.Models;
using Odin.Data.Persistence;
using Odin.IntegrationTests.Extensions;
using Odin.IntegrationTests.TestAttributes;
using Odin.ViewModels.Orders.Transferee;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using Odin.Domain;
using Odin.IntegrationTests.Helpers;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Web.Mvc;
using System.Net;

namespace Odin.IntegrationTests.Controllers
{
    [TestFixture]
    public class HomeFindingPropertiesControllerTests :WebApiBaseTest
    {
        private MapperConfiguration config;
        private IMapper mapper;
        private UnitOfWork unitOfWork;
        private ImageStore imageStore;

        private HomeFindingPropertiesController SetUpHomeFindingPropertiesController()
        {
            config = new MapperConfiguration(c => c.AddProfile(new MappingProfile()));
            mapper = config.CreateMapper();
            unitOfWork = new UnitOfWork(Context);
            imageStore = new ImageStore();
            var controller = new HomeFindingPropertiesController(unitOfWork, mapper, imageStore);
            controller.MockCurrentUser(dsc.Id, dsc.UserName);
            return controller;
        }

        // Tests
        [Test, Isolated]
        public async Task InsertProperty_ValidInsertRequest_CreatesRecords()
        {
            // Arrange
            Order order = BuildOrder();
            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();

            HousingPropertyViewModel propertyVM = new HousingPropertyViewModel();
            propertyVM.PropertyStreet1 = "abc";
            propertyVM.PropertyStreet2 = "apt 123";
            propertyVM.OrderId = order.Id;

            // Act
            HomeFindingPropertiesController controller = SetUpHomeFindingPropertiesController();
            controller.Create(propertyVM);

            // Assert
            Context.Entry(order).Reload();
            order.HomeFinding.HomeFindingProperties.Count().Should().Be(1);

            HomeFindingProperty hfp = order.HomeFinding.HomeFindingProperties.First();
            Property property = hfp.Property;
            property.Street1.Should().Be(propertyVM.PropertyStreet1);
            property.Street2.Should().Be(propertyVM.PropertyStreet2);
        }

        [Test, Isolated]
        public async Task InsertProperty_WithPhotos_AddsPhotosToBlobStorage()
        {
            // Arrange
            Order order = BuildOrder();
            Context.Orders.Add(order);
            Context.SaveChanges();
            Context.Entry(order).Reload();

            HousingPropertyViewModel propertyVM = new HousingPropertyViewModel();
            propertyVM.PropertyStreet1 = "abc";
            propertyVM.PropertyStreet2 = "apt 123";
            propertyVM.OrderId = order.Id;
            var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("Odin.IntegrationTests.Resources.odin_login.png");
            var postedFile = new HttpPostedFileBaseTesting(stream, "image/png", "odin_login.png");
            var postedFile2 = new HttpPostedFileBaseTesting(stream, "image/png", "odin_login.png");
            propertyVM.UploadedPhotos = new List<HttpPostedFileBase> { postedFile, postedFile2 };

            // Act
            HomeFindingPropertiesController controller = SetUpHomeFindingPropertiesController();
            controller.Create(propertyVM);

            // Assert
            Context.Entry(order).Reload();
            HomeFindingProperty hfp = order.HomeFinding.HomeFindingProperties.First();
            Property property = hfp.Property;

            property.Photos.Count().Should().Be(2);

            Photo propertyPhoto = property.Photos.First();
            ICloudBlob imageReference = imageStore.ImageBlobFor(propertyPhoto.StorageId);

            imageReference.Should().NotBeNull();

            // Cleanup so we don't flood the azure container
            foreach(Photo p in property.Photos)
            {
                ICloudBlob imageBlob = imageStore.ImageBlobFor(p.StorageId);
                imageBlob.Delete();
            }
        }


        [Test, Isolated]
        public void DeleteHomeFindingProperty_SoftDeletesTheRecord()
        {
            // Arrange
            Order order = BuildOrder(false);
            Context.Orders.Add(order);
            Context.SaveChanges();

            // Act
            HomeFindingPropertiesController controller = SetUpHomeFindingPropertiesController();

            HomeFindingProperty hfp = order.HomeFinding.HomeFindingProperties.First();
            HttpStatusCodeResult response = (HttpStatusCodeResult)controller.Delete(hfp.Id);
            Context.Entry(order).Reload();

            // Assert
            // Soft delete does not remove the association
            order.HomeFinding.HomeFindingProperties.Count().Should().Be(1);

            HomeFindingProperty loadedHfp = order.HomeFinding.HomeFindingProperties.First();
            loadedHfp.Deleted.Should().BeTrue();

            HttpStatusCodeResult expectedCode = new HttpStatusCodeResult(HttpStatusCode.NoContent);
            response.StatusCode.Should().Be(expectedCode.StatusCode);
        }

        [Test, Isolated]
        public void DeleteHomeFindingProperty_ReturnsRecordNotFoundWithBadId()
        {
            HttpStatusCodeResult expectedCode = new HttpStatusCodeResult(HttpStatusCode.NotFound);
            HomeFindingPropertiesController controller = SetUpHomeFindingPropertiesController();

            HttpStatusCodeResult response = (HttpStatusCodeResult)controller.Delete("SomeNonExisitingRecordId");
            response.StatusCode.Should().Be(expectedCode.StatusCode);
        }

        [Test, Isolated]
        public void UpdateHomeFindingProperty_UpdatesLiked()
        {
            // Arrange
            Order order = BuildOrder(false);
            Context.Orders.Add(order);
            HomeFindingProperty hfp = order.HomeFinding.HomeFindingProperties.First();
            hfp.Liked = null; // ensure this isn't already liked
            Context.SaveChanges();

            // Act
            HousingPropertyViewModel propertyVM = new HousingPropertyViewModel();
            propertyVM.Id = hfp.Id;
            propertyVM.Liked = true;

            HomeFindingPropertiesController controller = SetUpHomeFindingPropertiesController();
            HttpStatusCodeResult response = (HttpStatusCodeResult)controller.Update(propertyVM);

            // Assert
            Context.Entry(hfp).Reload();
            hfp.Liked.Should().BeTrue();
        }

        [Test, Isolated]
        public void UpdateHomeFindingProperty_UpdatesDisliked()
        {
            // Arrange
            Order order = BuildOrder(false);
            Context.Orders.Add(order);
            HomeFindingProperty hfp = order.HomeFinding.HomeFindingProperties.First();
            hfp.Liked = null; // ensure this isn't already disliked
            Context.SaveChanges();

            // Act
            HousingPropertyViewModel propertyVM = new HousingPropertyViewModel();
            propertyVM.Id = hfp.Id;
            propertyVM.Liked = false;

            HomeFindingPropertiesController controller = SetUpHomeFindingPropertiesController();
            HttpStatusCodeResult response = (HttpStatusCodeResult)controller.Update(propertyVM);

            // Assert
            Context.Entry(hfp).Reload();
            hfp.Liked.Should().BeFalse();
        }

        [Test, Isolated]
        public void UpdateHomeFindingProperty_UpdatesNull()
        {
            // Arrange
            Order order = BuildOrder(false);
            Context.Orders.Add(order);
            HomeFindingProperty hfp = order.HomeFinding.HomeFindingProperties.First();
            hfp.Liked = true; // ensure this is already liked
            Context.SaveChanges();

            // Act
            HousingPropertyViewModel propertyVM = new HousingPropertyViewModel();
            propertyVM.Id = hfp.Id;
            propertyVM.Liked = null;

            HomeFindingPropertiesController controller = SetUpHomeFindingPropertiesController();
            HttpStatusCodeResult response = (HttpStatusCodeResult)controller.Update(propertyVM);

            // Assert
            Context.Entry(hfp).Reload();
            hfp.Liked.Should().BeNull();
        }

        [Test, Isolated]
        public void UpdateHomeFindingProperty_DoesNotWipeOutOtherDataFromIncompleteViewModel()
        {
            // Arrange
            Order order = BuildOrder(false);
            Context.Orders.Add(order);
            HomeFindingProperty hfp = order.HomeFinding.HomeFindingProperties.First();
            Context.SaveChanges();

            // Act
            var expectedStreet = hfp.Property.Street1;
            HousingPropertyViewModel propertyVM = new HousingPropertyViewModel();
            propertyVM.Id = hfp.Id;
            propertyVM.Liked = null;
            // This ViewModel doesn't have anything for the property meaning nothing gets updated
            // Note: This is more of a test of the Mapper instead of the controller

            HomeFindingPropertiesController controller = SetUpHomeFindingPropertiesController();
            HttpStatusCodeResult response = (HttpStatusCodeResult)controller.Update(propertyVM);

            // Assert
            Context.Entry(hfp).Reload();
            hfp.Property.Street1.Should().Be(expectedStreet);
        }

        /*-------------------------------------------------*/
        private Order BuildOrder(bool emptyProperties=true)
        {
            Order order = OrderBuilder.New().First();
            order.Transferee = transferee;
            order.ProgramManager = pm;
            order.Consultant = dsc;

            HomeFinding homeFinding = HomeFindingBuilder.New();

            if (emptyProperties)
            {
                // The builder makes a single property, but we want it empty
                homeFinding.HomeFindingProperties = new Collection<HomeFindingProperty>();
            }

            order.HomeFinding = homeFinding;

            return order;
        }
    }
}
