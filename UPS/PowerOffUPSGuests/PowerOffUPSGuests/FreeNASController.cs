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
using System.Security.Cryptography.X509Certificates;

namespace BinaryMist.PowerOffUPSGuests {

    /// <summary>
    /// Controls the process of shutting down the associated FreeNAS server.
    /// </summary>
    /// <remarks>
    /// Plays the part of the Concrete Creator, in the Factory Method pattern.
    /// </remarks>
    internal class FreeNASController : ServerController {

        private string _host;
        private readonly string _userAgent = @"PowerOffUPSGuests:-)";
        private readonly string _accept = @"text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
        private readonly string _acceptLanguage = @"en-us,en;q=0.5";
        private readonly string _acceptEncoding = @"gzip,deflate";
        private readonly string _acceptCharset = @"ISO-8859-1,utf-8;q=0.7,*;q=0.7";

        public FreeNASController(ServerAdminDetails serverAdminDetails) : base(serverAdminDetails) {
            
        }


        /// <summary>
        /// Factory method implementation
        /// </summary>
        public override void AssembleRequests() {

            _host = "https://" + ServerAdminDetails.ServerName + ":" + ServerAdminDetails.ServerPort;
        }


        private HttpWebRequest CreateWebRequest(
            string uRL,
            RequestMethod requestMethod,
            string contentType = null,
            KeyValuePair<string, string> cacheControl = new KeyValuePair<string, string>(),
            string referer = null,
            KeyValuePair<string, string> cookie = new KeyValuePair<string, string>()) {
            return RequestAssembler.CreateWebRequest(
                uRL,
                (request) => {
                    request.Method = requestMethod.ToString();
                    request.UserAgent = _userAgent;
                    if(contentType != null)
                        request.ContentType = contentType;
                    request.Accept = _accept;
                    request.Headers.Add("Accept-Language", _acceptLanguage);
                    request.Headers.Add("Accept-Encoding", _acceptEncoding);
                    request.Headers.Add("Accept-Charset", _acceptCharset);
                    request.Headers.Add("Keep-Alive", "115");
                   //request.Headers.Add("Connection", "keep-alive");
                    if(!string.IsNullOrEmpty(cacheControl.Key))
                        request.Headers.Add(cacheControl.Key, cacheControl.Value);
                    if(!string.IsNullOrEmpty(referer))
                        request.Referer = referer;
                    if(!string.IsNullOrEmpty(cookie.Key))
                        request.Headers.Add("Cookie", cookie.Key + "=" + cookie.Value);
                }
            );
        }


        public override void Shutdown() {

            bool serverOnline = SimplePing();
            Logger.Instance.Log(
                serverOnline
                    ? string.Format("Initiating sending of hellow to server: {0}", ServerAdminDetails.ServerName)
                    : string.Format("Could not reach server: {0}. Aborting shutdown of server: {0}", ServerAdminDetails.ServerName)
            );
            HttpWebRequest helloHttpWebRequest = CreateWebRequest(
                _host + "/",
                RequestMethod.Get,
                cacheControl: new KeyValuePair<string, string>("Cache-Control", "max-age=0"));
            ServicePointManager.ServerCertificateValidationCallback += ValidateRemoteCertificate;
            string helloResponse;
            KeyValuePair<string, string> cookie;
            using (HttpWebResponse response = (HttpWebResponse)helloHttpWebRequest.GetResponse())

            // So I ran into some trouble here.
            // When the above GetResponse is executed, two requests are made to the web service and I'm not sure why.
            // The ValidateRemoteCertificate delegate method below gets invoked twice.
            // To save the head ach of working out why this was happeing, I decided to just try plugging the FreeNAS box into the UPS via USB.
            // This worked.
            // So currently FreeNASController isn't being used.

            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader streamReader = new StreamReader(responseStream)) {
                //pull out the bits we need for the next request.
                helloResponse = streamReader.ReadToEnd();
                string[] setCookieElementsResponse = response.Headers["Set-Cookie"].Split(new[] {"=", ";", "\"" }, StringSplitOptions.RemoveEmptyEntries);
                cookie = new KeyValuePair<string, string>(setCookieElementsResponse[0].TrimEnd('='), "\"" + setCookieElementsResponse[1] + "\"");
            }


            Logger.Instance.Log(string.Format("Will now attempt sending login message to server: {0}", ServerAdminDetails.ServerName));
            HttpWebRequest loginHttpWebRequest = CreateWebRequest(
                _host + "/login.php",
                RequestMethod.Post,
                referer: _host + "/login.php",
                cookie: cookie,
                contentType: "application/x-www-form-urlencoded"
            );
            RequestAssembler.InsertCredentialsIntoWebRequest(ServerAdminDetails, loginHttpWebRequest);
            string loginResponse;
            using (HttpWebResponse response = (HttpWebResponse)loginHttpWebRequest.GetResponse())
            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader streamReader = new StreamReader(responseStream)) {
                //pull out the bits we need for the next request.
                loginResponse = streamReader.ReadToEnd();
            }


            Logger.Instance.Log(string.Format("Will now attempt to GET of the shutdown authtoken from server: {0}", ServerAdminDetails.ServerName));
            HttpWebRequest getShutdownHttpWebRequest = CreateWebRequest(
                _host + "/shutdown.php",
                RequestMethod.Get,
                referer: _host + "/index.php",
                cookie: cookie
            );
            string getShutdownResponse;
            using (HttpWebResponse response = (HttpWebResponse)getShutdownHttpWebRequest.GetResponse())
            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader streamReader = new StreamReader(responseStream)) {
                //pull out the bits we need for the next request.
                getShutdownResponse = streamReader.ReadToEnd();
            }
            //dig out the authtoken





            Logger.Instance.Log(string.Format("Will now attempt to POST shutdown of the server: {0}", ServerAdminDetails.ServerName));


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
            if (policyErrors == SslPolicyErrors.None) {
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
