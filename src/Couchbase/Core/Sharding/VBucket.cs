using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Couchbase.Core.Configuration.Server;
using Couchbase.Utils;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Couchbase.Core.Sharding
{
     /// <summary>
    /// Represents a VBucket partition in a Couchbase cluster
    /// </summary>
    internal class VBucket : IVBucket
    {
        private readonly short[] _replicas;
        private readonly VBucketServerMap _vBucketServerMap;
        private readonly ILogger<VBucket> _logger;
        private readonly ICollection<HostEndpointWithPort> _endPoints;
        private readonly string _vbucketIdToString;

        public VBucket(ICollection<HostEndpointWithPort> endPoints, short index, short primary, short[] replicas, ulong rev,
            VBucketServerMap vBucketServerMap, string bucketName, ILogger<VBucket> logger, ConfigVersion configVersion)
        {
            if (logger == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(logger));
            }

            _endPoints = endPoints;
            Index = index;
            Primary = primary;
            _replicas = replicas;
            Rev = rev;
            _vBucketServerMap = vBucketServerMap;
            BucketName = bucketName;
            _logger = logger;
            _vbucketIdToString = $"{Index}-{configVersion.ToString()}";
        }

        /// <summary>
        /// Gets a reference to the primary server for this VBucket.
        /// </summary>
        /// <returns>A <see cref="IServer"/> reference which is the primary server for this <see cref="VBucket"/></returns>
        ///<remarks>If the VBucket doesn't have a active, it will return a random <see cref="IServer"/> to force a NMV and reconfig.</remarks>
        public HostEndpointWithPort? LocatePrimary()
        {
            HostEndpointWithPort? endPoint = null;
            if (Primary > -1 && Primary < _endPoints.Count &&
                Primary < _vBucketServerMap.EndPoints.Count)
            {
                try
                {
                    endPoint = _vBucketServerMap.EndPoints[Primary];
                }
                catch (Exception e)
                {
                    _logger.LogDebug(e, "Error locating Primary");
                }
            }
            if(endPoint == null)
            {
                if (_replicas.Any(x => x != -1))
                {
                    var index = _replicas.GetRandomValueType().GetValueOrDefault();
                    if (index > -1 && index < _endPoints.Count
                        && index < _vBucketServerMap.EndPoints.Count)
                    {
                        try
                        {
                            endPoint = _vBucketServerMap.EndPoints[index];
                        }
                        catch (Exception e)
                        {
                            _logger.LogDebug(e, "Error locating Primary");
                        }
                    }
                }
            }
            return endPoint ?? (_endPoints.GetRandomValueType());
        }

        /// <summary>
        /// Locates a replica for a given index.
        /// </summary>
        /// <param name="index">The index of the replica.</param>
        /// <returns>An <see cref="IServer"/> if the replica is found, otherwise null.</returns>
        public HostEndpointWithPort? LocateReplica(short index)
        {
            try
            {
                return _vBucketServerMap.EndPoints[index];
            }
            catch
            {
                _logger.LogDebug("No server found for replica with index of {0}.", index);
                return null;
            }
        }

        /// <summary>
        /// Gets an array of replica indexes.
        /// </summary>
        public short[] Replicas => _replicas;

        /// <summary>
        /// Gets the index of the VBucket.
        /// </summary>
        /// <value>
        /// The index.
        /// </value>
        public short Index { get; }

        /// <summary>
        /// Gets the index of the primary node in the VBucket.
        /// </summary>
        /// <value>
        /// The primary index that the key has mapped to.
        /// </value>
        public short Primary { get; }

        /// <summary>
        /// Gets or sets the configuration revision.
        /// </summary>
        /// <value>
        /// The rev.
        /// </value>
        public ulong Rev { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether this instance has replicas.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has replicas; otherwise, <c>false</c>.
        /// </value>
        public bool HasReplicas
        {
            get { return _replicas.Any(x => x > -1); }
        }

        public string BucketName { get; }

        public override string ToString()
        {
            return _vbucketIdToString;
        }
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
