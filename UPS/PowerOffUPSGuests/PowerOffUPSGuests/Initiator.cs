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
using System.Threading.Tasks;

namespace BinaryMist.PowerOffUPSGuests {
    
    /// <summary>
    /// Provides an entry point to the compilation of a queue of servers to be shutdown.
    /// </summary>
    public class Initiator {

        private enum Synchronicity {
            Synchronous,
            Asynchronous
        }

        public static string NewLine {
            get { return Environment.NewLine; }
        }
        

        /// <summary>
        /// Constructor of the Initiator class.
        /// </summary>
        public Initiator() {
            Logger.Instance.Log(
                "================================================================================" + NewLine +
                "=========Initiator constructed and Initiating shutdown routines=========" + NewLine +
                "================================================================================"
            );
        }
        
        
        private void ShutdownSynchronously(Queue<ServerController> serverControllers) {
            foreach (ServerController serverController in serverControllers) {
                serverController.Shutdown();
            }
        }


        private void ShutdownAsynchronously(Queue<ServerController> serverControllers) {
            Action[] shutdownActions = new Action[serverControllers.Count];
            ServerController[] serverControllerArray = serverControllers.ToArray();

            for (int i = 0; i < serverControllerArray.Length; i++) {
                shutdownActions[i] = serverControllerArray[i].Shutdown;
            }

            try {
                Parallel.Invoke(shutdownActions);
            }
                // No exception is expected in this example, but if one is still thrown from a task,
                // it will be wrapped in AggregateException and propagated to the main thread. See MSDN example
            catch (AggregateException e) {
                Logger.Instance.Log(string.Format("An action has thrown an exception. THIS WAS UNEXPECTED.\n{0}", e.InnerException));
                throw new Exception();
            }
        }


        /// <summary>
        /// Entry point into the assembly.
        /// Create a queue of instantiated server controllers based on the file BinaryMist.PowerOffUPSGuests.dll.config.
        /// The decission to shutdown servers synchronously or asynchronously
        /// is determmined by the Synchronicity element within the assemblysettings
        /// found in BinaryMist.PowerOffUPSGuests.dll.config.
        /// </summary>
        /// <returns>Confirmation message.</returns>
        public string InitShutdownOfServers() {
            Logger.Instance.LogTrace();
            Queue<ServerController> serverControllers = new Queue<ServerController>();

            try {
                foreach (ServerAdminDetails serverAdminDetail in ServerAdminDetails.QueuedDetails) {

                    Type t = Type.GetType(GetType().Namespace + "." + serverAdminDetail.ServerControllerType);
                    serverControllers.Enqueue(Activator.CreateInstance(t, serverAdminDetail) as ServerController);
                }
            } catch(Exception e) {
                Logger.Instance.Log("Exception occured while enqueueing the server controllers. Details follow:" +
                    NewLine +
                    e.ToString()
                );
                throw;
            }
            
            bool ignoreCase = true;
            Synchronicity synchronicity = (Synchronicity)Enum.Parse(typeof (Synchronicity), ConfigReader.Read["Synchronicity"], ignoreCase);

            if(synchronicity == Synchronicity.Synchronous)
                ShutdownSynchronously(serverControllers);
            else
                ShutdownAsynchronously(serverControllers);

            return "InitShutdownOfServers successfully executed.";
        }
    }
}

