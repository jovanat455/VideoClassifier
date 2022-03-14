using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Communication
{
    [DataContract]
    public enum JobState {[EnumMember] NotStarted, [EnumMember] Started, [EnumMember] Finished };
    [DataContract]
    public class JobObject
    {
        [DataMember]
        public string VideoUri { get; set; }
        [DataMember]
        public JobState State { get; set; }

        public JobObject() { }
        public JobObject(string VideoUri, JobState State)
        {
            this.VideoUri = VideoUri;
            this.State = State;
        }
    }
}
