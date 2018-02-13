using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Odin.Data.Core;
using Odin.Data.Core.Models;
using Odin.Data.Extensions;
using Odin.Data.Persistence;
using Odin.ToSeWebJob.QueueProcessors;
using ServicEngineImporter.Models;

namespace Odin.ToSeWebJob
{
    public class Functions
    {
        private readonly IUnitOfWork _unitOfWork;
        private const int MaxQueueCount = 5;

        public Functions(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called queue.

        // Test throwing exception and make sure item does not get dequeued.
        // TODO: Add conversion library of odin to servicengineimporter objects 

        public async Task ProcessQueueMessage([QueueTrigger("odintose")] string message, int dequeueCount, TextWriter log)
        {
            log.WriteLine("Deploy 1");
            log.WriteLine(message);
            if (dequeueCount >= MaxQueueCount)
            {
                log.WriteLine("Hit Max dequeue count");
                return;
            }
            var queueEntry = JsonConvert.DeserializeObject<OdinToSeQueueEntry>(message);
            if (queueEntry.QueueTypeId == (int) QueueType.Service)
            {
                var serviceProcessor = new ServiceProcessor(_unitOfWork);
                var result = await serviceProcessor.ProcessService(queueEntry.ObjectId, log);
                log.WriteLine($"Service Process Result: {result}");
            }
        }

    }
}
