using AIFactory.Message;
using AIFactory.Util;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace AIFactory.ViewModel
{
    public partial class ViewModelStartup : ObservableObject
    {

        [ObservableProperty]
        private string? plcIP;

        [ObservableProperty]
        private int? plcPort;

        [ObservableProperty]
        private string? mesIP;


        [ObservableProperty]
        private int? plcReadInterval;

        [ObservableProperty]
        private int? plcSaveInterval;

        [ObservableProperty]
        private int? mesReadInterval;

        [ObservableProperty]
        private int? mesSaveInterval;



        public ViewModelStartup()
        {
            var userPreference = App.Services.GetService<UserPreference>();

            plcIP = userPreference?.AddressPlc;
            plcPort = userPreference?.PortPlc;
            mesIP = userPreference?.AddressMES;
            plcReadInterval = userPreference?.PlcReadInterval;
            MesReadInterval = userPreference?.MesSaveInterval;
        }

        [RelayCommand]
        private void Confirm()
        {
            var userPreference = App.Services.GetService<UserPreference>();

            userPreference.AddressPlc = PlcIP;
            userPreference.PortPlc = plcPort.Value;
            userPreference.AddressMES = mesIP;
            userPreference.PlcReadInterval = plcReadInterval.Value;
            userPreference.MesSaveInterval = MesReadInterval.Value;

            XmlSerializer serializer = new XmlSerializer(typeof(UserPreference));

            // 写入 XML 文件
            using (StreamWriter writer = new StreamWriter(UserPreference.FileName))
            {
                serializer.Serialize(writer, userPreference);
            }

            StartWindowConfirmMessage msg = new StartWindowConfirmMessage(true);
            WeakReferenceMessenger.Default.Send(msg);
        }
    }
}
