using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Communication
{
    [DataContract]
    public class TagModel
    {
        [DataMember]
        public double Confidence { get; set; }
        [DataMember]
        public string Tag { get; set; }

        public TagModel() { }

        public TagModel(double Confidence, string Tag)
        {
            this.Confidence = Confidence;
            this.Tag = Tag;
        }
    }
}
