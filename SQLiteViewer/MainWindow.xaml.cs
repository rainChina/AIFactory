using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using System.Text;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using LiveChartsCore.Kernel;
using System.Linq;
using System.Collections.ObjectModel;
using System.IO;

namespace SQLiteViewer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    public MainWindow()
    {
        InitializeComponent();
        lvcChart.DataContext = this;
        FilteredSeries = new ObservableCollection<ISeries>();
    }
    private async void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Filter = "SQLite Database (*.db)|*.db|All files (*.*)|*.*"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            string filePath = openFileDialog.FileName;
            await LoadDataAsync(filePath);
            //LoadData(filePath);
        }
    }

    private void LoadJsonData(string dbPath)
    {
        var items = new List<dynamic>();

        try
        {
            using (var connection = new SqliteConnection($"Data Source={dbPath};"))
            {
                connection.Open();
                string query = "SELECT id, JsonData FROM RealTimeData"; // 修改为你的表名和字段名

                using (var command = new SqliteCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {

                        items.Add(new
                        {
                            Id = reader["id"],
                            Json = reader["JsonData"]
                        });
                    }
                }
            }

            //dataGrid.ItemsSource = items;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading data: {ex.Message}");
        }
    }


    private async Task LoadData(string dbPath)
    {
        var items = new List<dynamic>();

        try
        {
            using (var connection = new SqliteConnection($"Data Source={dbPath};"))
            {
                connection.Open();
                string query = "SELECT id, JsonData FROM RealTimeData"; // 修改为你的表名和字段名

                using (var command = new SqliteCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {

                        items.Add(new
                        {
                            Id = reader["id"],
                            Json = reader["JsonData"]
                        });
                    }
                }
            }

            //dataGrid.ItemsSource = items;

            // Now groupedResults is List<SensorGroup>, no more 'dynamic' headache!
            List<SensorGroup> groupedResults = items
                .Select(row => JsonNode.Parse((string)row.Json))
                .GroupBy(node => node["NameID"]?.ToString())
                .Select(group => new SensorGroup
                {
                    NameID = group.Key ?? "Unknown",
                    ChartPoints = group.Select(node => new SensorDataPoint
                    {
                        Time = DateTime.Parse(node["TimeRefresh"]?.ToString() ?? DateTime.Now.ToString()),
                        Value = ConvertToDouble(ExtractSmartValue(node["Value"]))
                    }).ToList()
                })
                .ToList();

            await LoadChartAsync(groupedResults);

            WinChart charWin = new WinChart(groupedResults);
            charWin.Show();

        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading data: {ex.Message}");
        }
    }

    // This property should be bound to your Chart in XAML
    private ISeries[] _series;
    private Axis[] _xAxes;

    //public ISeries[] Series
    //{
    //    get => _series;
    //    set
    //    {
    //        _series = value;
    //        OnPropertyChanged();
    //    }
    //}

    public Axis[] XAxes
    {
        get => _xAxes;
        set
        {
            _xAxes = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void LoadChart1(List<SensorGroup> groupedResults)
    {
        var seriesList = new List<ISeries>();

        // This is now perfectly type-safe
        foreach (SensorGroup group in groupedResults)
        {
            seriesList.Add(new LineSeries<DateTimePoint>
            {
                Name = group.NameID,
                Values = group.ChartPoints
                    .Select(p => new DateTimePoint(p.Time, p.Value))
                    .ToList(),
                Fill = null,
                GeometrySize = 5
            });
        }
        Series = seriesList.ToArray();

        // Configure the X-Axis to show time labels
        XAxes = new Axis[]
        {
        new Axis
        {
            Labeler = value => new DateTime((long)value).ToString("HH:mm:ss"),
            LabelsRotation = 15,
            UnitWidth = TimeSpan.FromSeconds(1).Ticks,
            MinStep = TimeSpan.FromSeconds(1).Ticks
        }
        };
    }
    private void LoadChart(List<SensorGroup> groupedResults)
    {
        if (groupedResults == null || !groupedResults.Any()) return;

        var seriesList = new List<ISeries>();

        foreach (var group in groupedResults)
        {
            // FIX 1: Ensure data is sorted by Time. 
            // LiveCharts draws points in the order they appear in the list.
            // Out-of-order data creates "spider-web" lines.
            var sortedPoints = group.ChartPoints
                .OrderBy(p => p.Time)
                .Select(p => new DateTimePoint(p.Time, p.Value))
                .ToList();

            seriesList.Add(new LineSeries<DateTimePoint>
            {
                Name = group.NameID,
                Values = sortedPoints,
                Fill = null,
                GeometrySize = 5
            });
        }

        // FIX 3: Assign to properties that trigger OnPropertyChanged()
        // Make sure your Series and XAxes properties call OnPropertyChanged in their setters!
        Series = seriesList.ToArray();

        XAxes = new Axis[]
        {
        new Axis
        {
            // FIX 4: Use the built-in DateTime format helper for safer scaling
            Labeler = value => value <= 0 ? "" : new DateTime((long)value).ToString("HH:mm:ss"),
            LabelsRotation = 15,
            
            // UnitWidth and MinStep are crucial for DateTime axes
            UnitWidth = TimeSpan.FromSeconds(1).Ticks,
            MinStep = TimeSpan.FromSeconds(1).Ticks
        }
        };
    }


    public async Task LoadChartAsync(List<SensorGroup> groupedResults)
    {
        if (groupedResults == null || !groupedResults.Any()) return;

        // 1. Move the heavy mapping to a background thread
        var result = await Task.Run(() =>
        {
            var seriesList = new List<ISeries>();

            foreach (var group in groupedResults)
            {
                // Sorting and mapping thousands of points is CPU intensive
                var chartPoints = group.ChartPoints
                    .OrderBy(p => p.Time)
                    .Select(p => new DateTimePoint(p.Time, p.Value))
                    .ToList();

                seriesList.Add(new LineSeries<DateTimePoint>
                {
                    Name = group.NameID,
                    Values = chartPoints,
                    Fill = null,
                    GeometrySize = 0, // OPTIMIZATION: GeometrySize 0 draws MUCH faster
                    LineSmoothness = 0 // OPTIMIZATION: Disabling curves saves CPU
                });
            }

            var xAxes = new Axis[]
            {
            new Axis
            {
                Labeler = value => value <= 0 ? "" : new DateTime((long)value).ToString("HH:mm:ss"),
                LabelsRotation = 15,
                UnitWidth = TimeSpan.FromSeconds(1).Ticks,
                MinStep = TimeSpan.FromSeconds(1).Ticks
            }
            };

            return new { Series = seriesList.ToArray(), Axes = xAxes };
        });

        // 2. Return to the UI thread to update the bound properties
        // This triggers the INotifyPropertyChanged events
        this.Series = result.Series;
        this.XAxes = result.Axes;
    }


    // Helper to ensure values are numeric for the chart
    private double ConvertToDouble(object val)
    {
        if (val is bool b) return b ? 1.0 : 0.0;
        if (val is int i) return (double)i;
        if (val is double d) return d;
        if (double.TryParse(val?.ToString(), out double parsed)) return parsed;
        return 0;
    }

    public static object ExtractSmartValue(JsonNode node)
    {
        if (node == null) return null;
        string text = node.ToString();
        if (bool.TryParse(text, out bool b)) return b;
        if (int.TryParse(text, out int i)) return i;
        if (double.TryParse(text, out double d)) return d;
        return text;
    }

    private void ChartView_Click(object sender, RoutedEventArgs e)
    {

    }

    private async void DataView_Click(object sender, RoutedEventArgs e)
    {
      await FExportSensorGroupsForLstmAsync(GroupedCheckResults);
    }

    public Dictionary<string, SensorStatistics> StatisticsDict { get; set; } = new();

    private ObservableCollection<SensorStatistics> _statisticsList = new ObservableCollection<SensorStatistics>();
    public ObservableCollection<SensorStatistics> StatisticsList
    {
        get => _statisticsList;
    }

    private async Task LoadDataAsync(string dbPath)
    {
        var items = new List<dynamic>();

        try
        {
            using (var connection = new SqliteConnection($"Data Source={dbPath};"))
            {
                connection.Open();
                string query = "SELECT id, JsonData FROM RealTimeData";

                using (var command = new SqliteCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new
                        {
                            Id = reader["id"],
                            Json = reader["JsonData"]
                        });
                    }
                }
            }

            //dataGrid.ItemsSource = items;

            List<SensorGroup> groupedResults = items
                .Select(row => JsonNode.Parse((string)row.Json))
                .GroupBy(node => node["NameID"]?.ToString())
                .Select(group => new SensorGroup
                {
                    NameID = group.Key ?? "Unknown",
                    ChartPoints = group.Select(node => new SensorDataPoint
                    {
                        Time = DateTime.Parse(node["TimeRefresh"]?.ToString() ?? DateTime.Now.ToString()),
                        Value = ConvertToDouble(ExtractSmartValue(node["Value"]))
                    }).ToList()
                })
                .ToList();

            // --- Statistics Calculation ---
            var statsBuilder = new StringBuilder();
            StatisticsList.Clear();
            GroupedCheckResults.Clear();
            foreach (var group in groupedResults)
            {
                if (group.ChartPoints.Count > 0)
                {
                    var min = group.ChartPoints.Min(p => p.Value);
                    var max = group.ChartPoints.Max(p => p.Value);
                    var start = group.ChartPoints.Min(p => p.Time);
                    var end = group.ChartPoints.Max(p => p.Time);
                    var duration = end - start;

                    statsBuilder.AppendLine(
                        $"Group: {group.NameID}\n" +
                        $"  Time Range: {start:yyyy-MM-dd HH:mm:ss} - {end:yyyy-MM-dd HH:mm:ss}\n" +
                        $"  Duration: {duration}\n" +
                        $"  Min: {min}\n" +
                        $"  Max: {max}\n");

                    GroupedCheckResults.Add(group);
                }
            }
            //MessageBox.Show(statsBuilder.ToString(), "Data Statistics");
            // --- End Statistics ---

            // After groupedResults is created
            StatisticsDict.Clear();
            var statsList = new List<SensorStatistics>();

            foreach (var group in groupedResults)
            {
                if (group.ChartPoints.Count > 0)
                {
                    var min = group.ChartPoints.Min(p => p.Value);
                    var max = group.ChartPoints.Max(p => p.Value);
                    var avg = group.ChartPoints.Average(p => p.Value);
                    var start = group.ChartPoints.Min(p => p.Time);
                    var end = group.ChartPoints.Max(p => p.Time);

                    var stats = new SensorStatistics
                    {
                        NameID = group.NameID,
                        StartTime = start,
                        EndTime = end,
                        Min = min,
                        Max = max,
                        Average = avg,
                        Count = group.ChartPoints.Count
                    };
                    StatisticsDict[group.NameID] = stats;
                    statsList.Add(stats);
                    StatisticsList.Add(stats);
                }
            }
            statsGrid.ItemsSource = StatisticsList;

            await LoadChartNormalizeAsync(groupedResults);

        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading data: {ex.Message}");
        }
    }


    List<SensorGroup> GroupedCheckResults = new List<SensorGroup>();
    public async Task LoadChartNormalizeAsync(List<SensorGroup> groupedResults)
    {
        if (groupedResults == null || !groupedResults.Any()) return;

        // 1. Move the heavy mapping and normalization to a background thread
        var result = await Task.Run(() =>
        {
            var seriesList = new List<ISeries>();

            foreach (var group in groupedResults)
            {
                if (group.ChartPoints == null || !group.ChartPoints.Any()) continue;

                // Find min and max for this specific group to normalize
                double min = group.ChartPoints.Min(p => p.Value);
                double max = group.ChartPoints.Max(p => p.Value);
                double range = max - min;

                if (range < double.Epsilon)
                {
                    continue;
                }

                // Sorting and mapping with Normalization logic
                var chartPoints = group.ChartPoints
                    .OrderBy(p => p.Time)
                    .Select(p =>
                    {
                        // Avoid division by zero if all values are the same
                        double normalizedValue = range > 0
                            ? (p.Value - min) / range
                            : 0;

                        return new DateTimePoint(p.Time, normalizedValue);
                    })
                    .ToList();

                seriesList.Add(new LineSeries<DateTimePoint>
                {
                    Name = group.NameID,
                    Values = chartPoints,
                    Fill = null,
                    GeometrySize = 0,
                    LineSmoothness = 0
                });
            }

            var xAxes = new Axis[]
            {
            new Axis
            {
                Labeler = value => value <= 0 ? "" : new DateTime((long)value).ToString("HH:mm:ss"),
                LabelsRotation = 15,
                UnitWidth = TimeSpan.FromSeconds(1).Ticks,
                MinStep = TimeSpan.FromSeconds(1).Ticks
            }
            };

            return new { Series = seriesList.ToArray(), Axes = xAxes };
        });

        // 2. Return to the UI thread to update the bound properties
        this.Series = result.Series;
        this.XAxes = result.Axes;
    }


    // Add these fields/properties to your MainWindow class
    public ObservableCollection<string> TraceNames { get; set; } = new();
    public ObservableCollection<ISeries> FilteredSeries { get; set; } = new();
    private ISeries[] _allSeries;

    public ISeries[] Series
    {
        get => _allSeries;
        set
        {
            _allSeries = value;
            TraceNames.Clear();
            TraceSelector.ItemsSource = TraceNames;
            if (_allSeries != null)
            {
                foreach (var s in _allSeries)
                    TraceNames.Add(s.Name);
            }
            // Select all by default
            TraceSelector.SelectedItems.Clear();
            foreach (var name in TraceNames)
                TraceSelector.SelectedItems.Add(name);
            UpdateFilteredSeries();
            OnPropertyChanged();
        }
    }

    private void UpdateFilteredSeries()
    {
        if (_allSeries == null) return;
        FilteredSeries.Clear();
        var selectedNames = TraceSelector.SelectedItems.Cast<string>().ToList();
        foreach (var s in _allSeries)
        {
            if (selectedNames.Contains(s.Name))
                FilteredSeries.Add(s);
        }
        OnPropertyChanged(nameof(FilteredSeries));
    }


    private void TraceSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateFilteredSeries();
    }



    public async Task ExportSensorGroupForLstmAsync(List<SensorGroup> groupedResults)
    {
        // 1. Let user select a sensor group
        var sensorNames = groupedResults.Select(g => g.NameID).ToList();
        var selectWindow = new Window
        {
            Title = "Select Sensor Group",
            Width = 300,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this
        };
        var listBox = new ListBox { ItemsSource = sensorNames, Margin = new Thickness(10) };
        var okButton = new Button { Content = "OK", IsDefault = true, Margin = new Thickness(10) };
        okButton.Click += (s, e) => selectWindow.DialogResult = true;
        var panel = new StackPanel();
        panel.Children.Add(listBox);
        panel.Children.Add(okButton);
        selectWindow.Content = panel;

        if (selectWindow.ShowDialog() != true || listBox.SelectedItem == null)
            return;

        string selectedName = listBox.SelectedItem.ToString();
        var group = groupedResults.FirstOrDefault(g => g.NameID == selectedName);
        if (group == null)
        {
            MessageBox.Show("Sensor group not found.");
            return;
        }

        // 2. Ask user where to save
        var saveDialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            FileName = $"{selectedName}_lstm.csv"
        };
        if (saveDialog.ShowDialog() != true)
            return;

        // 3. Write CSV
        var sb = new StringBuilder();
        sb.AppendLine("Time,Value");
        foreach (var point in group.ChartPoints)
        {
            sb.AppendLine($"{point.Time:O},{point.Value}");
        }
        await File.WriteAllTextAsync(saveDialog.FileName, sb.ToString());

        MessageBox.Show($"Exported {group.ChartPoints.Count} rows to {saveDialog.FileName}", "Export Complete");
    }

    public async Task FExportSensorGroupsForLstmAsync(List<SensorGroup> groupedResults)
    {
        // 1. Let user select one or more sensor groups
        var sensorNames = groupedResults.Select(g => g.NameID).ToList();
        var selectWindow = new Window
        {
            Title = "Select Sensor Groups",
            Width = 300,
            Height =450,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this
        };
        var listBox = new ListBox
        {
            ItemsSource = sensorNames,
            Margin = new Thickness(10),
            SelectionMode = SelectionMode.Extended
        };
        var okButton = new Button { Content = "OK", IsDefault = true, Margin = new Thickness(10) };
        okButton.Click += (s, e) => selectWindow.DialogResult = true;
        var panel = new StackPanel();
        var scrollViewer = new ScrollViewer
        {
            Content = listBox,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalAlignment = VerticalAlignment.Stretch,
            Height = 350
        };
        panel.Children.Add(scrollViewer);
        panel.Children.Add(okButton);
        selectWindow.Content = panel;

        if (selectWindow.ShowDialog() != true || listBox.SelectedItems.Count == 0)
            return;

        // 2. Ask user for a folder to save all CSVs
        var dlg = new OpenFolderDialog();
        if (dlg.ShowDialog() != DialogResult)
            return;
        string folder = dlg.FolderName;

        // 3. Export each selected group
        int totalExported = 0;
        foreach (string selectedName in listBox.SelectedItems)
        {
            var group = groupedResults.FirstOrDefault(g => g.NameID == selectedName);
            if (group == null) continue;

            var sb = new StringBuilder();
            sb.AppendLine("Time,Value");
            foreach (var point in group.ChartPoints)
            {
                sb.AppendLine($"{point.Time:O},{point.Value}");
            }
            string filePath = System.IO.Path.Combine(folder, $"{selectedName}_lstm.csv");
            await File.WriteAllTextAsync(filePath, sb.ToString());
            totalExported++;
        }

        MessageBox.Show($"Exported {totalExported} file(s) to {folder}", "Export Complete");
    }


}

public class MyDataPoint
{
    public DateTime Time { get; set; }
    public double Value { get; set; }
}

public class SensorDataPoint
{
    public DateTime Time { get; set; }
    public double Value { get; set; }
}

public class SensorGroup
{
    public string NameID { get; set; }
    public List<SensorDataPoint> ChartPoints { get; set; }
}

public class SensorStatistics
{
    public string NameID { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public double Min { get; set; }
    public double Max { get; set; }
    public double Average { get; set; }
    public int Count { get; set; }
}
