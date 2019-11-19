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
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Xml;


namespace BinaryMist.PowerOffUPSGuests {

    /// <summary>
    /// Provides ancillary operations for <see cref="ServerController">server controllers</see> 
    /// that assist in the creation of the requests intended for dispatch to the servers requiring shutdown.
    /// </summary>
    internal class RequestAssembler {

        private static string _soapEnvelope = @"<soap:Envelope xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>
    <soap:Header>
    </soap:Header>
    <soap:Body>
    </soap:Body>
</soap:Envelope>";

        
        // Note : A possible candidate for conversion to a concrete strategy
        /// <summary>
        /// Produces a SOAP envelope in XML form with the server admins user credentials
        /// </summary>
        /// <param name="headerContent">Content targeted for between the header tags.</param>
        /// <param name="bodyContent">Content targeted for between the body tags.</param>
        /// <param name="serverAdminDetails">The <see cref="ServerAdminDetails"/> instance used to access the user name and password.</param>
        /// <returns>The SOAP envelope.</returns>
        public XmlDocument CreateLoginSoapEnvelope(string headerContent, string bodyContent, ServerAdminDetails serverAdminDetails) {
            
            string failMessage = "The credentials were not correctly set. Username: {0} Password byte count: {1}";
            if (string.IsNullOrEmpty(serverAdminDetails.UserName) || serverAdminDetails.Password.Length < 1)
                throw new InvalidCredentialException(string.Format(failMessage, serverAdminDetails.UserName, serverAdminDetails.Password.Length));

            StringBuilder bodyContentBuilder= new StringBuilder(bodyContent, bodyContent.Length + 10);
            bodyContentBuilder.Insert(
                bodyContentBuilder.ToString().IndexOf("</userName>"),
                serverAdminDetails.UserName
            );

            bodyContentBuilder.Insert(
                bodyContentBuilder.ToString().IndexOf("</password>"),
                decryptedCredential(serverAdminDetails)
            );

            return CreateSoapEnvelope(headerContent, bodyContentBuilder.ToString());
        }


        private string decryptedCredential(ServerAdminDetails serverAdminDetails) {
            try {
                return new ASCIIEncoding().GetString(
                    ProtectedData.Unprotect(
                        serverAdminDetails.Password,
                        ServerAdminDetails.CredentialEntropy(),
                        DataProtectionScope.CurrentUser
                    )
                );
            } catch(CryptographicException e) {

                // Retrieve the exception that caused the current
                // CryptographicException exception.
                Exception innerException = e.InnerException;
                string innerExceptionMessage = "";
                if (innerException != null) {
                    innerExceptionMessage = innerException.ToString();
                }

                // Retrieve the message that describes the exception.
                string message = e.Message;

                // Retrieve the name of the application that caused the exception.
                string exceptionSource = e.Source;

                // Retrieve the call stack at the time the exception occured.
                string stackTrace = e.StackTrace;

                // Retrieve the method that threw the exception.
                System.Reflection.MethodBase targetSite = e.TargetSite;
                string siteName = targetSite.Name;

                // Retrieve the entire exception as a single string.
                string entireException = e.ToString();

                // Get the root exception that caused the current
                // CryptographicException exception.
                Exception baseException = e.GetBaseException();
                string baseExceptionMessage = "";
                if (baseException != null) {
                    baseExceptionMessage = baseException.Message;
                }

                Logger.Instance.Log(
                    "Caught an unexpected exception:" + Initiator.NewLine
                    + entireException + Initiator.NewLine
                    + Initiator.NewLine
                    + "Properties of the exception are as follows:" + Initiator.NewLine
                    + "Message: " + message + Initiator.NewLine
                    + "Source: " + exceptionSource + Initiator.NewLine
                    + "Stack trace: " + stackTrace + Initiator.NewLine
                    + "Target site's name: " + siteName + Initiator.NewLine
                    + "Base exception message: " + baseExceptionMessage + Initiator.NewLine
                    + "Inner exception message: " + innerExceptionMessage + Initiator.NewLine
                );
                throw;
            }
        }
        
        
        /// <summary>
        /// Produces a SOAP envelope in XML form.
        /// </summary>
        /// <param name="headerContent">Content targeted for between the header tags.</param>
        /// <param name="bodyContent">Content targeted for between the body tags.</param>
        /// <returns><see cref="System.Xml.XmlDocument">The SOAP envelope</see>.</returns>
        public XmlDocument CreateSoapEnvelope(string headerContent, string bodyContent) {
            
            StringBuilder sb = new StringBuilder(_soapEnvelope);

            try {
                sb.Insert(sb.ToString().IndexOf(Initiator.NewLine + "    " + "</soap:Header>"), headerContent);
                sb.Insert(sb.ToString().IndexOf(Initiator.NewLine + "    " + "</soap:Body>"), bodyContent);
            } catch(Exception e) {
                Logger.Instance.Log(e.ToString());
                throw;
            }

            XmlDocument soapEnvelopeXml = new XmlDocument();
            soapEnvelopeXml.LoadXml(sb.ToString());

            return soapEnvelopeXml;
        }


