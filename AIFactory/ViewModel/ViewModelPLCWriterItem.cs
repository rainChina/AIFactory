using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AIFactory.ViewModel
{
    public partial class ViewModelPLCWriterItem : ObservableObject
    {
        [ObservableProperty]
        private string? name;

        [ObservableProperty]
        private string? id;

        [ObservableProperty]
        private double value;

        [RelayCommand]
        private void OnButtonClick()
        {
        }
    }

}
