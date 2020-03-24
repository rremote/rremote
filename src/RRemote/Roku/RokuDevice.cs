using System;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using RRemote.Common;

namespace RRemote.Roku
{
    [XmlRoot("device-info")]
    [DataContract]
    public class RokuDevice : BindableBase
    {
        private ObservableCollection<RokuApp> _Apps = new ObservableCollection<RokuApp>();

        private string _country = "";


        private string _developer_enabled = "";

        private string _device_id = "";


        private string _Endpoint = "";

        private string _ethernet_mac = "";

        private bool _IsSelected;

        private bool _IsPlaceholder = false;

        private bool _IsStatic;

        private string _language = "";

        private string _locale = "";

        private string _model_name = "";

        private string _model_number = "";

        private string _model_region = "";

        private string _network_type = "";

        private string _power_mode = "";

        private string _secure_device = "";

        private string _serial_number = "";

        private string _software_build = "";

        private string _software_version = "";

        private string _supports_audio_guide = "";

        private string _supports_ethernet = "";

        private string _supports_find_remote = "";

        private string _supports_suspend = "";

        private string _time_zone = "";

        private string _time_zone_offset = "";
        private string _udn = "";

        private string _user_device_name = "";

        private string _vendor_name = "";

        private string _wifi_mac = "";

        public object AppLocker = new object();

        [XmlElement("udn")]
        public string udn
        {
            get => _udn;
            set => SetProperty(ref _udn, value);
        }

        [XmlElement("serial-number")]
        [DataMember]
        public string serial_number
        {
            get => _serial_number;
            set => SetProperty(ref _serial_number, value);
        }

        [XmlElement("device-id")]
        public string device_id
        {
            get => _device_id;
            set => SetProperty(ref _device_id, value);
        }

        [XmlElement("vendor-name")]
        public string vendor_name
        {
            get => _vendor_name;
            set => SetProperty(ref _vendor_name, value);
        }

        [XmlElement("model-number")]
        public string model_number
        {
            get => _model_number;
            set => SetProperty(ref _model_number, value);
        }

        [XmlElement("model-name")]
        public string model_name
        {
            get => _model_name;
            set => SetProperty(ref _model_name, value);
        }

        [XmlElement("model-region")]
        public string model_region
        {
            get => _model_region;
            set => SetProperty(ref _model_region, value);
        }

        [XmlElement("supports-ethernet")]
        public string supports_ethernet
        {
            get => _supports_ethernet;
            set => SetProperty(ref _supports_ethernet, value);
        }

        [XmlElement("wifi-mac")]
        public string wifi_mac
        {
            get => _wifi_mac;
            set => SetProperty(ref _wifi_mac, value);
        }

        [XmlElement("ethernet-mac")]
        public string ethernet_mac
        {
            get => _ethernet_mac;
            set => SetProperty(ref _ethernet_mac, value);
        }

        [XmlElement("network-type")]
        public string network_type
        {
            get => _network_type;
            set => SetProperty(ref _network_type, value);
        }

        [XmlElement("user-device-name")]
        [DataMember]
        public string user_device_name
        {
            get => _user_device_name;
            set => SetProperty(ref _user_device_name, value);
        }

        [XmlElement("software-version")]
        public string software_version
        {
            get => _software_version;
            set => SetProperty(ref _software_version, value);
        }

        [XmlElement("software-build")]
        public string software_build
        {
            get => _software_build;
            set => SetProperty(ref _software_build, value);
        }

        [XmlElement("secure-device")]
        public string secure_device
        {
            get => _secure_device;
            set => SetProperty(ref _secure_device, value);
        }

        [XmlElement("language")]
        public string language
        {
            get => _language;
            set => SetProperty(ref _language, value);
        }

        [XmlElement("country")]
        public string country
        {
            get => _country;
            set => SetProperty(ref _country, value);
        }

        [XmlElement("locale")]
        public string locale
        {
            get => _locale;
            set => SetProperty(ref _locale, value);
        }

        [XmlElement("time-zone")]
        public string time_zone
        {
            get => _time_zone;
            set => SetProperty(ref _time_zone, value);
        }

        [XmlElement("time-zone-offset")]
        public string time_zone_offset
        {
            get => _time_zone_offset;
            set => SetProperty(ref _time_zone_offset, value);
        }

        [XmlElement("power-mode")]
        public string power_mode
        {
            get => _power_mode;
            set => SetProperty(ref _power_mode, value);
        }

        [XmlElement("supports-suspend")]
        public string supports_suspend
        {
            get => _supports_suspend;
            set => SetProperty(ref _supports_suspend, value);
        }

        [XmlElement("supports-find-remote")]
        public string supports_find_remote
        {
            get => _supports_find_remote;
            set => SetProperty(ref _supports_find_remote, value);
        }

        [XmlElement("supports-audio-guide")]
        public string supports_audio_guide
        {
            get => _supports_audio_guide;
            set => SetProperty(ref _supports_audio_guide, value);
        }

        [XmlElement("developer-enabled")]
        public string developer_enabled
        {
            get => _developer_enabled;
            set => SetProperty(ref _developer_enabled, value);
        }

        [DataMember]
        public string Endpoint
        {
            get => _Endpoint;
            set => SetProperty(ref _Endpoint, value);
        }

        [DataMember]
        public bool IsSelected
        {
            get => _IsSelected;
            set => SetProperty(ref _IsSelected, value);
        }

        public bool IsPlaceholder
        {
            get => _IsPlaceholder;
            set => SetProperty(ref _IsPlaceholder, value);
        }

        [DataMember]
        public bool IsStatic
        {
            get => _IsStatic;
            set => SetProperty(ref _IsStatic, value);
        }

        public DateTime LastSeen { get; set; } = DateTime.MinValue;

        public ObservableCollection<RokuApp> Apps
        {
            get => _Apps;
            set => SetProperty(ref _Apps, value);
        }

        [OnDeserializing]
        public void Deserialize(StreamingContext sc)
        {
            AppLocker = new object();
            _Apps = new ObservableCollection<RokuApp>();
        }

        public override string ToString()
        {
            return user_device_name;
        }
    }
}