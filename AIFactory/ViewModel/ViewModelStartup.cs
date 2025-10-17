using AIFactory.Message;
using AIFactory.Util;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            StartWindowConfirmMessage msg = new StartWindowConfirmMessage(true);
            WeakReferenceMessenger.Default.Send(msg);
        }
    }
}
