using AIFactory.Model;
using HandyControl.Data;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json.Linq;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace AIFactory.Util
{
    public class PLCOPCManager
    {
        private string _opcIPAddress;
        private int _opcPort;
        private Session _opcSession;
        public PLCOPCManager(string opcAddressIp, int opcPort = 4840)
        {
            _opcIPAddress = opcAddressIp;
            _opcPort = opcPort;

        }
        public async Task<bool> Connect()
        {
            bool blRes = true;
            var config = new ApplicationConfiguration()
            {
                ApplicationName = "OpcUaClientDemo",
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = "CertificateStores/MachineDefault",
                        SubjectName = "CN=OpcUaClientDemo, O=YourOrganization, C=US"
                    },
                    AutoAcceptUntrustedCertificates = true,
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "CertificateStores/UA Certificate Authorities"
                    },
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "CertificateStores/UA Applications"
                    },
                    RejectedCertificateStore = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "CertificateStores/RejectedCertificates"
                    }
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 }
            };

            await config.Validate(ApplicationType.Client);

            // 2. Create the application instance
            var app = new ApplicationInstance
            {
                ApplicationName = "OpcUaClientDemo",
                ApplicationType = ApplicationType.Client,
                ApplicationConfiguration = config
            };

            // Replace the obsolete method call with the updated method
            bool haveAppCertificate = await app.CheckApplicationInstanceCertificates(false, 12);
            if (!haveAppCertificate)
            {
                throw new Exception("Application instance certificate invalid!");
            }


            // 3. Connect to the OPC UA server
            var endpointURL = "opc.tcp://192.168.0.1:4840"; // Replace with your PLC's endpoint
                                                            // Replace the following line:
                                                            // var selectedEndpoint = CoreClientUtils.SelectEndpoint(endpointURL, false);

            if (!string.IsNullOrEmpty(_opcIPAddress))
            {
                endpointURL = string.Format("opc.tcp://{0}:{1}", _opcIPAddress, _opcPort);
            }


            // 3. Connect to the OPC UA server
            var selectedEndpoint = CoreClientUtils.SelectEndpoint(config, endpointURL, false);
            var endpointConfig = EndpointConfiguration.Create(config);
            var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfig);

            // 4. Provide user credentials
            var userName = "user1"; // Replace with your username
            var password = "654321"; // Replace with your password
            var userIdentity = new UserIdentity(userName, password);

            _opcSession = await Opc.Ua.Client.Session.Create(config, endpoint, false, "MySession", 60000, userIdentity, null);

            if (_opcSession == null)
            {
                blRes = false;
            }

            return blRes;
        }

        public bool Read()
        {
            bool blRes = false;

            // 4. Read a value
            var readNodeId = new NodeId("ns=4;i=87"); // Replace with your variable NodeId
            DataValue readValue = _opcSession.ReadValue(readNodeId);
            Console.WriteLine($"Read Value: {readValue.Value}");

            readNodeId = new NodeId("ns=4;i=114"); // Replace with your variable NodeId
            readValue = _opcSession.ReadValue(readNodeId);
            Console.WriteLine($"Read Value: {readValue.Value}");


            //// Browse to find the NodeId by name
            //var browser = new Browser(_opcSession);
            //browser.BrowseDirection = BrowseDirection.Forward;
            //browser.NodeClassMask = (int)NodeClass.Variable;
            //browser.ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences;

            //var references = browser.Browse(Objects.Server); // Start browsing from the Server object

            //foreach (var reference in references)
            //{
            //    if (reference.DisplayName.Text == nodeName)
            //    {
            //        var nodeId = reference.NodeId;
            //        var value = session.ReadValue(nodeId);
            //        Console.WriteLine($"Value of {nodeName}: {value}");
            //        break;
            //    }
            //}

            //session.Close();


            return blRes;

        }


        private DataPoint _dataPoint = new DataPoint();

        public DataPoint ReadDatabyNodeID(string nodeID)
        {
            var readNodeId = new NodeId(nodeID); // Replace with your variable NodeId
            DataValue readValue = _opcSession.ReadValue(readNodeId);

            if (readValue.StatusCode != StatusCodes.Good)
            {
                return null;
            }

            _dataPoint.TimeLabel = readValue.SourceTimestamp.ToLocalTime();
            _dataPoint.DataValue = (double)readValue.Value;
            return _dataPoint;

        }

        private void DispatcherNofication(string message)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>

            {
                HandyControl.Controls.Growl.Info(message);
            });

        }


        public bool Write(string nodeName, object valueToWrite)
        {
            // Browse to find the NodeId by name
            var browser = new Browser(_opcSession)
            {
                BrowseDirection = BrowseDirection.Forward,
                NodeClassMask = (int)NodeClass.Variable,
                ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences
            };

            var references = browser.Browse(Objects.Server);

            foreach (var reference in references)
            {
                if (reference.DisplayName.Text == nodeName)
                {
                    var nodeId = reference.NodeId;

                    // Create a WriteValue
                    var writeValue = new WriteValue
                    {
                        NodeId = (NodeId)nodeId,
                        AttributeId = Attributes.Value,
                        Value = new DataValue(new Variant(valueToWrite))
                    };

                    // Write to the node
                    var writeResult = _opcSession.Write(null, new WriteValueCollection { writeValue }, out StatusCodeCollection results, out DiagnosticInfoCollection diagnosticInfos);

                    if (StatusCode.IsGood(results[0]))
                    {
                        Console.WriteLine($"Successfully wrote value to {nodeName}");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to write value: {results[0]}");
                    }

                    break;
                }
            }

            return true;
        }

        public void Close()
        {
            if (_opcSession == null)
                return;

            _opcSession.Close();
            _opcSession.Dispose();
        }

        public List<NodeAttribute> LoadPLCNodeConfig(string filePath)
        {
            List<NodeAttribute> nodeAvailable;
            XmlSerializer serializer = new XmlSerializer(typeof(List<NodeAttribute>));
            using (TextReader reader = new StreamReader(filePath))
            {
                var res = serializer.Deserialize(reader) as List<NodeAttribute>;

                if (res != null)
                {
                    nodeAvailable = res;
                }
                else
                {
                    nodeAvailable = new List<NodeAttribute>();
                }
                return nodeAvailable;
            }

        }

        private List<NodeAttribute> _nodesAttribute;

        public List<NodeAttribute> NodesAttribute
        {
            get { return _nodesAttribute; }
            set { _nodesAttribute = value; }
        }


        public SensorData ReadData()
        {
            //using var db = new SensorContext();
            //db.Database.EnsureCreated();

            var data = new SensorData
            {
                Timestamp = DateTime.UtcNow,
                Temperature = new Random().NextDouble() * 100,
                Status = "OK"
            };

            foreach (var node in _nodesAttribute)
            {
                var dp = ReadDatabyNodeID(nodeID: node?.NodeId);
                if (dp != null)
                {
                    //switch (node.DataType.ToLower())
                    //{
                    //    case "double":
                    //        data.Temperature = dp.DataValue;
                    //        break;
                    //    case "string":
                    //        data.Status = dp.DataValue.ToString();
                    //        break;
                    //    default:
                    //        break;
                    //}

                    var prop = typeof(SensorData).GetProperty(node.NodeName);
                    if (prop != null)
                        prop.SetValue(data, Convert.ChangeType(dp.DataValue, prop.PropertyType));


                }
            }



            //db.SensorRecords.Add(data);
            //db.SaveChanges();

            return data;
        }

        public SensorData ReadSensorData()
        {

            if (NodesAttribute == null)
            {
                NodesAttribute = LoadPLCNodeConfig("Config/PLCNodeConfig.xml");
            }

            var data = new SensorData
            {
                Timestamp = DateTime.UtcNow,
                Temperature = new Random().NextDouble() * 100,
                Status = "OK"
            };

            foreach (var node in _nodesAttribute)
            {
                var dp = ReadDatabyNodeID(nodeID: node?.NodeId);
                if (dp != null)
                {
                    //switch (node.DataType.ToLower())
                    //{
                    //    case "double":
                    //        data.Temperature = dp.DataValue;
                    //        break;
                    //    case "string":
                    //        data.Status = dp.DataValue.ToString();
                    //        break;
                    //    default:
                    //        break;
                    //}

                    var prop = typeof(SensorData).GetProperty(node.NodeName);
                    if (prop != null)
                        prop.SetValue(data, Convert.ChangeType(dp.DataValue, prop.PropertyType));


                }
            }

            return data;
        }

        public Dictionary<string, NodeAttribute> LoopNodeInfo()
        {
            Dictionary<string, NodeAttribute> dicNode = new Dictionary<string, NodeAttribute>();
            if (NodesAttribute == null)
            {
                NodesAttribute = LoadPLCNodeConfig("Config/PLCNodeConfig.xml");
            }
            foreach (var node in _nodesAttribute)
            {
                var dp = ReadDatabyNodeID(nodeID: node?.NodeId);
                if (dp != null)
                {
                    dicNode.Add(node.NodeName, node);
                }
            }
            return dicNode;

        }

        public Dictionary<string, NodeAttribute> LoopNode(string nodeName)
        {
            Dictionary<string, NodeAttribute> dicNode = new Dictionary<string, NodeAttribute>();
            if (_opcSession == null)
                return dicNode;
            // Start browsing from the Objects folder
            NodeId rootNodeId = new NodeId(nodeName);
            dicNode = RecursiveNodeDictionary(_opcSession, rootNodeId, dicNode);
            return dicNode;
        }


        static Dictionary<string, NodeAttribute> RecursiveNodeDictionary(Session session, NodeId rootNodeId, Dictionary<string,NodeAttribute> nodeInfoDic)
        {
            var nodeDict = new Dictionary<string, NodeAttribute>();
            ReferenceDescriptionCollection references;
            byte[] continuationPoint;

            session.Browse(
                null,
                null,
                rootNodeId,
                0,
                BrowseDirection.Forward,
                ReferenceTypeIds.HierarchicalReferences,
                true,
                (uint)NodeClass.Object | (uint)NodeClass.Variable,
                out continuationPoint,
                out references
            );

            foreach (var rd in references)
            {
                NodeId childNodeId = ExpandedNodeId.ToNodeId(rd.NodeId, session.NamespaceUris);

                // Read the Description attribute
                var readValueId = new ReadValueId
                {
                    NodeId = childNodeId,
                    AttributeId = Attributes.Description
                };

                //DataValueCollection dataValues;
                //var results = session.Read(null, 0, TimestampsToReturn.Neither, new ReadValueIdCollection { readValueId }, out dataValues, out _);
                
                DataValueCollection descCollection;

                var readResponse = session.Read(
                   null,
                   0,
                   TimestampsToReturn.Neither,
                   new ReadValueIdCollection { readValueId },
                   out descCollection,
                   out _);

                if (StatusCode.IsGood(readResponse.ServiceResult) && descCollection.Count > 0)
                    //if (dataValues != null &&  dataValues.Count > 0)
                {
                    string nDescription = descCollection[0].Value?.ToString() ?? "";
                    NodeAttribute ndInfo = new NodeAttribute()
                    {
                        NodeId = rd.NodeId.ToString(),
                        NodeName = rd.DisplayName.Text,
                        NodeDeisplayName = rd.DisplayName.Text,
                        NodeDescription = descCollection[0].Value?.ToString() ?? "N/A",
                        NodeDataType = rd.TypeDefinition?.ToString() ?? "N/A"
                    };
                    if(string.IsNullOrEmpty(nDescription) == false && !nodeInfoDic.ContainsKey(nDescription))
                    {
                        nodeDict.Add(ndInfo.NodeId, ndInfo);
                    }
                }
                else
                {
                    RecursiveNodeDictionary(session, childNodeId, nodeInfoDic);
                }

                //var description = dataValues[0].Value?.ToString() ?? "N/A";

                //Console.WriteLine($"{new string(' ', indent * 2)}Name: {rd.DisplayName.Text}, Description: {description}");

                //// Recurse into child nodes
                //BrowseRecursive(session, childNodeId, indent + 1);
            }
            return nodeDict;
        }


        static void BrowseRecursive(Session session, NodeId nodeId, int indent)
        {
            ReferenceDescriptionCollection references;
            byte[] continuationPoint;

            session.Browse(
                null,
                null,
                nodeId,
                0,
                BrowseDirection.Forward,
                ReferenceTypeIds.HierarchicalReferences,
                true,
                (uint)NodeClass.Object | (uint)NodeClass.Variable,
                out continuationPoint,
                out references
            );

            foreach (var rd in references)
            {
                NodeId childNodeId = ExpandedNodeId.ToNodeId(rd.NodeId, session.NamespaceUris);

                // Read the Description attribute
                var readValueId = new ReadValueId
                {
                    NodeId = childNodeId,
                    AttributeId = Attributes.Description
                };

                DataValueCollection dataValues;
                var results = session.Read(null, 0, TimestampsToReturn.Neither, new ReadValueIdCollection { readValueId }, out dataValues, out _);
                var description = dataValues[0].Value?.ToString() ?? "N/A";

                Console.WriteLine($"{new string(' ', indent * 2)}Name: {rd.DisplayName.Text}, Description: {description}");

                // Recurse into child nodes
                BrowseRecursive(session, childNodeId, indent + 1);
            }
        }


    }

}
