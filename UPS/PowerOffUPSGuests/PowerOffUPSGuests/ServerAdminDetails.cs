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
using System.IO;


namespace BinaryMist.PowerOffUPSGuests {

    /// <summary>
    /// Provides the administration details of the servers listed in the BinaryMist.PowerOffUPSGuests.dll.config file.
    /// </summary>
    public class ServerAdminDetails {

        #region singleton initialization
        private static readonly Lazy<Queue<ServerAdminDetails>> _queue = new Lazy<Queue<ServerAdminDetails>>(QueueServersForShutdown);


        private ServerAdminDetails(string serverController, string serverName, string serverPort, string userName, byte[] serverCredential) {
            ServerControllerType = serverController;
            ServerName = serverName;
            ServerPort = serverPort;
            UserName = userName;
            Password = serverCredential;
        }


        /// <summary>
        /// Provides the process wide single instance queue of each servers admin details.
        /// </summary>
        public static Queue<ServerAdminDetails> QueuedDetails {
            get {
                return _queue.Value;
            }
        }
        #endregion


        private static Queue<ServerAdminDetails> ServersQueuedForShutdown { get; set; }


        private static byte[] GetMyCredential(string pWFileName) {
            try {
                return File.ReadAllBytes(pWFileName);
            } catch(Exception e) {
                string error = string.Format(
                    "Error occured while instantiating a ServerAdminDetails instance for queueing. Specificaly while reading bytes from the following file: {0}{1}Exception details follow.{1}{2}",
                    pWFileName,
                    Initiator.NewLine,
                    e
                );
                Logger.Instance.Log(error);
                throw new Exception(error);
            }
        }


        private static Queue<ServerAdminDetails> QueueServersForShutdown() {

            ServersQueuedForShutdown = new Queue<ServerAdminDetails>();

            const int firstServerIndex = 0;
            int serverCount = firstServerIndex;
            string empty = string.Empty;
            
            do
            {
                string controller = ConfigReader.Read["Controller" + serverCount];
                string server = ConfigReader.Read["Server" + serverCount];
                string serverPort = ConfigReader.Read["ServerPort" + serverCount];
                string serverUser = ConfigReader.Read["ServerUser" + serverCount];
                string serverUserPwFile = ConfigReader.Read["ServerUserPwFile" + serverCount];

                if (controller == empty || server == empty || serverPort == empty || serverUser == empty || serverUserPwFile == empty)
                    break;
                
                ServersQueuedForShutdown.Enqueue(
                    new ServerAdminDetails(
                        controller,
                        server,
                        serverPort,
                        serverUser,
                        GetMyCredential(Path.GetFullPath(serverUserPwFile))
                    )
                );

                Logger.Instance.Log (
                    string.Format (
                        "Server admin details of Controller: {0}, Server: {1}, ServerPort: {2}, ServerUser: {3}, ServerUserPwFile: {4} added to queued element number {5}.",
                        controller,
                        server,
                        serverPort,
                        serverUser,
                        serverUserPwFile,
                        serverCount
                    )
                );
                serverCount++;

            } while (true);
            return ServersQueuedForShutdown;
        }


        /// <summary>
        /// Retreives the entropy found in the BinaryMist.PowerOffUPSGuests.dll.config file, used to encrypt all the passwords.
        /// </summary>
        /// <returns>byte[]</returns>
        public static byte[] CredentialEntropy() {
            string[] numbers = ConfigReader.Read["CredentialEntropy"].Split(',');
            byte[] entropy = new byte[numbers.Length];
            for (int i = 0; i < numbers.Length; i++) {
                entropy[i] = Byte.Parse(numbers[i]);
            }
            return entropy;
        }
        

        /// <summary>
        /// The name of the ServerController child type.
        /// </summary>
        internal string ServerControllerType { get; private set; }

        /// <summary>
        /// The name of the server
        /// </summary>
        internal string ServerName { get; private set; }

        /// <summary>
        /// The port of the server
        /// </summary>
        internal string ServerPort { get; private set; }

        /// <summary>
        /// The user name for the server
        /// </summary>
        internal string UserName { get; private set; }

        /// <summary>
        /// The password for the user
        /// </summary>
        internal byte[] Password { get; private set; }
    }
}

