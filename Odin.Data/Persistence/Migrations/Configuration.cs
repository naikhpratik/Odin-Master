using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Azure.Mobile.Server.Tables;
using Odin.Data.Builders;
using Odin.Data.Core.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;

namespace Odin.Data.Persistence.Migrations
{
    public sealed class Configuration : DbMigrationsConfiguration<ApplicationDbContext>
    {
        private static readonly string _odinConsultantUserName = "odinconsultant@dwellworks.com";
        private static readonly string _odinGscUserName = "odingsc@dwellworks.com";
        private static readonly string _odinPmUserName = "odinpm@dwellworks.com";
        private static readonly string _odinEeUserName = "odinee@dwellworks.com";
        private static readonly string _odinAdminUserName = "odinadmin@dwellworks.com";

        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            MigrationsDirectory = @"Persistence\Migrations";
            SetSqlGenerator("System.Data.SqlClient", new EntityTableSqlGenerator());
        }

        protected override void Seed(ApplicationDbContext context)
        {
            //Note for Seeding types:  byte primary keys can be specified during insert.
            //int primary keys can't; no identity insert, just autoincrementing.
            SeedNumberOfBathrooms(context);
            SeedHousingTypes(context);
            SeedAreaTypes(context);
            SeedTransportationTypes(context);
            SeedDepositTypes(context);
            SeedBrokerFeeTypes(context);
            SeedServiceTypes(context);

            SeedInitialRoles(context);
            SeedInitialUsers(context);
            CreateOrderData(context);
            PopulateSeContactUids(context);
            SeedHomeFindingPropertiesIfNone(context);
            CreateOrderWithTransferee(context);
            SeedHomeFindingServiceFlag(context);
        }

        private void PopulateSeContactUids(ApplicationDbContext context)
        {

            var users = context.Users.Where(u => !u.SeContactUid.HasValue);
            if (users.Any())
            {
                var maxSeContactUid = context.Users.OrderByDescending(u => u.SeContactUid).FirstOrDefault()?.SeContactUid ?? 1;
                foreach (var user in users)
                {
                    user.SeContactUid = ++maxSeContactUid;
                }
                context.SaveChanges();
            }
        }

        private void CreateOrderData(ApplicationDbContext context)
        {
            if (!context.Orders.Any())
            {
                var dsc = context.Consultants.First(u => u.UserName.Equals(_odinConsultantUserName));
                var pm = context.Managers.First(u => u.UserName.Equals(_odinPmUserName));

                int count = 10;
                var orders = OrderBuilder.New(count);

                for (int i = 0; i < count; i++)
                {
                    orders[i].Consultant = dsc;
                    orders[i].ProgramManager = pm;
                    orders[i].Transferee = CreateRandomTransferee(context);
                    orders[i].Services.Add(new Service()
                    {
                        ServiceType = context.ServiceTypes.First(),
                        Selected = true
                    });
                    orders[i].Services.Add(new Service()
                    {
                        ServiceType = context.ServiceTypes.OrderByDescending(st => st.Id).First(),
                        Selected = true
                    });
                    context.Orders.Add(orders[i]);

                    var homeFinding = HomeFindingBuilder.New();
                    homeFinding.Id = orders[i].Id;
                    context.HomeFindings.Add(homeFinding);

                    orders[i].Children.Add(ChildBuilder.New());
                    orders[i].Children.Add(ChildBuilder.New());
                    orders[i].Pets.Add(PetBuilder.New());
                    orders[i].Pets.Add(PetBuilder.New());
                }

                context.SaveChanges();
            }
        }

