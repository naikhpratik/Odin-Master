// <auto-generated />
namespace Odin.Data.Persistence.Migrations
{
    using System.CodeDom.Compiler;
    using System.Data.Entity.Migrations;
    using System.Data.Entity.Migrations.Infrastructure;
    using System.Resources;
    
    [GeneratedCode("EntityFramework.Migrations", "6.1.3-40302")]
    public sealed partial class AddEstimatedArrivalToOrders : IMigrationMetadata
    {
        private readonly ResourceManager Resources = new ResourceManager(typeof(AddEstimatedArrivalToOrders));
        
        string IMigrationMetadata.Id
        {
            get { return "201709051444089_AddEstimatedArrivalToOrders"; }
        }
        
        string IMigrationMetadata.Source
        {
            get { return null; }
        }
        
        string IMigrationMetadata.Target
        {
            get { return Resources.GetString("Target"); }
        }
    }
}