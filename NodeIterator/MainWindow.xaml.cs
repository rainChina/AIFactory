using AIFactory.Model;
using AIFactory.Util;
using Microsoft.Win32;
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

    HashSet<string> uniqueLines = new HashSet<string>();
    private List<string> IterateDescription()
    {
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

                var nodes = plcManager.LoopNodeInfo();
                List<NodeAttribute> nodeAttributes = new List<NodeAttribute>();
                foreach (var node in nodes)
                {
                    if (!string.IsNullOrEmpty(node.Key))
                    {
                        if(uniqueLines.Contains(node.Key))
                            nodeAttributes.add(new NodeAttribute
                            {
                                NodeId = node.Key,
                                NodeName = node.Value.NodeName,
                                NodeDeisplayName = node.Value.NodeDeisplayName,
                                NodeDescription = node.Value.NodeDescription,
                                NodeDataType = node.Value.NodeDataType.ToString()
                            });
                    }
                }

                var serializer = new XmlSerializer(typeof(List<NodeAttribute>));
                using (var writer = new StreamWriter("people.xml"))
                {
                    serializer.Serialize(writer, nodeAttributes);
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading file: {ex.Message}");
            }
        }

    }

}