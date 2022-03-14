using Client.Models;
using Communication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Query;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Controllers
{
    [Produces("application/json")]
    [Route("api/VideoClassifier")]
    public class RequestController : Controller
    {
        private readonly HttpClient httpClient;
        private readonly StatelessServiceContext context;
        private readonly FabricClient fabricClient;

        public static bool flag = false;

        public RequestController(HttpClient httpClient, StatelessServiceContext context, FabricClient fabricClient)
        {
            this.httpClient = httpClient;
            this.context = context;
            this.fabricClient = fabricClient;
        }

        [HttpPost]
        public async Task<IActionResult> ProcessRequest(string uploadedfileName)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            flag = true;
            ViewBag.flagvalue = flag;
            string prefix = @"D:\testSet\";
            if (!string.IsNullOrEmpty(uploadedfileName))
            {
                try
                {
                    string path = Path.Combine(prefix, uploadedfileName);
                    var serviceUri = new Uri("fabric:/VideoClassifier/Orchestrator");
                    ServicePartitionList partitions = await this.fabricClient.QueryManager.GetPartitionListAsync(serviceUri);

                    if (partitions == null || partitions.Count == 0)
                    {
                        return new ContentResult { StatusCode = 404, Content = "There's no available replica. Please check service status." };
                    }

                    ServicePartitionKey key = new ServicePartitionKey(((Int64RangePartitionInformation)(partitions[0].PartitionInformation)).LowKey);

                    var statefullProxy = ServiceProxy.Create<IOrchestrator>(serviceUri, key);

                    var testResult = await statefullProxy.GetVideoTags(path);
                    var result = new Result(); result.Message = "Jotodoro_test";
                    result.AllTags = testResult;
                    watch.Stop();
                    var elapsedMs = watch.ElapsedMilliseconds;
                    result.Message = $"Execution time {elapsedMs}";
                    return View("~/Views/Home/Index.cshtml", result);
                }
                catch (Exception ex)
                {
                    ViewBag.Message = "ERROR:" + ex.Message.ToString();
                    return View("~/Views/Home/Index.cshtml");
                }
            }
            else
            {
                ViewBag.Message = "You have not specified a file.";
                return View("~/Views/Home/Index.cshtml");
            }
            
        }
    }
}
