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
using LiveChartsCore;
using System.Diagnostics;
using System.Text.Json;
using System.IO;

namespace SQLiteViewer
{
    /// <summary>
    /// Interaction logic for WinChart.xaml
    /// </summary>
    public partial class WinChart : Window
    {
        //public WinChart(List<SensorGroup> groupedResults)
        //{
        //    InitializeComponent();
        //    dataGrid.AutoGenerateColumns = false;
        //    dataGrid.Columns.Clear();

        //    foreach (var group in groupedResults)
        //    {
        //        dataGrid.Columns.Add(new DataGridTextColumn
        //        {
        //            Header = group.NameID,
        //            Binding = new Binding(group.NameID)
        //        });
        //    }
        //    FillValue(groupedResults);
        //}
        // Dictionary to keep track of columns by their NameID
        private Dictionary<string, DataGridColumn> _columnMap = new Dictionary<string, DataGridColumn>();

        public WinChart(List<SensorGroup> groupedResults)
        {
            InitializeComponent();

            //FillValue(groupedResults);
            // Set up columns first (UI Thread)
            SetupColumns(groupedResults);

            // Load the data (Async)
            //_ = FillValueAsync(groupedResults);

            // Load your saved visibility settings
            //LoadAndApplySettings();
            LoadAndApplySettings();
        }

        private void SetupColumns(List<SensorGroup> groupedResults)
        {
            // Clear existing UI elements to prevent duplicates
            dataGrid.Columns.Clear();
            ColumnTogglePanel.Children.Clear();
            _columnMap.Clear();

            foreach (var group in groupedResults)
            {
                string sensorName = group.NameID;

                // 1. Create the DataGrid Column
                // This tells the grid: "Look for a key named 'sensorName' in the row object"
                var column = new DataGridTextColumn
                {
                    Header = sensorName,
                    Binding = new Binding(sensorName), // Matches the ExpandoObject key
                    Visibility = Visibility.Visible,
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star) // Optional: evenly space columns
                };

                dataGrid.Columns.Add(column);

                // Save reference for the Toggle/Save/Load logic
                _columnMap[sensorName] = column;

                // 2. Create the CheckBox for user selection
                var checkBox = new CheckBox
                {
                    Content = sensorName,
                    IsChecked = true,
                    Margin = new Thickness(10, 5, 10, 5),
                    Tag = sensorName
                };

                checkBox.Checked += ToggleColumnVisibility;
                checkBox.Unchecked += ToggleColumnVisibility;

                ColumnTogglePanel.Children.Add(checkBox);
            }
        }

        private async Task FillValueAsync(List<SensorGroup> groupedResults)
        {
            // 1. Show a loading state (optional)
            dataGrid.Cursor = Cursors.Wait;

            // 2. Perform heavy calculations on a background thread
            var processedRows = await Task.Run(() =>
            {
                // Find the maximum number of rows needed
                int maxRows = groupedResults.Max(g => g.ChartPoints.Count);

                // Pre-allocate the list capacity to avoid memory re-allocations
                var rows = new List<dynamic>(maxRows);

                for (int i = 0; i < maxRows; i++)
                {
                    IDictionary<string, object> row = new ExpandoObject();

                    foreach (var group in groupedResults)
                    {
                        if (!NameVisible.Contains(group.NameID))
                            continue;
                        // Accessing index is O(1), very fast
                        if (i < group.ChartPoints.Count)
                        {
                            row[group.NameID] = group.ChartPoints[i].Value;
                        }
                        else
                        {
                            row[group.NameID] = null;
                        }
                    }
                    rows.Add(row);
                }
                return rows;
            });

            // 3. Back on the UI thread, update the grid
            dataGrid.ItemsSource = processedRows;
            dataGrid.Cursor = Cursors.Arrow;
        }

        private void ToggleColumnVisibility(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.Tag is string sensorName)
            {
                if (_columnMap.TryGetValue(sensorName, out var column))
                {
                    column.Visibility = cb.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }
        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox cb in ColumnTogglePanel.Children.OfType<CheckBox>())
                cb.IsChecked = true;
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox cb in ColumnTogglePanel.Children.OfType<CheckBox>())
                cb.IsChecked = false;
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

        private string _settingsPath = "column_settings.json";

        HashSet<string> NameVisible = new HashSet<string>();
        // Call this at the END of your Constructor
        private void LoadAndApplySettings()
        {
            if (!File.Exists(_settingsPath)) return;

            try
            {
                string json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<UserSettings>(json);

                if (settings?.VisibleColumns == null) return;

                // Iterate through our CheckBoxes and update them
                foreach (CheckBox cb in ColumnTogglePanel.Children.OfType<CheckBox>())
                {
                    string sensorName = cb.Tag.ToString();
                    // If the sensor name is in our saved list, check it; otherwise, uncheck it
                    cb.IsChecked = settings.VisibleColumns.Contains(sensorName);

                    // Explicitly call the toggle logic since IsChecked might not trigger if value is same
                    UpdateColumnVisibility(sensorName, cb.IsChecked == true);

                    if (cb.IsChecked ?? true)
                        NameVisible.Add(sensorName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load settings: {ex.Message}");
            }
        }

        public void SaveSettings()
        {
            var settings = new UserSettings();

            // Grab the names of all currently checked CheckBoxes
            settings.VisibleColumns = ColumnTogglePanel.Children
                .OfType<CheckBox>()
                .Where(cb => cb.IsChecked == true)
                .Select(cb => cb.Tag.ToString())
                .ToList();

            string json = JsonSerializer.Serialize(settings);
            File.WriteAllText(_settingsPath, json);
        }

        // Helper to keep logic DRY (Don't Repeat Yourself)
        private void UpdateColumnVisibility(string sensorName, bool isVisible)
        {
            if (_columnMap.TryGetValue(sensorName, out var column))
            {
                column.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

                // OPTIONAL: If you want to hide the chart line too:
                //var series = Series.FirstOrDefault(s => s.Name == sensorName);
                //if (series != null) series.IsVisible = isVisible;
            }
        }

        private void Button_Save_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }
    }
}
