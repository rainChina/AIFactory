using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.Generic;

namespace SQLiteViewer
{
    /// <summary>
    /// Interaction logic for WinChart.xaml
    /// </summary>
    public partial class WinChart : Window
    {
        public WinChart(List<SensorGroup> groupedResults)
        {
            InitializeComponent();
            dataGrid.AutoGenerateColumns = false;
            dataGrid.Columns.Clear();

            foreach (var group in groupedResults)
            {
                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = group.NameID,
                    Binding = new Binding(group.NameID)
                });
            }
            FillValue(groupedResults);
        }

        private void FillDatagrid(List<SensorGroup> groupedResults)
        {
            // 1. Get all unique timestamps from all sensors
            var allTimestamps = groupedResults
                .SelectMany(g => g.ChartPoints.Select(p => p.Time))
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            var gridRows = new List<dynamic>();

            foreach (var time in allTimestamps)
            {
                IDictionary<string, object> row = new ExpandoObject();
                row["Time"] = time.ToString("yyyy-MM-dd HH:mm:ss");

                foreach (var group in groupedResults)
                {
                    var point = group.ChartPoints.FirstOrDefault(p => p.Time == time);

                    // We use object here so the column can handle both numbers and "N/A"
                    // But it is usually better to use null so the DataGrid shows an empty cell
                    row[group.NameID] = (object)point?.Value ?? "N/A";
                }
                gridRows.Add(row);
            }

            dataGrid.ItemsSource = gridRows;
        }

        private void FillData(List<SensorGroup> groupedResults)
        {
            // 1. Get all unique timestamps
            var allTimestamps = groupedResults
                .SelectMany(g => g.ChartPoints.Select(p => p.Time))
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            // 2. Pre-index the data: Dictionary<SensorName, Dictionary<DateTime, Value>>
            // This turns a slow search into a lightning-fast hash lookup
            var sensorLookup = groupedResults.ToDictionary(
                g => g.NameID,
                g => g.ChartPoints.ToDictionary(p => p.Time, p => p.Value)
            );

            var gridRows = new List<dynamic>(allTimestamps.Count);

            foreach (var time in allTimestamps)
            {
                // Use ExpandoObject for the DataGrid binding
                IDictionary<string, object> row = new ExpandoObject();
                row["Time"] = time.ToString("yyyy-MM-dd HH:mm:ss");

                foreach (var sensorName in sensorLookup.Keys)
                {
                    // O(1) Lookup instead of O(N) Search
                    if (sensorLookup[sensorName].TryGetValue(time, out double val))
                    {
                        row[sensorName] = val;
                    }
                    else
                    {
                        row[sensorName] = null; // Use null for speed; DataGrid handles it better than "N/A"
                    }
                }
                gridRows.Add(row);
            }
        }


        private void FillValue(List<SensorGroup> groupedResults)
        {
            // 1. Find the maximum number of rows needed (the longest sensor list)
            int maxRows = groupedResults.Max(g => g.ChartPoints.Count);

            // 2. Pre-allocate the list with capacity for speed
            var gridRows = new List<dynamic>(maxRows);

            // 3. Create the rows
            for (int i = 0; i < maxRows; i++)
            {
                IDictionary<string, object> row = new ExpandoObject();

                foreach (var group in groupedResults)
                {
                    // Check if this sensor has a value at this index
                    if (i < group.ChartPoints.Count)
                    {
                        row[group.NameID] = group.ChartPoints[i].Value;
                    }
                    else
                    {
                        row[group.NameID] = null; // Sensor has fewer data points than others
                    }
                }
                gridRows.Add(row);
            }

            dataGrid.ItemsSource = gridRows;
        }


    }
}
