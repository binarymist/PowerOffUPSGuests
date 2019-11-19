//Copyright (C) 2011  Kim Carter

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.  If not, see <http://www.gnu.org/licenses/>

using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Xml;

namespace BinaryMist.PowerOffUPSGuests {

    /// <summary>
    /// Controls the process of shutting down the associated server.
    /// </summary>
    /// <remarks>
    /// An instance of this class is created indirectly via the more specific concrete creators for each server that requires shutdown.
    /// Plays the part of the Creator, in the Factory Method pattern.
    /// </remarks>
    internal abstract class ServerController {

        protected static readonly string NewLine = Initiator.NewLine;
        
        protected enum RequestMethod {
            Get,
            Post
        }
        
        
        /// <summary>
        /// Constructor for the <see cref="ServerController"/> class.
        /// Called via the more specific children to initialize the less specific members.
        /// </summary>
        /// <param name="serverAdminDetails">
        /// The details required to create the messages that need to be sent to the server in order to perform the shutdown.</param>
        public ServerController(ServerAdminDetails serverAdminDetails) {
            ServerAdminDetails = serverAdminDetails;
            RequestAssembler = new RequestAssembler();
            SoapEnvelopes = new Queue<XmlDocument>();
            this.AssembleRequests();
        }


        protected ServerAdminDetails ServerAdminDetails { get; set; }

        protected RequestAssembler RequestAssembler { get; set; }

        /// <summary>
        /// Reference a <see cref="System.Collections.Generic.Queue{System.Xml.XmlDocument}">queue</see> of soap envelopes.
        /// used by the children of this class to send to the server.
        /// </summary>
        public Queue<XmlDocument> SoapEnvelopes { get; protected set; }


        /// <summary>
        /// Initial preparation of messages that will be sent to the server to perform shutdown.
        /// </summary>
        /// <remarks>Factory method.</remarks>
        public abstract void AssembleRequests();


        /// <summary>
        /// Completes the compilation of the sequence of messages that need to be sent to the server in order to perform the shutdown.
        /// Sends the messages.
        /// </summary>
        public abstract void Shutdown();


        protected void NotifyOfShutdown() {
            Logger.Instance.Log(
                string.Format(
                    "{0}.Shutdown on server: {1} has now been executed.",
                    ServerAdminDetails.ServerControllerType,
                    ServerAdminDetails.ServerName
                )
            );
        }


        protected bool SimplePing() {
            string serverName = ServerAdminDetails.ServerName;
            Logger.Instance.Log(string.Format("Performing Ping test on server: {0}", serverName));
            Ping pingSender = new Ping();
            int pingRetry = 3;
            PingReply reply = null;
            for (int i = 0; i < pingRetry; i++) {
                try {
                    //may take a couple of tries, as may time out due to arp delay
                    pingSender.Send(serverName);
                    Logger.Instance.Log(string.Format("Initiating Ping number {0} of {1} on server: {2}. ", i + 1, pingRetry, serverName));
                    reply = pingSender.Send(serverName);

                    if (reply.Status == IPStatus.Success) break;

                } catch (Exception e) {
                    Logger.Instance.Log(e.ToString());
                }
            }

            bool optionsAvailable = reply.Options != null;
            string noOptions = "No Options available";

            Logger.Instance.Log(
                "Reply status for server: " + serverName + " was " + reply.Status + ". " + NewLine +
                "Address: " + reply.Address.ToString() + ". " + NewLine +
                "RoundTrip time: " + reply.RoundtripTime + ". " + NewLine +
                "Time to live: " + ((optionsAvailable) ? reply.Options.Ttl.ToString() : noOptions) + ". " + NewLine +
                "Don't fragment: " + ((optionsAvailable) ? reply.Options.DontFragment.ToString() : noOptions) + ". " + NewLine +
                "Buffer size: " + reply.Buffer.Length + ". "
                );

            return reply.Status == IPStatus.Success;
        }
    }
}

