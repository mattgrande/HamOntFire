using System;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Routing;
using HamOntFire.Core;
using HamOntFire.Core.Domain;
using Raven.Database.Server;
using log4net;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Indexes;

namespace HamOntFire.Web
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        private static ILog _logger;
        public static IDocumentStore Store;

        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute("HeatMap",
                            "Statistics/HeatMap/{type}",
                            new {controller = "Statistics", action = "HeatMap", type = (string)null});

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );

        }

        protected void Application_Start()
        {
            if (!LogManager.GetRepository().Configured)
            {
                log4net.Config.XmlConfigurator.Configure();
            }

            _logger = LogManager.GetLogger(typeof(MvcApplication));
            _logger.Info("log4net initialized!");

            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);

            HttpEndpointRegistration.RegisterHttpEndpointTarget();

            Store = new DocumentStore {ConnectionStringName = "RavenDB"};
            Store.Initialize();

            IndexCreation.CreateIndexes(Assembly.GetAssembly(typeof(Event)), Store);

            using (var session = Store.OpenSession())
            {
                var tm = new TweetManager(session);

                long tweetsSince = tm.GetGreatestTweetId();
                tm.DownloadTweets(tweetsSince);
            }
        }
    }
}