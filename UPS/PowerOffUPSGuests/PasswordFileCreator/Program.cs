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
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BinaryMist.PowerOffUPSGuests;

namespace BinaryMist.PasswordFileCreator {

    /// <summary>
    /// Creates a file that stores the password used to log into the target server requiring shutdown.
    /// </summary>
    class Program {

        static void Main() {
            CreatePasswordFile();
        }


        /// <summary>
        /// Provides interactive capture for the insertion of an encrypted password,
        /// based on the ServerUserPwFile0 specified in the BinaryMist.PowerOffUPSGuests.dll.config file.
        /// </summary>
        public static void CreatePasswordFile() {

            bool validPath;
            string path = null;
            string RetryMessage = "Please try again.";

            Console.WriteLine("You must create the password file running under" + Initiator.NewLine + "the same account that will run BinaryMist.PowerOffUPSGuests.");

            do {
                Console.WriteLine(
                    "From the BinaryMist.PowerOffUPSGuests.dll.config file." + Initiator.NewLine +
                    "Please specify the ServerUserPwFile[n] value" + Initiator.NewLine +
                    "for the encrypted Password to be stored to." + Initiator.NewLine +
                    "This must be a valid path" + Initiator.NewLine
                );

                try {
                    validPath = true;
                    path = Path.GetFullPath(Console.ReadLine());

                    if (!
                        ((IEnumerable<string>)ConfigReader.Read.AllKeyVals.Values)
                        .Contains<string>(
                            path, StringComparer.CurrentCultureIgnoreCase
                        )
                    ) {
                        Console.WriteLine(Initiator.NewLine);
                        Console.WriteLine("The value that was entered" + Initiator.NewLine +
                            "was not one of the specified values for ServerUserPwFile[n]");
                        Console.WriteLine(RetryMessage + Initiator.NewLine);
                        validPath = false;
                    }
                } catch (Exception) {
                    Console.WriteLine(Initiator.NewLine + "An invalid path was entered." + Initiator.NewLine + RetryMessage + Initiator.NewLine);
                    validPath = false;
                }
            } while (validPath == false);

            Console.WriteLine(
                Initiator.NewLine
                + "The password you are about to enter"
                + Initiator.NewLine
                + "will be encrypted to file \"{0}\""
                , path
            );

            byte[] encryptedBytes = ProtectedData.Protect(
                new ASCIIEncoding().GetBytes(pWord),
                ServerAdminDetails.CredentialEntropy(),
                DataProtectionScope.CurrentUser
            );
            File.WriteAllBytes(path, encryptedBytes);

            Console.WriteLine(
                Initiator.NewLine
                + Initiator.NewLine
                + string.Format(
                    "The password you just entered has been encrypted"
                    + Initiator.NewLine
                    + "and saved to {0}"
                    , path
                )
            );
            Console.WriteLine(Initiator.NewLine + "Press any key to exit");
            Console.ReadKey(true);
        }


        private static string pWord {
            get {
                bool passWordsMatch = false;
                bool firstAttempt = true;
                string passWord = null;
                while (!passWordsMatch) {

                    if (!firstAttempt)
                        Console.WriteLine(Initiator.NewLine + "The passwords did not match." + Initiator.NewLine);

                    Console.WriteLine(Initiator.NewLine + "Please enter the password..." + Initiator.NewLine);
                    string passWordFirstAttempt = GetPWordFromUser();
                    Console.WriteLine(Initiator.NewLine + Initiator.NewLine + "Please confirm by entering the password once again..." + Initiator.NewLine);
                    string passWordSecondAttempt = GetPWordFromUser();

                    if (string.Compare(passWordFirstAttempt, passWordSecondAttempt) == 0) {
                        passWordsMatch = true;
                        passWord = passWordFirstAttempt;
                    }
                    firstAttempt = false;
                }
                Console.WriteLine(Initiator.NewLine + Initiator.NewLine + "Success, the passwords match." + Initiator.NewLine);
                return passWord;
            }
        }


        private static string GetPWordFromUser() {
            string passWord = string.Empty;
            ConsoleKeyInfo info = Console.ReadKey(true);
            while (info.Key != ConsoleKey.Enter) {
                if (info.Key != ConsoleKey.Backspace) {
                    if (info.KeyChar < 0x20 || info.KeyChar > 0x7E) {
                        info = Console.ReadKey(true);
                        continue;
                    }
                    passWord += info.KeyChar;
                    Console.Write("*");
                    info = Console.ReadKey(true);
                } else if (info.Key == ConsoleKey.Backspace) {
                    if (!string.IsNullOrEmpty(passWord)) {
                        passWord = passWord.Substring
                            (0, passWord.Length - 1);
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        Console.Write(' ');
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                    }
                    info = Console.ReadKey(true);
                }
            }
            return passWord;
        }
    }
}
