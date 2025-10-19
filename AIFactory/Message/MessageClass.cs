using AIFactory.Model;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIFactory.Message
{
    public class ClearChartDataMessage : ValueChangedMessage<string>
    {
        public ClearChartDataMessage(string reason) : base(reason) { }
    }



    public class GasMessage : ValueChangedMessage<DataPoint>
    {
        public GasMessage(DataPoint value) : base(value) { }
    }

    public class StartWindowConfirmMessage : ValueChangedMessage<bool>
    {
        public StartWindowConfirmMessage(bool reason) : base(reason) { }
    }

    public class TaskStartMessage : ValueChangedMessage<bool>
    {
        public TaskStartMessage(bool reason) : base(reason) { }
    }
}
