using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace RRemote.Roku
{
    [DataContract]
    public class RokuApp
    {
        [XmlAttribute("id")] 
        [DataMember] 
        public string Id { get; set; }

        [XmlText] 
        [DataMember] 
        public string Name { get; set; }

        [XmlAttribute("type")]
        [DataMember] 
        public string Type { get; set; }

        [XmlAttribute("version")] 
        [DataMember] 
        public string Version { get; set; }

        [DataMember] 
        public bool IsSelected { get; set; }

        public string NumberAndName => $"{Id} {Name}";

        public string Endpoint { get; set; }
        public string Icon => Endpoint + "query/icon/" + Id;
    }

    [XmlRoot("apps")]
    public class DeviceAppWrapper
    {
        [XmlElement("app")] 
        public List<RokuApp> Apps { get; set; }
    }
}