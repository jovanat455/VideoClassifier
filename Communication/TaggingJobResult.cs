using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Communication
{
    public enum FinalState { Succeeded, Error};
    [DataContract]
    public class TaggingJobResult
    {
        [DataMember]
        public string Tags { get; set; }
        [DataMember]
        public  FinalState ResultCode { get; set; }
        [DataMember]
        public string Error { get; set; }
        [DataMember]
        public int TaskId { get; set; }

        public TaggingJobResult() { }

        public TaggingJobResult(string Tags, FinalState ResultCode, string Error, int TaskId)
        {
            this.Tags = Tags;
            this.ResultCode = ResultCode;
            this.Error = Error;
            this.TaskId = TaskId;
        }
    }
}
