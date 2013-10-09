using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Management;
using System.Web.Mvc;
using HamOntFire.Core;
using HamOntFire.Core.Domain;

namespace HamOntFire.Web.Controllers
{
    public class HomeController : RavenController
    {
        //
        // GET: /Home/
        public ActionResult Index()
        {
            ViewBag.Title = "Home";
            return View();
        }

        public JsonResult Fetch()
        {
            try
            {
                var events = RavenSession.Query<Event>("Events/ByUpdatedAtSortByUpdatedAt")
                    .Where(e => e.UpdatedAt > DateTime.Now.AddHours(-3));
                var list = new List<dynamic>();

                foreach (Event @event in events)
                {
                    dynamic dynamicEvent = @event.ToViewModel();
                    list.Add(dynamicEvent);
                }
                return Json(list, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                new LogEvent(ex).Raise();
                throw;
            }
        }

        /// <summary>
        /// Re-GeoCode -> ReoCode.  Get it?
        /// </summary>
        public JsonResult ReoCode()
        {
            var events = RavenSession.Query<Event>().Where(e => (e.Lat == 0m || e.Long == 0m) && e.Location != "Location Not Available").ToList();

            var dict = new Dictionary<string, List<String>>
                {
                    {"Parsed", new List<string>()},
                    {"Unparsed", new List<string>()}
                };

            var g = new GeoCoder();
            foreach (Event @event in events)
            {
                TweetManager.ParseLocation(@event, @event.Location);
                g.GeoCode( @event );

                if (@event.Lat == 0 || @event.Long == 0)
                {
                    dict["Unparsed"].Add(@event.Location);
                }
                else
                {
                    dict["Parsed"].Add(string.Format("{0}:{1},{2}", @event.Location, @event.Lat, @event.Long));
                }
            }

            return Json(dict, JsonRequestBehavior.AllowGet);
        }

        public ActionResult About()
        {
            ViewBag.Title = "About";
            return View();
        }
    }

    public class LogEvent : WebRequestErrorEvent
    {
        public LogEvent(string message) : base(null, null, 100001, new Exception(message))
        {
            
        }
        public LogEvent(Exception ex) : base(null, null, 100001, ex)
        {
            
        }
    }
}