        private void CreateOrderWithTransferee(ApplicationDbContext context)
        {
            if (!context.Orders.Any(o => o.Transferee.Email == "eeWithOrder@dwellworks.com"))
            {
                var dsc = context.Consultants.First(u => u.UserName.Equals(_odinConsultantUserName));
                var pm = context.Managers.First(u => u.UserName.Equals(_odinPmUserName));

                int count = 1;
                var orders = OrderBuilder.New(count);
               
                var userStore = new UserStore<Transferee>(context);
                var userManager = new UserManager<Transferee>(userStore);

                Transferee user;
                if (!context.Users.Any(u => u.Email == "eeWithOrder@dwellworks.com"))
                {
                    user = TransfereeBuilder.New();
                    user.Email = "eeWithOrder@dwellworks.com";
                    user.UserName = "eeWithOrder@dwellworks.com";
                    userManager.Create(user, "OdinOdin5$");
                    var transfereeStore = new UserStore<Transferee>(context);
                    var transfereeManager = new UserManager<Transferee>(transfereeStore);
                    transfereeManager.AddToRole(user.Id, UserRoles.Transferee);
                }
                else
                {
                    user = context.Transferees.Single<Transferee>(t => t.Email == "eeWithOrder@dwellworks.com");
                }

                orders[0].TrackingId = Guid.NewGuid().ToString().Substring(0,20);
                orders[0].Transferee = user;
                orders[0].Consultant = dsc;
                orders[0].ProgramManager = pm;
                orders[0].Services.Add(new Service()
                {
                    ServiceType = context.ServiceTypes.First(),
                    Selected = true
                });
                orders[0].Services.Add(new Service()
                {
                    ServiceType = context.ServiceTypes.OrderByDescending(st => st.Id).First(),
                    Selected = true
                });
                context.Orders.Add(orders[0]);

                var homeFinding = HomeFindingBuilder.New();
                homeFinding.Id = orders[0].Id;
                context.HomeFindings.Add(homeFinding);

                orders[0].Children.Add(ChildBuilder.New());
                orders[0].Children.Add(ChildBuilder.New());
                orders[0].Pets.Add(PetBuilder.New());
                orders[0].Pets.Add(PetBuilder.New());              
               
            }
        }

        private Transferee CreateRandomTransferee(ApplicationDbContext context)
        {
            var userStore = new UserStore<Transferee>(context);
            var userManager = new UserManager<Transferee>(userStore);

            var user = TransfereeBuilder.New();
            userManager.Create(user, "Transferee5$");
            return user;
        }

        private void SeedInitialUsers(ApplicationDbContext context)
        {
            var userStore = new UserStore<ApplicationUser>(context);
            var userManager = new UserManager<ApplicationUser>(userStore);


            var adminUser = userManager.FindByName(_odinAdminUserName);
            if (adminUser == null)
            {
                var newAdmin = new ApplicationUser()
                {
                    UserName = _odinAdminUserName,
                    FirstName = "Odin",
                    LastName = "Admin",
                    Email = _odinAdminUserName,
                    PhoneNumber = "4403184188"
                };
                var result = userManager.Create(newAdmin, "OdinOdin5$");
                userManager.AddToRole(newAdmin.Id, UserRoles.Admin);
            }

            var consultantStore = new UserStore<Consultant>(context);
            var consultantManager = new UserManager<Consultant>(consultantStore);

            var consultantUser = consultantManager.FindByName(_odinConsultantUserName);
            if (consultantUser == null)
            {
                var newConsultant = new Consultant()
                {
                    UserName = _odinConsultantUserName,
                    FirstName = "Odin",
                    LastName = "Consultant",
                    Email = _odinConsultantUserName,
                    PhoneNumber = "4403184188"
                };
                var result = consultantManager.Create(newConsultant, "OdinOdin5$");
                consultantManager.AddToRole(newConsultant.Id, UserRoles.Consultant);
            }


            var managerStore = new UserStore<Manager>(context);
            var managerManager = new UserManager<Manager>(managerStore);

            var gscUser = managerManager.FindByName(_odinGscUserName);
            if (gscUser == null)
            {
                var newGsc = new Manager()
                {
                    UserName = _odinGscUserName,
                    FirstName = "George",
                    LastName = "Gsc",
                    Email = _odinGscUserName,
                    PhoneNumber = "2166824239"
                };
                managerManager.Create(newGsc, "OdinOdin5$");
                managerManager.AddToRole(newGsc.Id, UserRoles.GlobalSupplyChain);
            }



            var pmUser = managerManager.FindByName(_odinPmUserName);
            if (pmUser == null)
            {
                var newPm = new Manager()
                {
                    UserName = _odinPmUserName,
                    FirstName = "Odin",
                    LastName = "Pm",
                    Email = _odinPmUserName,
                    PhoneNumber = "2166824239"
                };
                managerManager.Create(newPm, "OdinOdin5$");
                managerManager.AddToRole(newPm.Id, UserRoles.ProgramManager);
            }

            var transfereeStore = new UserStore<Transferee>(context);
            var transfereeManager = new UserManager<Transferee>(transfereeStore);

            var eeUser = transfereeManager.FindByName(_odinEeUserName);
            if (eeUser == null)
            {
                var newEe = new Transferee()
                {
                    UserName = _odinEeUserName,
                    Email = _odinEeUserName,
                    FirstName = "Odin",
                    LastName = "Ee",
                    PhoneNumber = "2166824239"
                };
                transfereeManager.Create(newEe, "OdinOdin5$");
                transfereeManager.AddToRole(newEe.Id, UserRoles.Transferee);
            }
        }

