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
            Items.Add(new ViewModelPLCWriterItem { Name = "易爆Co低定值", Id = "A001", Value = 10 });
            Items.Add(new ViewModelPLCWriterItem { Name = "易爆Co高定值", Id = "B002", Value = 20 });
            Items.Add(new ViewModelPLCWriterItem { Name = "易爆O2低定值", Id = "B002", Value = 20 });
            Items.Add(new ViewModelPLCWriterItem { Name = "易爆O2高定值", Id = "B002", Value = 20 });
            Items.Add(new ViewModelPLCWriterItem { Name = "二次下抢Co低1", Id = "B002", Value = 20 });
            Items.Add(new ViewModelPLCWriterItem { Name = "二次下抢Co高2", Id = "B002", Value = 20 });
            Items.Add(new ViewModelPLCWriterItem { Name = "进口Co浓度量程设定", Id = "B002", Value = 20 });
            Items.Add(new ViewModelPLCWriterItem { Name = "进口Co透过率量程设定", Id = "B002", Value = 20 });
            Items.Add(new ViewModelPLCWriterItem { Name = "进口O2浓度量程设定", Id = "B002", Value = 20 });
            Items.Add(new ViewModelPLCWriterItem { Name = "进口O2透过率量程设定", Id = "B002", Value = 20 });
        }

      
    }



}
