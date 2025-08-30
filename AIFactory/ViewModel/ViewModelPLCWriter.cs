using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIFactory.ViewModel
{
    public class ViewModelPLCWriter : ObservableObject
    {
        public ObservableCollection<ViewModelPLCWriterItem> Items { get; } = new();

        public ViewModelPLCWriter()
        {
            Items.Add(new ViewModelPLCWriterItem { Name = "Item A", Id = "A001", Value = 10 });
            Items.Add(new ViewModelPLCWriterItem { Name = "Item B", Id = "B002", Value = 20 });
        }
    }



}
