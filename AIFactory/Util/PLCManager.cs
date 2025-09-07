using AIFactory.Model;
using HandyControl.Data;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AIFactory.Util
{
    class PLCOPCManager
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
            _dataPoint.DataValue = readValue.Value;
            return _dataPoint;

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

    }

}