        /// <summary>
        /// Creates a web request based on the url passed in. 
        /// </summary>
        /// <param name="url">The target URL of the server that will be shutdown.</param>
        /// <param name="additionalWebRequestManipulation">delegate of type 
        /// <see cref="System.Action{HttpWebRequest}">Action{HttpWebRequest}</see>.
        /// This is used to take additional tasking defined in the calling procedure.
        /// This procedure creates the web request, 
        /// and passes it into this parameter for the additional initialization work to be performed on the web request.
        /// </param>
        /// <returns>
        /// An initialized <see cref="System.Net.HttpWebRequest">web request</see>,
        /// ready to have a <see cref="System.Xml.XmlDocument">soap envelope</see> inserted.</returns>
        public HttpWebRequest CreateWebRequest(string url, Action<HttpWebRequest> additionalWebRequestManipulation = null) {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
           
            if(additionalWebRequestManipulation != null) {
                additionalWebRequestManipulation(webRequest);
            }
            return webRequest;
        }


        /// <summary>
        /// Insert the <see cref="System.Xml.XmlDocument">soap envelope</see> into the <see cref="System.Net.HttpWebRequest">web request</see>.
        /// </summary>
        /// <param name="soapEnvelopeXml">
        /// The <see cref="System.Xml.XmlDocument">soap envelope</see> to be inserted into the 
        /// <see cref="System.Net.HttpWebRequest">web request</see>.
        /// </param>
        /// <param name="webRequest">The <see cref="System.Net.HttpWebRequest">web request</see> that the 
        /// <see cref="System.Xml.XmlDocument"/>soap envelope</see> is inserted into.
        /// </param>
        public void InsertSoapEnvelopeIntoWebRequest(XmlDocument soapEnvelopeXml, HttpWebRequest webRequest) {
            using (Stream stream = webRequest.GetRequestStream()) {
                soapEnvelopeXml.Save(stream);
            }
        }


        private static void InsertByteArrayIntoWebRequest(byte[] postData, HttpWebRequest webRequest) {
            using (Stream stream = webRequest.GetRequestStream()) {
                stream.Write(postData, 0, postData.Length);
            }
            webRequest.ContentLength = postData.Length;
        }


        /// <summary>
        /// Inserts the credentials from the <see cref="ServerAdminDetails">server admmin details</see> into the 
        /// <see cref="System.Net.HttpWebRequest">web request</see> supplied.
        /// </summary>
        /// <param name="serverAdminDetails">
        /// The <see cref="ServerAdminDetails">server admin details</see>
        /// containing the information for the server that the request will be sent to.
        /// </param>
        /// <param name="webRequest">
        /// The <see cref="System.Net.HttpWebRequest">web request</see> that will have the server administration credentials inserted.
        /// </param>
        public void InsertCredentialsIntoWebRequest(ServerAdminDetails serverAdminDetails, HttpWebRequest webRequest) {
            InsertByteArrayIntoWebRequest(
                new ASCIIEncoding().GetBytes("username=" + serverAdminDetails.UserName + "&password=" + decryptedCredential(serverAdminDetails)),
                webRequest
             );
        }
    }
}

