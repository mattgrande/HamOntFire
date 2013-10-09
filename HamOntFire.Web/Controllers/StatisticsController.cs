using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Web.UI;
using HamOntFire.Core;
using HamOntFire.Core.Domain;

namespace HamOntFire.Web.Controllers
{
    public class StatisticsController : RavenController
    {
        //
        // GET: /Statistics/
        public ActionResult Index()
        {
            ViewBag.Title = "Stats";

            var vm = new StatisticsViewModel();

            // Event Count by Event Type
            vm.EventCountByType = RavenSession.Query<Events_Count.ReduceResult, Events_Count>()
                .OrderByDescending(x => x.Count).ToList();
            int total = vm.EventCountByType.Sum(r => r.Count);

            // Anything less than 1%, put into the 'Other' column
            var other = new Events_Count.ReduceResult {Name = "Other"};
            for (int i = vm.EventCountByType.Count - 1; i >= 0; i--)
            {
                var percent = ((double)vm.EventCountByType[i].Count / total) * 100;
                if (percent > 1)
                    break;

                other.Count += vm.EventCountByType[i].Count;
                vm.EventCountByType.RemoveAt(i);
            }

            if (other.Count > 0)
                vm.EventCountByType.Add(other);

            // Average # of Units per Event Type
            vm.AverageUnitsPerEventType = RavenSession.Query<Events_UnitsPerType.ReduceResult, Events_UnitsPerType>()
                .OrderByDescending(x => x.UnitsPerType).ToList();

            var types = RavenSession.Query<Events_DistinctTypes.ReduceResult, Events_DistinctTypes>()
                .OrderBy(x => x.Type).Take(100);
            foreach (var type in types)
            {
                vm.EventTypes.Add( new SelectListItem() { Text = type.Type });
            }
            
            return View( vm );
        }

        [OutputCache(NoStore = true, Location = OutputCacheLocation.None)]
        public FileStreamResult HeatMap(string type=null)
        {
            if (type == string.Empty)
                type = null;
            var mapGen = new MapGenerator();
            var bmap = mapGen.Generate(type, 0, RavenSession);
            var ms = new MemoryStream();
            bmap.Save(ms, ImageFormat.Jpeg);
            ms.Position = 0;
            return File(ms, "image/jpeg");
        }
    }

    public class StatisticsViewModel
    {
        public StatisticsViewModel()
        {
            EventTypes = new List<SelectListItem> {new SelectListItem {Text = "All", Value = string.Empty}};
        }

        public List<Events_Count.ReduceResult> EventCountByType { get; set; }
        public List<Events_UnitsPerType.ReduceResult> AverageUnitsPerEventType { get; set; }
        public List<SelectListItem> EventTypes { get; set; } 
    }

    public static class JsonExtension
    {
        public static string ToJson(this object o)
        {
            var jss = new JavaScriptSerializer();
            return jss.Serialize(o);
        }
    }
}
