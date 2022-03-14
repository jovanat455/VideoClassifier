using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Communication
{
    public interface IOrchestrator : IService
    {
        Task<List<TagModel>> GetVideoTags(string videoUri);
    }
}
