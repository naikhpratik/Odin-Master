using System.Web.Optimization;

namespace Odin
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            //Scripts----------------------------------------------------------------------------------------------------------------------

            bundles.Add(new ScriptBundle("~/bundles/app").Include(
                        "~/Scripts/app/controllers/userManagementController.js"));

            bundles.Add(new ScriptBundle("~/bundles/lib").Include(
                        "~/Scripts/jquery-{version}.js",                        
                        "~/Scripts/bootstrap.js",
                        "~/Scripts/moment.js",
                        "~/Scripts/bootstrap-datetimepicker.js",                        
                        "~/Scripts/respond.js",
                        "~/Scripts/jspdf.min.js",
                        "~/Scripts/datatables/jquery.datatables.js",
                        "~/Scripts/datatables/datatables.bootstrap.js",
                        "~/Scripts/app/views/_header.js",
                        "~/Scripts/dw_loader.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap-notify").Include(
                        "~/Scripts/bootstrap-notify.min.js"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at https://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            // Page scripts
            bundles.Add(new ScriptBundle("~/Scripts/orders").Include("~/Scripts/app/views/orders.js", "~/Scripts/typeahead.js"));
            bundles.Add(new ScriptBundle("~/Scripts/help").Include("~/Scripts/app/views/help.js"));
            bundles.Add(new ScriptBundle("~/Scripts/transferee").Include(
                "~/Scripts/bootstrap-notify.min.js",
                "~/Scripts/app/views/transferee.js",
                "~/Scripts/app/views/transferee-intake.js",
                "~/Scripts/app/views/transferee-details.js",
                "~/Scripts/app/views/transferee-housing.js",
                "~/Scripts/app/views/transferee-itinerary.js",
                "~/Scripts/app/views/transferee-itin-appntmt.js",
                "~/Scripts/app/views/transferee-hfp-messages.js",
                "~/Scripts/app/views/transferee-selectedproperty.js",
                "~/Scripts/leaflet.js"));

            bundles.Add(new ScriptBundle("~/Scripts/forgotPassword").Include(
                "~/Scripts/bootstrap-notify.min.js",
                "~/Scripts/app/views/forgotPassword.js"));

            //Originally bundled in Scripts folder, but getting 403 error in IFrame.
            bundles.Add(new ScriptBundle("~/bundles/bookmarklet").Include(
                "~/Scripts/bootstrap-notify.min.js",
                "~/Scripts/app/views/bookmarklet.js"));


            //STYLES----------------------------------------------------------------------------------------------------------------------


            bundles.Add(new StyleBundle("~/Content/cssBundle").Include(
                      "~/Content/styles/css/application.css"));

            // Page Styling
            bundles.Add(new StyleBundle("~/Styling/help").Include("~/Content/styles/css/refactor/help.css"));
            bundles.Add(new StyleBundle("~/Styling/orders").Include("~/Content/styles/css/refactor/orders.css"));
            bundles.Add(new StyleBundle("~/Styling/login").Include("~/Content/styles/css/refactor/login.css"));
            bundles.Add(new StyleBundle("~/Styling/forgotPassword").Include("~/Content/styles/css/refactor/forgotpassword.css"));
            bundles.Add(new StyleBundle("~/Styling/bookmarklet").Include("~/Content/styles/css/refactor/bookmarklet.css"));
            bundles.Add(new StyleBundle("~/Styling/transferee").Include("~/Content/styles/css/refactor/transferee.css"));
            bundles.Add(new StyleBundle("~/Styling/itineraryPdf").Include("~/Content/styles/css/refactor/itineraryPdf.css"));
            bundles.Add(new StyleBundle("~/Styling/propertiesPDF").Include("~/Content/styles/css/refactor/transferee/HFPropertiesPDF.css"));

        }
    }
}
