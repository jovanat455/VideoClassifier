using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Communication
{
    public interface IWorker : IService
    {
        Task<TaggingJobResult> StartJob(int jobId, string imageUri);
        Task<List<TagModel>> GetRelevantTags(List<string> tags);
    }
}
