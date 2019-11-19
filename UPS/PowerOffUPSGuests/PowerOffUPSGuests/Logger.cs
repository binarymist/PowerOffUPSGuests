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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace BinaryMist.PowerOffUPSGuests {

    /// <summary>
    /// Provides single instance access to the logging functionality in 
    /// </summary>
    internal class Logger {

        #region singleton initialization
        private static readonly Lazy<Logger> _instance = new Lazy<Logger>(() => new Logger());
        
        private Logger() {
        }


        /// <summary>
        /// The single instance of <see cref="Logger">Logger</see>
        /// </summary>
        public static Logger Instance {
            get {
                return _instance.Value;
            }
        }
        #endregion

        private static readonly string LogLinePrefix = "  :";


        private void AppendToLog(Action<TextWriter> write) {
            using (StreamWriter w = File.AppendText(ConfigReader.Read["LogFilePath"])) {
                write(w);
            }
        }


        private string FormatLogMessage(string message) {
            StringBuilder sB = new StringBuilder();
            sB.Insert(0, LogLinePrefix);
            if (message.Contains(Initiator.NewLine)) {
                string[] lines = message.Split(new string[] { Initiator.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < lines.Length; i++) {
                    if (i != 0) {
                        sB.Append(LogLinePrefix);
                    }
                    sB.AppendLine(lines[i]);
                }
            } else {
                sB.AppendLine(message);
            }
            return sB.ToString();
        }


        /// <summary>
        /// Writes message to the log file, who's location is specified in the LogFilePath element
        /// of the assemblySettings attribute found in the BinaryMist.PowerOffUPSGuests.dll.config file.
        /// </summary>
        /// <param name="message">The message to be written to the log file.</param>
        public void Log(string message) {
            AppendToLog(
                (TextWriter w) => {
                    w.Write("\r\nLog Entry : ");
                    w.WriteLine("{0:HH:mm:ss.fff} {1}", DateTime.Now, DateTime.Now.ToLongDateString());
                    w.WriteLine("  :");
                    w.WriteLine(FormatLogMessage(message));
                    w.WriteLine("-------------------------------");
                    w.Flush();
                }
            );
        }


        /// <summary>
        /// Produces a simple trace message, containing details of the type and method that the execution path passed through.
        /// This message is written to the log file, who's location is specified in the LogFilePath element
        /// of the assemblySettings attribute found in the BinaryMist.PowerOffUPSGuests.dll.config file.
        /// </summary>
        public void LogTrace() {
            if (ConfigReader.Read.Debug) {
                MethodBase callingMethod = new StackFrame(1).GetMethod();
                Log(
                    string.Format(
                        "Execution trace through: Type: {0} Method: {1}",
                        callingMethod.DeclaringType.FullName,
                        callingMethod.Name
                    )
                );
            }
        }
    }
}

