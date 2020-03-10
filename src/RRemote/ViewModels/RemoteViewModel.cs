﻿using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Toolkit.Uwp.Helpers;
using RRemote.Common;
using RRemote.Roku;

namespace RRemote.ViewModels
{
    public class RemoteViewModel : BindableBase
    {
        private static readonly object DeviceLocker = new object();
        private ObservableCollection<RokuDevice> _availableDevices = new ObservableCollection<RokuDevice>();
        private bool _buttonsEnabled;
        private string _myIp = "";
        private RokuDevice _selectedDevice;
        private ComboBoxItem _selectedInput;
        private bool _suspending;
        private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;
        private string _mcIp = "239.255.255.250";
        private int _mcPort = 1900;

        private bool Suspending
        {
            get => _suspending;
            set
            {
                _suspending = value;
                DiscoverDeviceTimer?.Cancel();
                DiscoverDeviceTimer = null;
            }
        }

        public string MyIp
        {
            get => _myIp;
            set => SetProperty(ref _myIp, value);
        }

        public ObservableCollection<RokuDevice> AvailableDevices
        {
            get => _availableDevices;
            //OnPropertyChanged("AvailableDevices");
            set => SetProperty(ref _availableDevices, value);
        }

        public RokuDevice SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                ButtonsEnabled = value != null;
                SetProperty(ref _selectedDevice, value);

                if (value != null)
                {
                    lock (DeviceLocker)
                    {
                        foreach (var dev in AvailableDevices)
                            dev.IsSelected = false;
                    }

                    value.IsSelected = true;
                    DiscoverApps(value);
                }

