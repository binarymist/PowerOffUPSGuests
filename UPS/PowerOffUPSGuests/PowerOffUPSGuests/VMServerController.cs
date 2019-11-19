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
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace BinaryMist.PowerOffUPSGuests {

    /// <summary>
    /// Controls the process of shutting down the associated vSphere server.
    /// </summary>
    /// <remarks>
    /// Plays the part of the Concrete Creator, in the Factory Method pattern.
    /// </remarks>
    internal class VMServerController : ServerController {

        private static string _operationIDEndTag = "</operationID>";
        private static string _operationIDTags = "<operationID>" + _operationIDEndTag;
        private uint _operationIDaVal = 0xAC1CF80C;
        private uint _operationIDbVal = 0x00000000;
        private const int EstimatedHeaderSize = 45;

        private enum RequestType {
            Hello, HandShake, Login, Shutdown
        }

        private string _host;
        private string _uRL;
        private readonly string _action = @"""urn:internalvim25/4.1""";
        private readonly string _userAgent = @"VMware VI Client/4.0.0";
        private KeyValuePair<string, string> _cookie;

        
        public VMServerController(ServerAdminDetails serverAdminDetails) : base(serverAdminDetails) {
            
        }


        /// <summary>
        /// Loads the <see cref="ServerController.SoapEnvelopes">SoapEnvelopes</see> with the sequence of messages
        /// that need to be sent to the server in order to perform the shutdown.
        /// </summary>
        /// <remarks>
        /// Factory method implementation
        /// </remarks>
        public override void AssembleRequests() {
            Logger.Instance.LogTrace();
            _host = "https://" + ServerAdminDetails.ServerName + ":" +ServerAdminDetails.ServerPort;
            _uRL = "/sdk";
            SoapEnvelopes.Enqueue(CreateHelloEnvelope());
            SoapEnvelopes.Enqueue(CreateHandshakeEnvelope());
            SoapEnvelopes.Enqueue(CreateLoginEnvelope());
            SoapEnvelopes.Enqueue(CreateShutdownEnvelope());
        }


        private string InitialHeaderContent() {
            StringBuilder headerContent = new StringBuilder(_operationIDTags, EstimatedHeaderSize);
            headerContent.Insert(headerContent.ToString().IndexOf(_operationIDEndTag), _operationIDaVal.ToString("X8") + "-" + (++_operationIDbVal).ToString("X8"));
            return headerContent.ToString();
        }


        private XmlDocument CreateHelloEnvelope() {
            Logger.Instance.LogTrace();
            string bodyContent = @"
    <RetrieveServiceContent xmlns=""urn:internalvim25"">
      <_this xsi:type=""ManagedObjectReference"" type=""ServiceInstance"" serverGuid="""">ServiceInstance</_this>
    </RetrieveServiceContent>";
            return RequestAssembler.CreateSoapEnvelope(InitialHeaderContent(), bodyContent);
        }

        
        private XmlDocument CreateHandshakeEnvelope() {
            Logger.Instance.LogTrace();
            string bodyContent = @"
    <RetrieveInternalContent xmlns=""urn:internalvim25"">
      <_this xsi:type=""ManagedObjectReference"" type=""ServiceInstance"" serverGuid="""">ServiceInstance</_this>
    </RetrieveInternalContent>";
            return RequestAssembler.CreateSoapEnvelope(InitialHeaderContent(), bodyContent);
        }

        
        private XmlDocument CreateLoginEnvelope() {
            Logger.Instance.LogTrace();
            string bodyContent = @"
    <Login xmlns=""urn:internalvim25"">
      <_this xsi:type=""ManagedObjectReference"" type=""SessionManager"" serverGuid="""">ha-sessionmgr</_this>
      <userName></userName>
      <password></password>
      <locale>en_US</locale>
    </Login>";
            try {
                // As VMware insist on putting credentials in the SOAP body what else can we do?
                return RequestAssembler.CreateLoginSoapEnvelope(InitialHeaderContent(), bodyContent, ServerAdminDetails);
            } catch(InvalidCredentialException e) {
                string error = string.Format(
                    "Error occured during a call to CreateLoginSoapEnvelope, for server: {0}.{1}Exception details follow.{1}{2}",
                    ServerAdminDetails.ServerName,
                    NewLine,
                    e
                );
                Logger.Instance.Log(error);
                throw new Exception(error);
            }
        }

        
        private XmlDocument CreateShutdownEnvelope() {
            Logger.Instance.LogTrace();
            string bodyContent = @"
    <ShutdownHost_Task xmlns=""urn:internalvim25"">
      <_this xsi:type=""ManagedObjectReference"" type=""HostSystem"" serverGuid="""">ha-host</_this>
      <force>true</force>
    </ShutdownHost_Task>";
            return RequestAssembler.CreateSoapEnvelope(InitialHeaderContent(), bodyContent);
        }


        private HttpWebRequest CreateWebRequest(string uRL, KeyValuePair<string, string> cookie) {
            return RequestAssembler.CreateWebRequest(
                uRL,
                (request)=>{
                    request.Method = RequestMethod.Post.ToString();
                    request.UserAgent = _userAgent;
                    request.ContentType = "text/xml; charset=\"utf-8\"";
                    request.Headers.Add("SOAPAction", _action);
                    request.Accept = "text/xml";
                    request.KeepAlive = true;

                    if (!string.IsNullOrEmpty(cookie.Key))
                        request.Headers.Add("Cookie", cookie.Key + "=" + cookie.Value);
                }
            );
        }


        private void DequeueSendRequestProcessResponse(RequestType requestType, KeyValuePair<string, string> cookie, Action<HttpWebResponse> additionalResponseProcessing = null) {
            Logger.Instance.Log(string.Format("Will now attempt sending {0} message to server: {1}", requestType, ServerAdminDetails.ServerName));
            XmlDocument soapEnvelope = SoapEnvelopes.Dequeue();
            HttpWebRequest httpWebRequest = CreateWebRequest(_host + _uRL, cookie);
            RequestAssembler.InsertSoapEnvelopeIntoWebRequest(soapEnvelope, httpWebRequest);

            string soapResult;
            using (HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponse())
            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader streamReader = new StreamReader(responseStream)) {
                //pull out the bits we need for the next request.

                if (additionalResponseProcessing != null) {
                    additionalResponseProcessing(response);
                }

                soapResult = streamReader.ReadToEnd();
            }
        }


        /// <summary>
        /// Perform the shutdown of the server specified in the <see cref="ServerAdminDetails">server admin details</see>.
        /// </summary>
        public override void Shutdown() {

            bool serverOnline = SimplePing();
            Logger.Instance.Log(
                serverOnline
                    ? string.Format("Initiating sending of {0} to server: {1}", RequestType.Hello, ServerAdminDetails.ServerName)
                    : string.Format("Could not reach server: {0}. Aborting shutdown of server: {0}", ServerAdminDetails.ServerName)
            );

            ServicePointManager.ServerCertificateValidationCallback += ValidateRemoteCertificate;
            
            KeyValuePair<string, string> emptyCookie = new KeyValuePair<string, string>();
            DequeueSendRequestProcessResponse(
                RequestType.Hello,
                emptyCookie,
                (response)=> {
                    string[] setCookieElementsResponse = response.Headers["Set-Cookie"].Split(new[] { "\"" }, StringSplitOptions.RemoveEmptyEntries);
                    _cookie = new KeyValuePair<string, string>(setCookieElementsResponse[0].TrimEnd('='), "\"" + setCookieElementsResponse[1] + "\"");
                }
            );
            DequeueSendRequestProcessResponse(RequestType.HandShake, _cookie);
            DequeueSendRequestProcessResponse(RequestType.Login, _cookie);
            DequeueSendRequestProcessResponse(RequestType.Shutdown, _cookie);
            NotifyOfShutdown();
        }
        

        private static string RemoteCertificateDetails(X509Certificate certificate) {
            return string.Format(
                "Details of the certificate provided by the remote party are as follows:" + NewLine +
                "Subject: {0}" + NewLine +
                "Issuer: {1}",
                certificate.Subject,
                certificate.Issuer
            );
        }


        // callback used to validate the certificate in an SSL conversation
        private bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors) {
            bool ignoreSslErrors;
#if DEBUG
            return true;
#endif
            if (bool.TryParse(ConfigReader.Read["IgnoreSslErrors"], out ignoreSslErrors) && ignoreSslErrors) {
                if (
                    string.Compare(certificate.Issuer, "O=VMware Installer", true) == 0 &&
                    certificate.Subject.Contains("CN=" + ConfigReader.Read[ServerAdminDetails.ServerName])
                ) {
                    return true;
                }
                Logger.Instance.Log(
                    "The certificate provided by who we thought was the remote server appears to be invalid." + NewLine +
                    RemoteCertificateDetails(certificate)
                );
                return false;
            }
            if(policyErrors == SslPolicyErrors.None) {
                return true;
            }
            Logger.Instance.Log(
                RemoteCertificateDetails(certificate) +
                string.Format("The value of SslPolicyErrors was {0}.", policyErrors)
            );
            return false;
        }
    }
}