        private void SeedInitialRoles(ApplicationDbContext context)
        {
            var roleStore = new RoleStore<IdentityRole>(context);
            var roleManager = new RoleManager<IdentityRole>(roleStore);

            var consultantRole = roleManager.FindByName(UserRoles.Consultant);
            if (consultantRole == null)
            {
                consultantRole = new IdentityRole(UserRoles.Consultant);
                roleManager.Create(consultantRole);
            }

            var gscRole = roleManager.FindByName(UserRoles.GlobalSupplyChain);
            if (gscRole == null)
            {
                gscRole = new IdentityRole(UserRoles.GlobalSupplyChain);
                roleManager.Create(gscRole);
            }

            var eeRole = roleManager.FindByName(UserRoles.Transferee);
            if (eeRole == null)
            {
                eeRole = new IdentityRole(UserRoles.Transferee);
                roleManager.Create(eeRole);
            }

            var pmRole = roleManager.FindByName(UserRoles.ProgramManager);
            if (pmRole == null)
            {
                pmRole = new IdentityRole(UserRoles.ProgramManager);
                roleManager.Create(pmRole);
            }

            var adminRole = roleManager.FindByName(UserRoles.Admin);
            if (adminRole == null)
            {
                adminRole = new IdentityRole(UserRoles.Admin);
                roleManager.Create(adminRole);
            }
        }

