using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Communication;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using System.IO;

namespace Worker
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class Worker : StatelessService, IWorker
    {
        public Worker(StatelessServiceContext context)
            : base(context)
        { }

        public async Task<TaggingJobResult> StartJob(int jobId, string imageUri)
        {
            //using StreamWriter file = new StreamWriter($@"D:\testSet\output\{Guid.NewGuid()}_taskId_{jobId}.txt", append: true);
            //file.WriteLine($"Task Id: {jobId} worker: {this.Context.InstanceId}");

            var response = await WorkerUtility.GetImageTags(imageUri);
            response.TaskId = jobId;
            return response;
        }

        public async Task<List<TagModel>> GetRelevantTags(List<string> tags)
        {
            return WorkerUtility.PrepareTags(tags).OrderByDescending(a => a.Confidence).ToList();
        }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            //return new ServiceInstanceListener[0];
            return this.CreateServiceRemotingInstanceListeners();
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            long iterations = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ServiceEventSource.Current.ServiceMessage(this.Context, "Working-{0}", ++iterations);

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}
