using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarryEyes.Mystique.Models.Connection.Continuous;

namespace StarryEyes.Mystique.Models.Connection
{
    public static class ConnectionManager
    {
        private static SortedDictionary<long, ConnectionGroup> connectionGroups;

        private static SortedDictionary<string, LinkedList<UserStreamsConnection>> trackResolver;

        /// <summary>
        /// Update connection states.<para />
        /// Apply current setting params, maintain the connections.
        /// </summary>
        public static void Update()
        {
        }
    }

    public class ConnectionGroup
    {
        // essential connections
        private UserStreamsConnection userStreams;
    }
}