        private void SeedNumberOfBathrooms(ApplicationDbContext context)
        {

            if (!context.NumberOfBathrooms.Any(n => n.Id == 1))
            {
                context.NumberOfBathrooms.Add(new NumberOfBathroomsType() { Id = 1, Name = "0", SeValue = "0" });
            }

            if (!context.NumberOfBathrooms.Any(n => n.Id == 2))
            {
                context.NumberOfBathrooms.Add(new NumberOfBathroomsType() { Id = 2, Name = "1/2", SeValue = "1/2" });
            }

            if (!context.NumberOfBathrooms.Any(n => n.Id == 3))
            {
                context.NumberOfBathrooms.Add(new NumberOfBathroomsType() { Id = 3, Name = "1", SeValue = "1" });
            }

            if (!context.NumberOfBathrooms.Any(n => n.Id == 4))
            {
                context.NumberOfBathrooms.Add(new NumberOfBathroomsType() { Id = 4, Name = "1 1/2", SeValue = "1 1/2" });
            }

            if (!context.NumberOfBathrooms.Any(n => n.Id == 5))
            {
                context.NumberOfBathrooms.Add(new NumberOfBathroomsType() { Id = 5, Name = "2", SeValue = "2" });
            }

            if (!context.NumberOfBathrooms.Any(n => n.Id == 6))
            {
                context.NumberOfBathrooms.Add(new NumberOfBathroomsType() { Id = 6, Name = "2 1/2", SeValue = "2 1/2" });
            }

            if (!context.NumberOfBathrooms.Any(n => n.Id == 7))
            {
                context.NumberOfBathrooms.Add(new NumberOfBathroomsType() { Id = 7, Name = "3", SeValue = "3" });
            }

            if (!context.NumberOfBathrooms.Any(n => n.Id == 8))
            {
                context.NumberOfBathrooms.Add(new NumberOfBathroomsType() { Id = 8, Name = "3 1/2", SeValue = "3 1/2" });
            }

            if (!context.NumberOfBathrooms.Any(n => n.Id == 9))
            {
                context.NumberOfBathrooms.Add(new NumberOfBathroomsType() { Id = 9, Name = "4", SeValue = "4" });
            }

            if (!context.NumberOfBathrooms.Any(n => n.Id == 10))
            {
                context.NumberOfBathrooms.Add(new NumberOfBathroomsType() { Id = 10, Name = "4 1/2", SeValue = "4 1/2" });
            }

            if (!context.NumberOfBathrooms.Any(n => n.Id == 11))
            {
                context.NumberOfBathrooms.Add(new NumberOfBathroomsType() { Id = 11, Name = "5", SeValue = "5" });
            }

            if (!context.NumberOfBathrooms.Any(n => n.Id == 12))
            {
                context.NumberOfBathrooms.Add(new NumberOfBathroomsType() { Id = 12, Name = "5 1/2", SeValue = "5 1/2" });
            }

            if (!context.NumberOfBathrooms.Any(n => n.Id == 13))
            {
                context.NumberOfBathrooms.Add(new NumberOfBathroomsType() { Id = 13, Name = "5+", SeValue = "5+" });
            }

            context.SaveChanges();
        }

        private void SeedHousingTypes(ApplicationDbContext context)
        {

            if (!context.HousingTypes.Any(h => h.Id == 1))
            {
                context.HousingTypes.Add(new HousingType() { Id = 1, Name = "House", SeValue = "HOUSE" });
            }

            if (!context.HousingTypes.Any(h => h.Id == 2))
            {
                context.HousingTypes.Add(new HousingType() { Id = 2, Name = "Town House", SeValue = "TOWNRH" });
            }

            if (!context.HousingTypes.Any(h => h.Id == 3))
            {
                context.HousingTypes.Add(new HousingType() { Id = 3, Name = "Apartment", SeValue = "APARTM" });
            }

            if (!context.HousingTypes.Any(h => h.Id == 4))
            {
                context.HousingTypes.Add(new HousingType() { Id = 4, Name = "Condo", SeValue = "CONDOM" });
            }

            context.SaveChanges();
        }

        private void SeedAreaTypes(ApplicationDbContext context)
        {

            if (!context.AreaTypes.Any(a => a.Id == 1))
            {
                context.AreaTypes.Add(new AreaType() { Id = 1, Name = "Suburban" });
            }

            if (!context.AreaTypes.Any(a => a.Id == 2))
            {
                context.AreaTypes.Add(new AreaType() { Id = 2, Name = "Urban" });
            }

            if (!context.AreaTypes.Any(a => a.Id == 3))
            {
                context.AreaTypes.Add(new AreaType() { Id = 3, Name = "Rural" });
            }

            context.SaveChanges();
        }

        private void SeedTransportationTypes(ApplicationDbContext context)
        {
            if (!context.TransportationTypes.Any(t => t.Id == 1))
            {
                context.TransportationTypes.Add(new TransportationType() { Id = 1, Name = "Automobile", SeValue = "AUTOMO" });
            }

            if (!context.TransportationTypes.Any(t => t.Id == 2))
            {
                context.TransportationTypes.Add(new TransportationType() { Id = 2, Name = "Public Transportation", SeValue = "PUBLIC" });
            }

            context.SaveChanges();
        }