                PressButtonCommand.RaiseCanExecuteChanged();
                LaunchAppCommand.RaiseCanExecuteChanged();
                DeleteDeviceCommand.RaiseCanExecuteChanged();
                ShowDeviceDetailsCommand.RaiseCanExecuteChanged();
            }
        }

        public ComboBoxItem SelectedInput
        {
            get => _selectedInput;
            set
            {
                if (SelectedDevice != null)
                    _ = RokuClient.PressKey(SelectedDevice.Endpoint, (string) value.Tag);
                SetProperty(ref _selectedInput, null);
            }
        }

        public bool ButtonsEnabled
        {
            get => _buttonsEnabled;
            set => SetProperty(ref _buttonsEnabled, value);
        }

        public async Task PressKeyboardKey(string key)
        {
            var prefix = "";
            if (key.Length == 1)
                prefix = "Lit_";
            await RokuClient.PressKey(SelectedDevice.Endpoint, prefix + WebUtility.UrlEncode(key));
        }

        private void UiRun(Action action)
        {
            _ = CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () => { action(); });
        }

        private string GetLocalIp()
        {
            var ips = "";
            var icp = NetworkInformation.GetInternetConnectionProfile();

            if (icp?.NetworkAdapter == null) return "";
            var hostnames = NetworkInformation.GetHostNames();
            foreach (var host in hostnames)
                //if (host.IPInformation != null
                //    && host.IPInformation.NetworkAdapter != null
                //    && host.IPInformation.NetworkAdapter.NetworkAdapterId != null)
                //    ips += $"{host.IPInformation.NetworkAdapter.NetworkAdapterId} ";
                //else
                ips += $"{host.CanonicalName} ";

            // the ip address
            return ips;
        }

        #region Construction/Destruction

        public RemoteViewModel()
        {
            MyIp = GetLocalIp();
            AvailableDevices.CollectionChanged += AvailableDevices_CollectionChanged;
        }

        private void AvailableDevices_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UiRun(() =>
            {
                //If no default device has been selected make it this one
                if (e.Action == NotifyCollectionChangedAction.Add
                    && SelectedDevice == null
                    && e.NewItems.Count > 0)
                    SelectedDevice = (RokuDevice) e.NewItems[0];
            });
        }

        public void Resume()
        {
            _ = LoadSettings();
            DiscoverDeviceTimer_Tick(null);
        }

        public void Suspend()
        {
            Suspending = true;
            _ = SaveSettings();
        }

        #endregion

        #region Discovery

        private int DiscoverDeviceTimeInMinutes { get; } = 15;
        private ThreadPoolTimer DiscoverDeviceTimer { get; set; }

        private void DiscoverDeviceTimer_Tick(ThreadPoolTimer timer)
        {
            Task.Run(() =>
            {
                //Search the local network using SSDP
                _ = RokuClient.Discover(DeviceDiscovered, _mcIp, _mcPort);

                //Update all the static devices that may not be discoverable
                //due to being on a different subnet or SSDP being blocked
                RokuDevice[] devices;
                lock (DeviceLocker)
                {
                    devices = AvailableDevices.Where(x => x.IsStatic).ToArray();
                }

                foreach (var dev in devices)
                    _ = RokuClient.ReadDevice(DeviceDiscovered, dev.Endpoint);

                //Remove any devices that haven't been seen via SSDP
                //Static devices have a LastSeen of +100 years so they don't get removed
                devices = new RokuDevice[0];
                lock (DeviceLocker)
                {
                    devices = AvailableDevices.Where(x => !x.IsStatic).ToArray();
                }

                foreach (var dev in devices)
                    if (dev.LastSeen < DateTime.Now.AddMinutes(-1 * DiscoverDeviceTimeInMinutes))
                        UiRun(() =>
                        {
                            lock (DeviceLocker)
                            {
                                AvailableDevices.Remove(dev);
                            }
                        });
                //If this is the first run the timer is null so set it up for regular
                //polling
                if (DiscoverDeviceTimer == null)
                    DiscoverDeviceTimer = ThreadPoolTimer.CreatePeriodicTimer(DiscoverDeviceTimer_Tick,
                        TimeSpan.FromMinutes(DiscoverDeviceTimeInMinutes));
            });
        }

        private void DeviceDiscovered(RokuDevice discoveredDevice)
        {
            var selected = SelectedDevice;
            var bFound = false;
            discoveredDevice.LastSeen = DateTime.Now;

            //This function can be called from different threads so 
            //make access to AvailableDevices thread safe
            RokuDevice[] devices;
            lock (DeviceLocker)
            {
                devices = AvailableDevices.ToArray();
            }

            foreach (var aDevice in devices)
                if (aDevice.serial_number == discoveredDevice.serial_number)
                {
                    //Static devices always exist in AvailableDevices so set the
                    //last seen high enough that it doesn't get removed
                    if (aDevice.IsStatic)
                        discoveredDevice.LastSeen = DateTime.Now.AddYears(100);
                    MergeDevice(discoveredDevice, aDevice);
                    bFound = true;
                    break;
                }

            //If the device didn't already exist in our list, add it now
            if (!bFound)
                UiRun(() =>
                {
                    lock (DeviceLocker)
                    {
                        AvailableDevices.Add(discoveredDevice);
                    }
                });
        }

        private void MergeDevice(RokuDevice source, RokuDevice dest)
        {
            UiRun(() =>
            {
                dest.udn = source.udn;
                dest.serial_number = source.serial_number;
                dest.device_id = source.device_id;
                dest.vendor_name = source.vendor_name;
                dest.model_number = source.model_number;
                dest.model_name = source.model_name;
                dest.model_region = source.model_region;
                dest.supports_ethernet = source.supports_ethernet;
                dest.wifi_mac = source.wifi_mac;
                dest.ethernet_mac = source.ethernet_mac;
                dest.network_type = source.network_type;
                dest.user_device_name = source.user_device_name;
                dest.software_version = source.software_version;
                dest.software_build = source.software_build;
                dest.secure_device = source.secure_device;
                dest.language = source.language;
                dest.country = source.country;
                dest.locale = source.locale;
                dest.time_zone = source.time_zone;
                dest.time_zone_offset = source.time_zone_offset;
                dest.power_mode = source.power_mode;
                dest.supports_suspend = source.supports_suspend;
                dest.supports_find_remote = source.supports_find_remote;
                dest.supports_audio_guide = source.supports_audio_guide;
                dest.developer_enabled = source.developer_enabled;
                dest.Endpoint = source.Endpoint;
                dest.IsStatic = source.IsStatic || dest.IsStatic;
                dest.LastSeen = source.LastSeen;
            });
        }

        private void DiscoverApps(RokuDevice device)
        {
            Task.Run(() =>
            {
                var dApps = RokuClient.ListDeviceApps(device.Endpoint).Result;

                foreach (var dApp in dApps)
                    AppDiscovered(device, dApp);

                FindAndSetActiveApp();
            });
        }

        private void AppDiscovered(RokuDevice device, RokuApp discoveredApp)
        {
            var bFound = false;
            discoveredApp.Endpoint = device.Endpoint;
            UiRun(() =>
            {
                lock (device.AppLocker)
                {
                    foreach (var aApp in device.Apps)
                        if (aApp.Id == discoveredApp.Id)
                        {
                            MergeApp(discoveredApp, aApp);
                            bFound = true;
                            break;
                        }

                    if (!bFound)
                        device.Apps.Add(discoveredApp);
                }
            });
        }

        private void MergeApp(RokuApp source, RokuApp dest)
        {
            dest.Name = source.Name;
            dest.Type = source.Type;
            dest.Version = source.Version;
        }

        private void FindAndSetActiveApp()
        {
            if (SelectedDevice != null)
            {
                var activeApp = RokuClient.GetCurrentApp(SelectedDevice.Endpoint).Result;
                MakeAppActive(SelectedDevice, activeApp);
            }
        }

        private void MakeAppActive(RokuDevice device, RokuActiveApp activeApp)
        {
            UiRun(() =>
            {
                lock (device.AppLocker)
                {
                    foreach (var app in device.Apps)
                        app.IsSelected = app.Id == activeApp?.App.Id;
                }
            });
        }

        #endregion

        #region New Manual Device and Details

        private bool _showNewDeviceForm;

        public bool ShowNewDeviceForm
        {
            get => _showNewDeviceForm;
            set => SetProperty(ref _showNewDeviceForm, value);
        }

        private bool _showDeviceDetails;

        public bool ShowDeviceDetails
        {
            get => _showDeviceDetails;
            set => SetProperty(ref _showDeviceDetails, value);
        }

        private RokuDevice _newDevice;

        public RokuDevice NewDevice
        {
            get => _newDevice;
            set => SetProperty(ref _newDevice, value);
        }

        private DelegateCommand _addNewDeviceCommand;

        public DelegateCommand AddNewDeviceCommand =>
            _addNewDeviceCommand ?? (_addNewDeviceCommand = new DelegateCommand(
                async () => { await AddNewDeviceCommandAsync(); },
                AddNewDeviceCommandCanExecute));

        private async Task AddNewDeviceCommandAsync()
        {
            NewDevice = new RokuDevice();
            ShowNewDeviceForm = true;
        }

        private bool AddNewDeviceCommandCanExecute()
        {
            return true;
        }

        private DelegateCommand _deleteDeviceCommand;

        public DelegateCommand DeleteDeviceCommand =>
            _deleteDeviceCommand ?? (_deleteDeviceCommand = new DelegateCommand(
                async () => { await DeleteDeviceCommandAsync(); },
                DeleteDeviceCommandCanExecute));

        private async Task DeleteDeviceCommandAsync()
        {
            if (SelectedDevice != null)
            {
                var dialog = new ContentDialog
                {
                    Title = "Confirm",
                    Content = "Are you sure you want to delete this device?",
                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel"
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                    lock (DeviceLocker)
                    {
                        AvailableDevices.Remove(SelectedDevice);
                        if (AvailableDevices.Count > 0)
                        {
                            SelectedDevice = AvailableDevices.ElementAt(0);
                            SelectedDevice.IsSelected = true;
                        }
                    }
            }
        }

        private bool DeleteDeviceCommandCanExecute()
        {
            return SelectedDevice != null;
        }

        private DelegateCommand _saveNewDeviceCommand;

        public DelegateCommand SaveNewDeviceCommand =>
            _saveNewDeviceCommand ?? (_saveNewDeviceCommand = new DelegateCommand(
                async () => { await SaveNewDeviceCommandAsync(); },
                SaveNewDeviceCommandCanExecute));

        private async Task SaveNewDeviceCommandAsync()
        {
            ShowNewDeviceForm = false;
            await RokuClient.ReadDevice(DeviceDiscovered, $"http://{NewDevice.Endpoint}/");
            NewDevice = null;
        }

        private bool SaveNewDeviceCommandCanExecute()
        {
            return true;
        }

        private DelegateCommand _cancelNewDeviceCommand;

        public DelegateCommand CancelNewDeviceCommand =>
            _cancelNewDeviceCommand ?? (_cancelNewDeviceCommand = new DelegateCommand(
                async () => { await CancelNewDeviceCommandAsync(); },
                CancelNewDeviceCommandCanExecute));

        private async Task CancelNewDeviceCommandAsync()
        {
            NewDevice = new RokuDevice();
            ShowNewDeviceForm = false;
        }

        private bool CancelNewDeviceCommandCanExecute()
        {
            return true;
        }

        private DelegateCommand _showDeviceDetailsCommand;

        public DelegateCommand ShowDeviceDetailsCommand =>
            _showDeviceDetailsCommand ?? (_showDeviceDetailsCommand = new DelegateCommand(
                async () => { await ShowDeviceDetailsCommandAsync(); },
                ShowDeviceDetailsCommandCanExecute));

        private async Task ShowDeviceDetailsCommandAsync()
        {
            ShowDeviceDetails = true;
        }

        private bool ShowDeviceDetailsCommandCanExecute()
        {
            return SelectedDevice != null;
        }

        private DelegateCommand _hideFormsCommand;

        public DelegateCommand HideFormsCommand => _hideFormsCommand ?? (_hideFormsCommand = new DelegateCommand(
                                                       async () => { await HideFormsCommandAsync(); },
                                                       HideFormsCommandCanExecute));

        private async Task HideFormsCommandAsync()
        {
            ShowDeviceDetails = false;
            ShowNewDeviceForm = false;
        }

        private bool HideFormsCommandCanExecute()
        {
            return true;
        }

        #endregion

        #region Commands

        private DelegateCommand<string> _pressButtonCommand;

        public DelegateCommand<string> PressButtonCommand =>
            _pressButtonCommand ?? (_pressButtonCommand = new DelegateCommand<string>(
                async obj => { await PressButtonCommandAsync(obj); },
                PressButtonCommandCanExecute));

        private async Task PressButtonCommandAsync(string obj)
        {
            if (SelectedDevice != null) _ = RokuClient.PressKey(SelectedDevice.Endpoint, obj);
        }

        private bool PressButtonCommandCanExecute(string obj)
        {
            return SelectedDevice != null;
        }

        private DelegateCommand<string> _launchAppCommand;

        public DelegateCommand<string> LaunchAppCommand =>
            _launchAppCommand ?? (_launchAppCommand = new DelegateCommand<string>(
                async obj => { await LaunchAppCommandAsync(obj); },
                LaunchAppCommandCanExecute));

        private async Task LaunchAppCommandAsync(string obj)
        {
            if (SelectedDevice != null) _ = RokuClient.LaunchApp(SelectedDevice.Endpoint, obj);
        }

        private bool LaunchAppCommandCanExecute(string obj)
        {
            return SelectedDevice != null;
        }

        #endregion

        #region Settings

        private async Task LoadSettings()
        {
            if (!_localSettings.Values.ContainsKey("mc_ip"))
                _localSettings.Values["mc_ip"] = "239.255.255.250";
            _mcIp = _localSettings.Values["mc_ip"] as string;

            if (!_localSettings.Values.ContainsKey("mc_port"))
                _localSettings.Values["mc_port"] = 1900;
            _mcPort = (int) _localSettings.Values["mc_port"];

            var localFolder = ApplicationData.Current.LocalFolder;
            StorageFile deviceFile = null;

            if (!await localFolder.FileExistsAsync("devices.xml"))
                deviceFile = await localFolder.CreateFileAsync("devices.xml");
            else
                deviceFile = await localFolder.GetFileAsync("devices.xml");
            var devicesXml = await FileIO.ReadTextAsync(deviceFile);

            var devices = new RokuDevice[0];
            if (!string.IsNullOrWhiteSpace(devicesXml))
                devices = Serializer.Deserialize<RokuDevice[]>(devicesXml);
            AvailableDevices.Clear();
            foreach (var dev in devices)
            {
                if (dev.IsStatic)
                    dev.LastSeen = DateTime.Now.AddYears(100);
                else
                    dev.LastSeen = DateTime.Now;
                if (dev.Apps == null)
                    dev.Apps = new ObservableCollection<RokuApp>();
                AvailableDevices.Add(dev);
                if (dev.IsSelected)
                    SelectedDevice = dev;
            }

            if (SelectedDevice == null && AvailableDevices.Count > 0)
                SelectedDevice = AvailableDevices.ElementAt(0);
        }

        private async Task SaveSettings()
        {
            _localSettings.Values["mc_ip"] = _mcIp;
            _localSettings.Values["mc_port"] = _mcPort;

            var localFolder = ApplicationData.Current.LocalFolder;
            StorageFile deviceFile = null;

            if (!await localFolder.FileExistsAsync("devices.xml"))
                deviceFile = await localFolder.CreateFileAsync("devices.xml");
            else
                deviceFile = await localFolder.GetFileAsync("devices.xml");

            await FileIO.WriteTextAsync(deviceFile, Serializer.Serialize(AvailableDevices.ToArray()));
        }

        #endregion
    }
}