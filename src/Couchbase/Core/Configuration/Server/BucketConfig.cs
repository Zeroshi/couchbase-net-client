using System;
using Couchbase.Core.Sharding;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using Couchbase.Core.Compatibility;
using Couchbase.Core.Diagnostics;
using Couchbase.Core.Exceptions;
using Couchbase.Utils;
using Google.Protobuf.WellKnownTypes;

namespace Couchbase.Core.Configuration.Server
{
    internal class Ports : IEquatable<Ports>
    {
        [JsonPropertyName("direct")] public int Direct { get; set; }
        [JsonPropertyName("proxy")] public int Proxy { get; set; }
        [JsonPropertyName("sslDirect")] public int SslDirect { get; set; }
        [JsonPropertyName("httpsCAPI")] public int HttpsCapi { get; set; }
        [JsonPropertyName("httpsMgmt")] public int HttpsMgmt { get; set; }

        public bool Equals(Ports other)
        {
            if (other == null) return false;
            return Direct == other.Direct &&
                Proxy == other.Proxy &&
                SslDirect == other.SslDirect &&
                HttpsCapi == other.HttpsCapi &&
                HttpsMgmt == other.HttpsMgmt;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Ports) obj);
        }

        public override int GetHashCode()
        {
            return Direct;
        }

        public static bool operator ==(Ports left, Ports right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Ports left, Ports right)
        {
            return !Equals(left, right);
        }
    }

    internal class Node : IEquatable<Node>
    {
        public Node()
        {
            Ports = new Ports
            {
                Direct = 11210,
                Proxy = 11211,
                SslDirect = 11207,
                HttpsCapi = 18092,
                HttpsMgmt = 18091
            };
            CouchApiBase = "http://$HOST:8092/default";
            CouchApiBaseHttps = "https://$HOST:18092/default";
        }
        [JsonPropertyName("couchApiBase")] public string CouchApiBase { get; set; }
        [JsonPropertyName("couchApiBaseHttps")] public string CouchApiBaseHttps { get; set; }
        [JsonPropertyName("hostname")] public string Hostname { get; set; }
        [JsonPropertyName("ports")] public Ports Ports { get; set; }
        [JsonPropertyName("services")] public List<string> Services { get; set; }
        [JsonPropertyName("version")] public string Version { get; set; }

        public bool Equals(Node other)
        {
            if (other == null) return false;
            return string.Equals(CouchApiBase, other.CouchApiBase) &&
                   string.Equals(Hostname, other.Hostname) &&
                   Equals(Ports, other.Ports);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Node) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (CouchApiBase != null ? CouchApiBase.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Hostname != null ? Hostname.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Ports != null ? Ports.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(Node left, Node right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Node left, Node right)
        {
            return !Equals(left, right);
        }
    }

    internal class Services : IEquatable<Services>
    {
        [JsonPropertyName("mgmt")] public int Mgmt { get; set; }
        [JsonPropertyName("mgmtSSL")] public int MgmtSsl { get; set; }
        [JsonPropertyName("indexAdmin")] public int IndexAdmin { get; set; }
        [JsonPropertyName("indexScan")] public int IndexScan { get; set; }
        [JsonPropertyName("indexHttp")] public int IndexHttp { get; set; }
        [JsonPropertyName("indexStreamInit")] public int IndexStreamInit { get; set; }
        [JsonPropertyName("indexStreamCatchup")] public int IndexStreamCatchup { get; set; }
        [JsonPropertyName("indexStreamMaint")] public int IndexStreamMaint { get; set; }
        [JsonPropertyName("indexHttps")] public int IndexHttps { get; set; }
        [JsonPropertyName("kv")] public int Kv { get; set; }
        [JsonPropertyName("kvSSL")] public int KvSsl { get; set; }
        [JsonPropertyName("capi")] public int Capi { get; set; }
        [JsonPropertyName("capiSSL")] public int CapiSsl { get; set; }
        [JsonPropertyName("projector")] public int Projector { get; set; }
        [JsonPropertyName("n1ql")] public int N1Ql { get; set; }
        [JsonPropertyName("n1qlSSL")] public int N1QlSsl { get; set; }
        [JsonPropertyName("cbas")] public int Cbas { get; set; }
        [JsonPropertyName("cbasSSL")] public int CbasSsl { get; set; }
        [JsonPropertyName("fts")] public int Fts { get; set; }
        [JsonPropertyName("ftsSSL")] public int FtsSsl { get; set; }
        [JsonPropertyName("moxi")] public int Moxi { get; set; }
        [JsonPropertyName("eventingAdminPort")] public int EventingAdminPort { get; set; }
        [JsonPropertyName("eventingSSL")] public int EventingSSL { get; set; }

        public bool Equals(Services other)
        {
            if (other == null) return false;
            return Mgmt == other.Mgmt && MgmtSsl == other.MgmtSsl && IndexAdmin == other.IndexAdmin &&
                   IndexScan == other.IndexScan && IndexHttp == other.IndexHttp &&
                   IndexStreamInit == other.IndexStreamInit && IndexStreamCatchup == other.IndexStreamCatchup &&
                   IndexStreamMaint == other.IndexStreamMaint && IndexHttps == other.IndexHttps && Kv == other.Kv &&
                   KvSsl == other.KvSsl && Capi == other.Capi && CapiSsl == other.CapiSsl &&
                   Projector == other.Projector && N1Ql == other.N1Ql && N1QlSsl == other.N1QlSsl &&
                   EventingAdminPort == other.EventingAdminPort && EventingSSL == other.EventingSSL;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Services) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Mgmt;
                hashCode = (hashCode * 397) ^ MgmtSsl;
                hashCode = (hashCode * 397) ^ IndexAdmin;
                hashCode = (hashCode * 397) ^ IndexScan;
                hashCode = (hashCode * 397) ^ IndexHttp;
                hashCode = (hashCode * 397) ^ IndexStreamInit;
                hashCode = (hashCode * 397) ^ IndexStreamCatchup;
                hashCode = (hashCode * 397) ^ IndexStreamMaint;
                hashCode = (hashCode * 397) ^ IndexHttps;
                hashCode = (hashCode * 397) ^ Kv;
                hashCode = (hashCode * 397) ^ KvSsl;
                hashCode = (hashCode * 397) ^ Capi;
                hashCode = (hashCode * 397) ^ CapiSsl;
                hashCode = (hashCode * 397) ^ Projector;
                hashCode = (hashCode * 397) ^ N1Ql;
                hashCode = (hashCode * 397) ^ N1QlSsl;
                return hashCode;
            }
        }

        public static bool operator ==(Services left, Services right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Services left, Services right)
        {
            return !Equals(left, right);
        }
    }

    internal class NodesExt : IEquatable<NodesExt>
    {
        public NodesExt()
        {
            Services = new Services();
        }

        [JsonPropertyName("thisNode")] public bool ThisNode { get; set; }
        [JsonPropertyName("services")] public Services Services { get; set; }
        [JsonPropertyName("hostname")] public string Hostname { get; set; }
        [JsonPropertyName("serverGroup")] public string ServerGroup { get; set; }
        [JsonPropertyName("alternateAddresses")] public Dictionary<string, ExternalAddressesConfig> AlternateAddresses { get; set; }
        [JsonPropertyName("appTelemetryPath")] public string AppTelemetryPath { get; set; }
        [JsonPropertyName("nodeUUID")] public string NodeUuid { get; set; }

        public bool HasAlternateAddress => AlternateAddresses != null && AlternateAddresses.Count != 0;

        public bool Equals(NodesExt other)
        {
            if (other == null) return false;
            return Equals(Services, other.Services) &&
                   Hostname == other.Hostname;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((NodesExt) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Services != null ? Services.GetHashCode() : 0) * 397) ^
                       ((Hostname != null ? Hostname.GetHashCode() : 0) * 397);
            }
        }

        public static bool operator ==(NodesExt left, NodesExt right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(NodesExt left, NodesExt right)
        {
            return !Equals(left, right);
        }
    }

    internal class Ddocs
    {
        public string uri { get; set; }
    }

    //Root object
    internal class BucketConfig : IEquatable<BucketConfig>, IJsonOnDeserialized
    {
        internal const string GlobalBucketName = "CLUSTER";
        private string _networkResolution = Couchbase.NetworkResolution.Auto;
        private List<string> _bucketCaps = new();
        private Dictionary<string, IEnumerable<string>> _clusterCaps = new();
        internal ClusterLabels ClusterLabels = new();
        private List<NodesExt> _nodesExt = new();
        private List<NodesExt> NodesWithAppTelemetry => NodesExt
            .Where(n => !string.IsNullOrEmpty(n.AppTelemetryPath))
            .ToList();

        internal Uri GetAppTelemetryPath(int attempt, bool? tlsEnabled = false)
        {
            if (NodesWithAppTelemetry is null || NodesWithAppTelemetry.Count == 0) return null;

            var targetIndex = attempt % NodesWithAppTelemetry.Count;
            var node = NodesWithAppTelemetry.ElementAt(targetIndex);

            if (node == null) return null;

            if (!node.HasAlternateAddress) return ConstructAppTelemetryUri(tlsEnabled, node.Hostname, node.Services, node.AppTelemetryPath);
            var alt = node.AlternateAddresses.FirstOrDefault().Value;
            return ConstructAppTelemetryUri(tlsEnabled, alt.Hostname, alt.Ports, node.AppTelemetryPath);
        }

        private static Uri ConstructAppTelemetryUri(bool? tlsEnabled, string hostname, Services services, string appTelemetryPath)
        {
            return tlsEnabled.HasValue && tlsEnabled.Value
                ? new Uri("wss://" + hostname + ":" + services.MgmtSsl + appTelemetryPath)
                : new Uri("ws://" + hostname + ":" + services.Mgmt + appTelemetryPath);
        }

        public ConfigVersion ConfigVersion { get; private set; }

        public BucketConfig()
        {
            Nodes = new List<Node>();
            VBucketServerMap = new VBucketServerMapDto();
        }

        public string NetworkResolution
        {
            get => _networkResolution;
            set
            {
                _networkResolution = value;

                //After setting the network resolution we need to update the
                //servers used for VBucketMapping so the correct addresses are used.
                ResolveHostName();
            }
        }

        /// <summary>
        /// Sets the "effective" network resolution to be used for alternate addresses using the following heuristic:
        /// "internal": The SDK should be using the normal addresses/ports as specified in the config.
        /// "external": The SDK should be using the "external" alternate-address address/ports as specified in the config.
        /// "auto" (default): The SDK should be making a determination based on the heuristic that is in the RFC at bootstrap
        /// time only, and then once this determination has been made, the network resolution mode should be unambiguously
        /// set to "internal" or "external".
        /// </summary>
        /// <param name="options">THe <see cref="ClusterOptions"/> for configuration.</param>
        public void SetEffectiveNetworkResolution(ClusterOptions options)
        {
            if (NodesExt.FirstOrDefault()!.HasAlternateAddress)
            {
                //Use heuristic to derive the network resolution from auto
                if (options.NetworkResolution == Couchbase.NetworkResolution.Auto)
                {
                    foreach (var nodesExt in NodesExt)
                    {
                        if (nodesExt.AlternateAddresses.Any())
                        {
                            NetworkResolution = options.EffectiveNetworkResolution =
                                Couchbase.NetworkResolution.External;
                            return;
                        }
                    }
                    //We detect internal or "default" should be used
                    NetworkResolution = options.EffectiveNetworkResolution = Couchbase.NetworkResolution.Default;
                }
                else
                {
                    //use whatever the caller wants to use
                    NetworkResolution = options.EffectiveNetworkResolution = options.NetworkResolution;
                }
            }
            else
            {
                //we don't have any alt addresses so just use the internal address
                NetworkResolution = options.EffectiveNetworkResolution = Couchbase.NetworkResolution.Default;
            }
        }

        /// <summary>
        /// Set to true if a GCCCP config
        /// </summary>
        [JsonIgnore]
        public bool IsGlobal => Name == GlobalBucketName;

        /// <summary>
        ///When true, we want to ignore the config revision and just accept the
        ///config provided. This happens when a DNS SRV refresh is detected and
        ///we need to "rebootstrap".
        /// </summary>
        [JsonIgnore] public bool IgnoreRev { get; set; }

        [JsonPropertyName("rev")] public ulong Rev { get; set; }
        [JsonPropertyName("revEpoch")] public ulong RevEpoch { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; } = GlobalBucketName;
        [JsonPropertyName("uri")] public string Uri { get; set; }
        [JsonPropertyName("streamingUri")] public string StreamingUri { get; set; }
        [JsonPropertyName("nodes")] public List<Node> Nodes { get; set; }
        [JsonPropertyName("nodesExt")] public List<NodesExt> NodesExt { get; set; }
        [JsonPropertyName("nodeLocator")] public string NodeLocator { get; set; }
        [JsonPropertyName("uuid")] public string Uuid { get; set; }
        [JsonPropertyName("ddocs")] public Ddocs Ddocs { get; set; }
        [JsonPropertyName("vBucketServerMap")] public VBucketServerMapDto VBucketServerMap { get; set; }
        [JsonPropertyName("bucketCapabilitiesVer")] public string BucketCapabilitiesVer { get; set; }

        [JsonPropertyName("clusterUUID")]
        public string ClusterUuid
        {
            get => ClusterLabels.ClusterUuid;
            set => ClusterLabels.ClusterUuid = value;
        }

        [JsonPropertyName("clusterName")]
        public string ClusterName
        {
            get => ClusterLabels.ClusterName;
            set => ClusterLabels.ClusterName = value;
        }

        [JsonPropertyName("bucketCapabilities")]
        public List<string> BucketCapabilities
        {
            get => _bucketCaps;
            set
            {
                _bucketCaps = value;

                // because checking caps may be on a hot path, we copy them into a set for O(1) lookup
                if (value is not null)
                {
                    _bucketCapsSet = new HashSet<string>(value);
                }
                else
                {
                    _bucketCapsSet = new();
                }
            }
        }
        [JsonPropertyName("clusterCapabilitiesVer")] public List<int> ClusterCapabilitiesVersion { get; set; }

        [JsonPropertyName("clusterCapabilities")]
        public Dictionary<string, IEnumerable<string>> ClusterCapabilities
        {
            get => _clusterCaps;
            set
            {
                _clusterCaps = value;
                if (value is not null)
                {
                    _clusterCapsSet = new();

                    // because checking the cluster caps is now on a semi-hot path,
                    // we copy the caps into a set for O(1) lookup
                    // The list of capabilities is small enough that we could optimize it into an enum flag check,
                    // if needed, but we would then not be able to support checking for caps by string
                    foreach (var kvp in value)
                    {
                        var section = kvp.Key;
                        var caps = kvp.Value;
                        foreach (var cap in caps)
                        {
                            _clusterCapsSet.Add($"{section}.{cap}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Maps each Hostname to its index in <see cref="VBucketServerMap"/>'s ServerList.
        /// Example: { "10.0.0.1": 0, "10.0.0.2": 1, ... }
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, int> HostnamesAndIndex => VBucketServerMap.ServerList
            .Select((hostname, index) => new { hostname, index })
            .ToDictionary(item => item.hostname.Split(':')[0], item => item.index);

        /// <summary>
        /// Maps each Hostname to which ServerGroup it belongs to.
        /// Example: { "10.0.0.1": "group_1", "10.0.0.2": "group_1", ... }
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, string> HostnameAndServerGroup => NodesExt
            .Where(nodeExt => !string.IsNullOrEmpty(nodeExt.ServerGroup))
            .Select(nodeExt => new { nodeExt.Hostname, nodeExt.ServerGroup })
            .ToDictionary(item => item.Hostname, item => item.ServerGroup);

        /// <summary>
        /// Maps each unique ServerGroup to the indexes of the nodes it contains, in <see cref="VBucketServerMap"/>'s ServerList.
        /// Example: { "group_1": [0, 1, 2], "group_2": [3, 4, 5], ... }
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, int[]> ServerGroupNodeIndexes =>
            HostnameAndServerGroup
                .GroupBy(x => x.Value)
                .Where(group => !string.IsNullOrEmpty(group.Key))
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(kvp => HostnamesAndIndex[kvp.Key]).ToArray()
                );

        public bool HasBucketCap(string capability) => _bucketCapsSet?.Contains(capability) == true;
        internal void AssertBucketCap(string bucketCap, string message = null)
        {
            if (!HasBucketCap(bucketCap))
            {
                var errorMsg = message is null ? bucketCap : $"{bucketCap}: {message}";
                throw new FeatureNotAvailableException(errorMsg);
            }
        }

        public bool HasClusterCap(string sectionDotCapability) => _clusterCapsSet.Contains(sectionDotCapability);
        internal void AssertClusterCap(string clusterCap, string message = null)
        {
            if (!HasClusterCap(clusterCap))
            {
                var errorMsg = message is null ? clusterCap : $"{clusterCap}: {message}";
                throw new FeatureNotAvailableException(errorMsg);
            }
        }

        public bool Equals(BucketConfig other)
        {
            if (other == null) return false;

            return Rev == other.Rev && RevEpoch == other.RevEpoch && string.Equals(Name, other.Name) && string.Equals(Uri, other.Uri) &&
                   string.Equals(StreamingUri, other.StreamingUri) && string.Equals(NodeLocator, other.NodeLocator) &&
                   Equals(VBucketServerMap, other.VBucketServerMap) && (NodesExt.AreEqual(other.NodesExt) && Nodes.AreEqual(other.Nodes));
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((BucketConfig)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Rev;
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Uri != null ? Uri.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (StreamingUri != null ? StreamingUri.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Nodes != null ? Nodes.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (NodesExt != null ? NodesExt.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (NodeLocator != null ? NodeLocator.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Uuid != null ? Uuid.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Ddocs != null ? Ddocs.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (VBucketServerMap != null ? VBucketServerMap.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (BucketCapabilitiesVer != null ? BucketCapabilitiesVer.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (BucketCapabilities != null ? BucketCapabilities.GetHashCode() : 0);
                return hashCode;
            }
        }

        public void OnDeserialized()
        {
            ConfigVersion = new ConfigVersion(RevEpoch, Rev);
        }

        public static bool operator ==(BucketConfig left, BucketConfig right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(BucketConfig left, BucketConfig right)
        {
            return !Equals(left, right);
        }

        public void ResolveHostName()
        {
            for (var i = 0; i < VBucketServerMap.ServerList.Length; i++)
            {
                var nodeExt = NodesExt?.FirstOrDefault(x => x.Hostname != null && VBucketServerMap.ServerList[i].Contains(x.Hostname));
                if (nodeExt != null && UseAlternateAddresses)
                {
                    var alternateAddress = nodeExt.AlternateAddresses[NetworkResolution];
                    var port = alternateAddress.Ports.Kv > 0 ? alternateAddress.Ports.Kv : alternateAddress.Ports.KvSsl;
                    VBucketServerMap.ServerList[i] = $"{alternateAddress.Hostname}:{port}";
                }
            }
        }

        private bool? _useAlternateAddresses;
        private HashSet<string> _bucketCapsSet;
        private HashSet<string> _clusterCapsSet;

        public bool UseAlternateAddresses
        {
            get
            {
                if (_useAlternateAddresses == null)
                {
                    _useAlternateAddresses = NodesExt?.Any(x => x.HasAlternateAddress && NetworkResolution == Couchbase.NetworkResolution.External) ?? false;
                }
                return _useAlternateAddresses.Value;
            }
        }
    }

    internal class ClusterCapabilities
    {
        // flattened section.capabilityName constants for set-based lookup.
        public const string SCOPED_SEARCH_INDEX = "search.scopedSearchIndex";
        public const string VECTOR_SEARCH = "search.vectorSearch";

        [JsonPropertyName("clusterCapabilitiesVer")] public IEnumerable<int> Version { get; set; }
        [JsonPropertyName("clusterCapabilities")] public Dictionary<string, IEnumerable<string>> Capabilities { get; set; }

        internal bool EnhancedPreparedStatementsEnabled
        {
            get
            {
                if (Capabilities != null)
                {
                    if (Capabilities.TryGetValue(ServiceType.Query.GetDescription(), out var features))
                    {
                        return features.Contains(ClusterCapabilityFeatures.EnhancedPreparedStatements.GetDescription());
                    }
                }

                return false;
            }
        }

        internal bool UseReplicaEnabled
        {
            get
            {
                if (Capabilities != null)
                {
                    if (Capabilities.TryGetValue(ServiceType.Query.GetDescription(), out var features))
                    {
                        return features.Contains(ClusterCapabilityFeatures.UseReplicaFeature.GetDescription());
                    }
                }
                return false;
            }
        }
    }

    internal enum ClusterCapabilityFeatures
    {
        [Description("enhancedPreparedStatements")] EnhancedPreparedStatements,
        [Description("readFromReplica")] UseReplicaFeature
    }

    internal sealed class AlternateAddressesConfig
    {
        [JsonPropertyName("external")] public ExternalAddressesConfig External { get; set; }

        public bool HasExternalAddress => External?.Hostname != null;
    }

    internal sealed class ExternalAddressesConfig
    {
        [JsonPropertyName("hostname")] public string Hostname { get; set; }
        [JsonPropertyName("ports")] public Services Ports { get; set; }
    }
}


/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2021 Couchbase, Inc.
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *
 * ************************************************************/