        private void SeedDepositTypes(ApplicationDbContext context)
        {
            if (!context.DepositTypes.Any(d => d.Id == 1))
            {
                context.DepositTypes.Add(new DepositType() { Id = 1, Name = "Individual", SeValue = "INDIVI" });
            }

            if (!context.DepositTypes.Any(d => d.Id == 2))
            {
                context.DepositTypes.Add(new DepositType() { Id = 2, Name = "Company", SeValue = "COMPAN" });
            }

            context.SaveChanges();
        }

        private void SeedBrokerFeeTypes(ApplicationDbContext context)
        {
            if (!context.BrokerFeeTypes.Any(b => b.Id == 1))
            {
                context.BrokerFeeTypes.Add(new BrokerFeeType() { Id = 1, Name = "Individual", SeValue = "INDIVI" });
            }

            if (!context.BrokerFeeTypes.Any(b => b.Id == 2))
            {
                context.BrokerFeeTypes.Add(new BrokerFeeType() { Id = 2, Name = "Company", SeValue = "COMPAN" });
            }

            context.SaveChanges();
        }

        private void SeedServiceTypes(ApplicationDbContext context)
        {
            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Initial/Pre-Arrival Consultation".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Initial/Pre-Arrival Consultation",
                    Category = ServiceCategory.InitialConsultation,
                    SortOrder = 1,
                    Default = (int)DefaultType.Domestic + (int)DefaultType.International,
                    ActionLabel = "Initial/Pre-Arrival Consultation"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Initial/Pre-Arrival Consultation".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 1;
                    type.Default = (int)DefaultType.Domestic + (int)DefaultType.International;

                }
                type.ActionLabel = "Initial/Pre-Arrival Consultation";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Welcome Packet".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Welcome Packet",
                    Category = ServiceCategory.WelcomePacket,
                    SortOrder = 2,
                    Default = (int)DefaultType.Domestic + (int)DefaultType.International,
                    ActionLabel = "Receive Welcome Packet"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Welcome Packet".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 2;
                    type.Default = (int)DefaultType.Domestic + (int)DefaultType.International;

                }
                type.ActionLabel = "Receive Welcome Packet";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Social Security Registration".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Social Security Registration",
                    Category = ServiceCategory.SettlingIn,
                    SortOrder = 5,
                    Default = (int)DefaultType.International,
                    ActionLabel = "Complete Social Security Registration"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Social Security Registration".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 5;
                    type.Default = (int)DefaultType.International;

                }
                type.ActionLabel = "Complete Social Security Registration";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Colleges/Universities".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Colleges/Universities",
                    Category = ServiceCategory.SettlingIn,
                    SortOrder = 19,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Explore Colleges/Universities"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Colleges/Universities".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 19;
                    type.Default = (int)DefaultType.No;

                }
                type.ActionLabel = "Explore Colleges/Universities";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Language Assistance".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Language Assistance",
                    Category = ServiceCategory.SettlingIn,
                    SortOrder = 20,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Review Language Assistance Options"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Language Assistance".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 20;
                    type.Default = (int)DefaultType.No;

                }
                type.ActionLabel = "Review Language Assistance Options";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Telephone Systems".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Telephone Systems",
                    Category = ServiceCategory.SettlingIn,
                    SortOrder = 21,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Compare Telephone Service Providers"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Telephone Systems".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 21;
                    type.Default = (int)DefaultType.No;

                }
                type.ActionLabel = "Compare Telephone Service Providers";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Utility Hook-up".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Utility Hook-up",
                    Category = ServiceCategory.SettlingIn,
                    SortOrder = 22,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Review Utility Setup Protocol"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Utility Hook-up".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 22;
                    type.Default = (int)DefaultType.No;

                }
                type.ActionLabel = "Review Utility Setup Protocol";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Internet Service Providers".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Internet Service Providers",
                    Category = ServiceCategory.SettlingIn,
                    SortOrder = 23,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Review Internet Service Providers"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Internet Service Providers".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 23;
                    type.Default = (int)DefaultType.No;

                }
                type.ActionLabel = "Review Internet Service Providers";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Furniture Rental".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Furniture Rental",
                    Category = ServiceCategory.SettlingIn,
                    SortOrder = 24,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Review Furniture Rental Options"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Furniture Rental".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 24;
                    type.Default = (int)DefaultType.No;
                }
                type.ActionLabel = "Review Furniture Rental Options";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Furniture Purchase".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Furniture Purchase",
                    Category = ServiceCategory.SettlingIn,
                    SortOrder = 25,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Purchase Furniture"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Furniture Purchase".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 25;
                    type.Default = (int)DefaultType.No;
                }
                type.ActionLabel = "Purchase Furniture";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Appliances(Compatibility / Purchase)".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Appliances(Compatibility / Purchase)",
                    Category = ServiceCategory.SettlingIn,
                    SortOrder = 26,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Discuss or Purchase Appliances"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Appliances(Compatibility / Purchase)".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 26;
                    type.Default = (int)DefaultType.No;
                }
                type.ActionLabel = "Discuss or Purchase Appliances";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Driving/Auto Info (Licensing / Driving Schools)".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Driving/Auto Info (Licensing / Driving Schools)",
                    Category = ServiceCategory.SettlingIn,
                    SortOrder = 7,
                    Default = (int)DefaultType.International,
                    ActionLabel = "Review Driving Information (license, driving schools, vehicle purchase, etc.)"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Driving/Auto Info (Licensing / Driving Schools)".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 7;
                    type.Default = (int)DefaultType.International;
                }
                type.ActionLabel = "Review Driving Information (license, driving schools, vehicle purchase, etc.)";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Transportation Options".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Transportation Options",
                    Category = ServiceCategory.SettlingIn,
                    SortOrder = 27,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Review Transportation Options"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Transportation Options".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 27;
                    type.Default = (int)DefaultType.No;
                }
                type.ActionLabel = "Review Transportation Options";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Homeowner's Services".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Homeowner's Services",
                    Category = ServiceCategory.SettlingIn,
                    SortOrder = 28,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Discuss Homeowner's Services"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Homeowner's Services".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 28;
                    type.Default = (int)DefaultType.No;
                }
                type.ActionLabel = "Discuss Homeowner's Services";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Insurance (Auto / Renter / Homeowner)".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Insurance (Auto / Renter / Homeowner)",
                    Category = ServiceCategory.SettlingIn,
                    SortOrder = 29,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Review Insurance Providers (auto, home, etc.)"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Insurance (Auto / Renter / Homeowner)".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 29;
                    type.Default = (int)DefaultType.No;
                }
                type.ActionLabel = "Review Insurance Providers (auto, home, etc.)";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Medical/Dental Information".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Medical/Dental Information",
                    Category = ServiceCategory.SettlingIn,
                    SortOrder = 30,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Receive Medical and Dental Information"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Medical/Dental Information".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 30;
                    type.Default = (int)DefaultType.No;
                }
                type.ActionLabel = "Receive Medical and Dental Information";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Money Issues/Banking".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Money Issues/Banking",
                    Category = ServiceCategory.SettlingIn,
                    SortOrder = 6,
                    Default = (int)DefaultType.International,
                    ActionLabel = "Review Banking Services and Money Issues"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Money Issues/Banking".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 6;
                    type.Default = (int)DefaultType.International;
                }
                type.ActionLabel = "Review Banking Services and Money Issues";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Mail Services".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Mail Services",
                    Category = ServiceCategory.SettlingIn,
                    SortOrder = 31,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Find Mail Services"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Mail Services".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 31;
                    type.Default = (int)DefaultType.No;
                }
                type.ActionLabel = "Find Mail Services";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Domestic Services(Maid / Cook / Driver)".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Domestic Services(Maid / Cook / Driver)",
                    Category = ServiceCategory.SettlingIn,
                    SortOrder = 32,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Hire Domestic Services (cleaning services, household staff, etc. )"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Domestic Services(Maid / Cook / Driver)".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 32;
                    type.Default = (int)DefaultType.No;
                }
                type.ActionLabel = "Hire Domestic Services (cleaning services, household staff, etc. )";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Pets (Care / Licensing / etc...)".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Pets (Care / Licensing / etc...)",
                    Category = ServiceCategory.SettlingIn,
                    SortOrder = 33,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Review Pet Support (vetrinary services, licensing, etc.)"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Pets (Care / Licensing / etc...)".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 33;
                    type.Default = (int)DefaultType.No;
                }
                type.ActionLabel = "Review Pet Support (vetrinary services, licensing, etc.)";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Childcare".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Childcare",
                    Category = ServiceCategory.SettlingIn,
                    SortOrder = 34,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Find Childcare"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Childcare".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 34;
                    type.Default = (int)DefaultType.No;
                }
                type.ActionLabel = "Find Childcare";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Eldercare".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Eldercare",
                    Category = ServiceCategory.SettlingIn,
                    SortOrder = 35,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Find Eldercare"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Eldercare".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 35;
                    type.Default = (int)DefaultType.No;
                }
                type.ActionLabel = "Find Eldercare";
            }

            /*Duplicate Fix*/
            if (context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Area Orientation/Overview".ToUpper()))
            {
                List<ServiceType> types = context.ServiceTypes.Where(s => s.Name.Trim().ToUpper() == "Area Orientation/Overview".ToUpper()).ToList();
                foreach (var type in types)
                {
                    context.ServiceTypes.Remove(type);
                }
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Area Orientation / Overview".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Area Orientation / Overview",
                    Category = ServiceCategory.AreaOrientation,
                    SortOrder = 3,
                    Default = (int)DefaultType.Domestic + (int)DefaultType.International,
                    ActionLabel = "Discuss Area Orientation/Overview"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Area Orientation / Overview".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 3;
                    type.Default = (int)DefaultType.Domestic + (int)DefaultType.International;
                }
                type.ActionLabel = "Discuss Area Orientation/Overview";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Housing / Neighborhoods".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Housing / Neighborhoods",
                    Category = ServiceCategory.AreaOrientation,
                    SortOrder = 8,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Research and Visit Housing/Neighborhoods"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Housing / Neighborhoods".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 8;
                    type.Default = (int)DefaultType.No;
                }
                type.ActionLabel = "Research and Visit Housing/Neighborhoods";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Religious Worship".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Religious Worship",
                    Category = ServiceCategory.AreaOrientation,
                    SortOrder = 9,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Find Places for Religious Worship"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Religious Worship".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 9;
                    type.Default = (int)DefaultType.No;
                }
                type.ActionLabel = "Find Places for Religious Worship";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Shopping Information".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Shopping Information",
                    Category = ServiceCategory.AreaOrientation,
                    SortOrder = 10,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Review Shopping Information"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Shopping Information".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 10;
                    type.Default = (int)DefaultType.No;
                }
                type.ActionLabel = "Review Shopping Information";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Restaurants".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Restaurants",
                    Category = ServiceCategory.AreaOrientation,
                    SortOrder = 11,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Review Restaurant Information"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Restaurants".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 11;
                    type.Default = (int)DefaultType.No;
                }
                type.ActionLabel = "Review Restaurant Information";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Arts and Leisure Facilities".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Arts and Leisure Facilities",
                    Category = ServiceCategory.AreaOrientation,
                    SortOrder = 12,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Find Local Arts and Leisure Facilities"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Arts and Leisure Facilities".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 12;
                    type.Default = (int)DefaultType.No;
                }
                type.ActionLabel = "Find Local Arts and Leisure Facilities";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Clubs (Social / Health / Recreational)".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Clubs (Social / Health / Recreational)",
                    Category = ServiceCategory.AreaOrientation,
                    SortOrder = 13,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Find Local Clubs (social, health, recreational)"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Clubs (Social / Health / Recreational)".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 13;
                    type.Default = (int)DefaultType.No;
                }
                type.ActionLabel = "Find Local Clubs (social, health, recreational)";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Sports Information".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Sports Information",
                    Category = ServiceCategory.AreaOrientation,
                    SortOrder = 14,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Research Sports"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Sports Information".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 14;
                    type.Default = (int)DefaultType.No;
                }
                type.ActionLabel = "Research Sports";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Volunteer Associations".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Volunteer Associations",
                    Category = ServiceCategory.AreaOrientation,
                    SortOrder = 15,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Research Associations and Volunteer Opportunities"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Volunteer Associations".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 15;
                    type.Default = (int)DefaultType.No;
                }
                type.ActionLabel = "Research Associations and Volunteer Opportunities";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Library".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Library",
                    Category = ServiceCategory.AreaOrientation,
                    SortOrder = 16,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Find Local Library"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Library".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 16;
                    type.Default = (int)DefaultType.No;
                }
                type.ActionLabel = "Find Local Library";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Emergency / Police / Fire".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Emergency / Police / Fire",
                    Category = ServiceCategory.AreaOrientation,
                    SortOrder = 17,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Review Local Emergency Services (police, fire, etc.)"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Emergency / Police / Fire".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 17;
                    type.Default = (int)DefaultType.No;
                }
                type.ActionLabel = "Review Local Emergency Services (police, fire, etc.)";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Local / Regional / Government Holidays".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Local / Regional / Government Holidays",
                    Category = ServiceCategory.AreaOrientation,
                    SortOrder = 18,
                    Default = (int)DefaultType.No,
                    ActionLabel = "Note Local/Regional/Government Holidays"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Local / Regional / Government Holidays".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 18;
                    type.Default = (int)DefaultType.No;
                }
                type.ActionLabel = "Note Local/Regional/Government Holidays";
            }

            if (!context.ServiceTypes.Any(s => s.Name.Trim().ToUpper() == "Settling In / Overview".ToUpper()))
            {
                context.ServiceTypes.Add(new ServiceType()
                {
                    Name = "Settling In / Overview",
                    Category = ServiceCategory.SettlingIn,
                    SortOrder = 4,
                    Default = (int)DefaultType.Domestic + (int)DefaultType.International,
                    ActionLabel = "Review Settling-In/Overview"
                });
            }
            else
            {
                ServiceType type = context.ServiceTypes.SingleOrDefault(s => s.Name.Trim().ToUpper() == "Settling In / Overview".ToUpper());
                if (type.SortOrder == 0)
                {
                    type.SortOrder = 4;
                    type.Default = (int)DefaultType.Domestic + (int)DefaultType.International;
                }
                type.ActionLabel = "Review Settling-In/Overview";
            }

            context.SaveChanges();
        }

        private void SeedHomeFindingPropertiesIfNone(ApplicationDbContext context)
        {
            if (!context.HomeFindingProperties.Any())
            {
                DbSet<HomeFinding> homeFindings = context.HomeFindings;
                foreach (HomeFinding hf in homeFindings)
                {
                    hf.HomeFindingProperties = HomeFindingPropertiesBuilder.New();
                }

                context.SaveChanges();
            }
        }

        private void SeedHomeFindingServiceFlag(ApplicationDbContext context)
        {
            //Get the first five test orders
            var testOrders = context.Orders
                .Include(o => o.Consultant)
                .Where(o => o.Consultant.Email.Equals(_odinConsultantUserName)).OrderBy(o => o.Id).Take(5);

            foreach (var testOrder in testOrders)
            {
                testOrder.ServiceFlag = (int) ServiceCategory.AccompaniedHomeFinding;
            }

            context.SaveChanges();
        }

    }
}
    