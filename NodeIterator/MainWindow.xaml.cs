using AIFactory.Model;
using AIFactory.Util;
using HandyControl.Tools.Extension;
using Microsoft.Win32;
using Opc.Ua;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;

namespace NodeIterator;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    ObservableCollection<NodeAttribute> nodeAttributes = new ObservableCollection<NodeAttribute>();
    private void Load_Click(object sender, RoutedEventArgs e)
    {
        var nAttributes = IterateDescription();
        nodeAttributes.Clear();
        nodeAttributes.AddRange(nAttributes);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog
        {
            Filter = "XML files (*.xml)|*.xml",
            Title = "Save XML File",
            FileName = "data.xml"
        };

        if (saveFileDialog.ShowDialog() is true)
        {
            string filePath = saveFileDialog.FileName;

            var serializer = new XmlSerializer(typeof(ObservableCollection<NodeAttribute>));
            using (var writer = new StreamWriter(filePath))
            {
                serializer.Serialize(writer, nodeAttributes);
            }


            MessageBox.Show("File saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void Connect_Click(object sender, RoutedEventArgs e)
    {
        var nAttributes = IterateDescription();
        nodeAttributes.Clear();
        nodeAttributes.AddRange(nAttributes);
    }
    
    HashSet<string> uniqueLines = new HashSet<string>();
    private List<NodeAttribute> IterateDescription()
    {
        List<NodeAttribute> nodeAttributes = new List<NodeAttribute>();

        // Create OpenFileDialog
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
        };

        // Show dialog and check if user selected a file
        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                // Read all lines into a HashSet
                HashSet<string> lines = new HashSet<string>(File.ReadAllLines(openFileDialog.FileName));

                // Example: Display count
                MessageBox.Show($"Loaded {lines.Count} unique lines.");

                PLCOPCManager plcManager = new PLCOPCManager("");

                var nodes = plcManager.LoopNode("");
                foreach (var node in nodes)
                {
                    if (!string.IsNullOrEmpty(node.Key) && uniqueLines.Contains(node.Key))
                    {
                        nodeAttributes.Add(new NodeAttribute
                        {
                            NodeId = node.Key,
                            NodeName = node.Value.NodeName,
                            NodeDeisplayName = node.Value.NodeDeisplayName,
                            NodeDescription = node.Value.NodeDescription,
                            NodeDataType = node.Value.NodeDataType.ToString()
                        });
                    }
                }

                plcManager.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading file: {ex.Message}");
            }


        }
        return nodeAttributes;

    }


    private List<NodeAttribute> IterateAllNodes()
    {
        var nodeAttributes = new List<NodeAttribute>();
        PLCOPCManager plcManager = new PLCOPCManager("");

        var nodes = plcManager.LoopNode("");
        foreach (var node in nodes)
        {
            if (!string.IsNullOrEmpty(node.Key))
            {
                nodeAttributes.Add(new NodeAttribute
                {
                    NodeId = node.Key,
                    NodeName = node.Value.NodeName,
                    NodeDeisplayName = node.Value.NodeDeisplayName,
                    NodeDescription = node.Value.NodeDescription,
                    NodeDataType = node.Value.NodeDataType.ToString()
                });
            }
        }

        plcManager.Close();

        return nodeAttributes;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var nAttributes = IterateAllNodes();
        nodeAttributes.Clear();
        nodeAttributes.AddRange(nAttributes);
    }
    private void Filter_Click(object sender, RoutedEventArgs e)
    {
        var nAttributes = IterateAllNodes();
        nodeAttributes.Clear();
        nodeAttributes.AddRange(nAttributes);
    }
}