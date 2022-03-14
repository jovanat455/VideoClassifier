using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Communication;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System.IO;
//using Microsoft.ServiceFabric.Services.Remoting.Runtime;

namespace Orchestrator
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class Orchestrator : StatefulService, IOrchestrator
    {
        public Orchestrator(StatefulServiceContext context)
            : base(context)
        { }

        public async Task<List<TagModel>> GetVideoTags(string videoUri)
        {
            var taskId = Guid.NewGuid();
            using (var tx = this.StateManager.CreateTransaction())
            {
                var tasksQueue = await this.StateManager.GetOrAddAsync<IReliableQueue<Guid>>("tasksQueue");
                await tasksQueue.EnqueueAsync(tx, taskId);

                var currentTasks = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, JobObject>>("allTasks");
                await currentTasks.AddAsync(tx, key: taskId, value: new JobObject(videoUri, JobState.NotStarted));
                await tx.CommitAsync();

            }

            using (var tx = this.StateManager.CreateTransaction())
            {
                var jobsResult = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, List<TagModel>>>("jobsResult");
                var currentTasks = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, JobObject>>("allTasks");
                while (true)
                {
                    var currentTask = await currentTasks.TryGetValueAsync(tx, taskId);
                    if (currentTask.Value.State == JobState.Finished)
                    {
                        var jobResult = await jobsResult.TryGetValueAsync(tx, taskId);
                        return jobResult.Value;
                    }

                }
            }

        }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            //return new ServiceReplicaListener[0];
            return this.CreateServiceRemotingReplicaListeners();
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");
            var allTasks = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, JobObject>>("allTasks");
            var tasksQueue = await this.StateManager.GetOrAddAsync<IReliableQueue<Guid>>("tasksQueue");

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var tx = this.StateManager.CreateTransaction())
                {
                    var result = await myDictionary.TryGetValueAsync(tx, "Counter");
                    var nextTask = await tasksQueue.TryDequeueAsync(tx);

                    if(nextTask.Value != Guid.Empty)
                    {
                        var task = allTasks.TryGetValueAsync(tx, nextTask.Value);
                        task.Result.Value.State = JobState.Started;
                        await ProcessJob(task.Result.Value.VideoUri, nextTask.Value);
                    }

                    ServiceEventSource.Current.ServiceMessage(this.Context, "Current Counter Value: {0}",
                        result.HasValue ? result.Value.ToString() : "Value does not exist.");

                    await myDictionary.AddOrUpdateAsync(tx, "Counter", 0, (key, value) => ++value);

                    // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                    // discarded, and nothing is saved to the secondary replicas.
                    await tx.CommitAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        private async Task ProcessJob(string videoUri, Guid taskId)
        {
            var fileName = await Utility.GetVideoThumbnails(videoUri);
            //get all Thumbnails from the output folder
            var thumbnails = Utility.GetListOfThumbnails(fileName);

            //Reducing number of key frames
            thumbnails = Utility.ReduceNumberOfThumbnails(thumbnails);
            Dictionary<int, string> thumbnailList = PrepareThumbnails(thumbnails);

            var serviceUri = new Uri("fabric:/VideoClassifier/Worker");
            List<string> results = new List<string>();
            var workerProxy = ServiceProxy.Create<IWorker>(serviceUri);

            List<Task<TaggingJobResult>> allTasks = new List<Task<TaggingJobResult>>();
            List<Task<TaggingJobResult>> failedTasks = new List<Task<TaggingJobResult>>();
            foreach (var task in thumbnailList)
            {
                var t = new Task<TaggingJobResult>(() => workerProxy.StartJob(task.Key, task.Value).Result);
                t.Start();
                allTasks.Add(t);
            }

            Task.WaitAll(allTasks.ToArray());

            foreach (var t in allTasks)
            {
                if (t.Result.ResultCode == FinalState.Succeeded)
                {
                    results.Add(t.Result.Tags);
                }
            }
            var failedTaskCount = allTasks.Where(a => a.Result.ResultCode == FinalState.Error).Count();

            while (failedTaskCount > 0)
            {
                allTasks = RunFailedTasks(allTasks, thumbnailList, workerProxy, results);
                failedTaskCount = allTasks.Where(a => a.Result.ResultCode == FinalState.Error).Count();
            }

            var videoTags = await workerProxy.GetRelevantTags(results);

            using (var tx = this.StateManager.CreateTransaction())
            {
                var jobResult = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, List<TagModel>>>("jobsResult");
                await jobResult.AddAsync(tx, key: taskId, value: videoTags);

                var taskCollection = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, JobObject>>("allTasks");
                var currentTask = await taskCollection.TryGetValueAsync(tx, taskId);

                currentTask.Value.State = JobState.Finished;

                await tx.CommitAsync();
            }
        }

        private Dictionary<int, string> PrepareThumbnails(List<string> thumbnails)
        {
            Dictionary<int, string> thumbnailsList = new Dictionary<int, string>();
            int idCounter = 0;
            foreach(var t in thumbnails)
            {
                thumbnailsList.Add(idCounter++, t);
            }

            return thumbnailsList;
        }

        private List<Task<TaggingJobResult>> RunFailedTasks(List<Task<TaggingJobResult>> allTasks, Dictionary<int, string> thumbnailList, IWorker workerProxy, List<string> results)
        {
            var failedTasks = new List<Task<TaggingJobResult>>();

            foreach(var t in allTasks)
            {
                if(t.Result.ResultCode == FinalState.Error)
                {
                    string thumbnail;
                    thumbnailList.TryGetValue(t.Result.TaskId, out thumbnail);
                    var newTask = new Task<TaggingJobResult>(() => workerProxy.StartJob(t.Result.TaskId, thumbnail).Result);
                    failedTasks.Add(newTask);
                    newTask.Start();
                }
            }

            Task.WaitAll(failedTasks.ToArray());

            foreach(var t in failedTasks)
            {
                if(t.Result.ResultCode == FinalState.Succeeded)
                {
                    results.Add(t.Result.Tags);
                }
            }
            return failedTasks;
        }
    }
}
