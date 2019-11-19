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
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using System.Xml;


namespace BinaryMist.PowerOffUPSGuests {

    /// <summary>
    /// Reads the libraries configuration into memory.
    /// Provides convienient readonly access of the configuration from a single instance.
    /// </summary>
    public class ConfigReader {
        
        #region singleton initialization
        private static readonly Lazy<ConfigReader> _instance = new Lazy<ConfigReader>(() => new ConfigReader());
        
        
        /// <summary>
        /// constructor that sets the value of our "_settings" variable
        /// </summary>
        private ConfigReader() {
            _settings = ReadConfig(Assembly.GetCallingAssembly());
        }

        
        /// <summary>
        /// The first time Read is called must be from within this assembly,
        /// in order to create the instance of this class for the containing assembly.
        /// </summary>
        public static ConfigReader Read {
            get {
                return _instance.Value;
            }
        }
        #endregion


        /// <summary>
        /// settings to be used throughout the class
        /// </summary>
        private IDictionary _settings;
        /// <summary>
        /// constant name for the node name we're looking for
        /// </summary>
        private const string NodeName = "assemblySettings";


        /// <summary>
        /// class Indexer.
        /// Provides the value of the specified key in the config file.
        /// If the key doesn't exist, an empty string is returned.
        /// </summary>
        public string this[string key] {
            get {
                string settingValue = null;

                if (_settings != null) {
                    settingValue = _settings[key] as string;
                }
                
                return settingValue ?? string.Empty;
            }
        }


        public IDictionary<string, string> AllKeyVals {
            get {
                IDictionary<string, string> settings = new Dictionary<string, string>();
                foreach(DictionaryEntry item in _settings) {
                    settings.Add((string)item.Key, (string)item.Value);
                }
                return settings;
            }
        }


        /// <summary>
        /// Open and parse the config file for the provided assembly
        /// </summary>
        /// <param name="assembly">The assembly that has a config file.</param>
        /// <returns></returns>
        private static IDictionary ReadConfig(Assembly assembly) {
            try {
                string cfgFile = assembly.CodeBase + ".config";
                
                XmlDocument doc = new XmlDocument();
                doc.Load(new XmlTextReader(cfgFile));
                XmlNodeList nodes = doc.GetElementsByTagName(NodeName);
                
                foreach (XmlNode node in nodes) {
                    if (node.LocalName == NodeName) {
                        DictionarySectionHandler handler = new DictionarySectionHandler();
                        return (IDictionary)handler.Create(null, null, node);
                    }
                }
            } catch (Exception e) {
                Logger.Instance.Log(e.Message);
            }

            return (null);
        }


        #region Config related settings


        /// <summary>
        /// Readonly value, specifying whether debug is set to true in the assemblySettings of the BinaryMist.PowerOffUPSGuests.dll.config file.
        /// </summary>
        public bool Debug {
            get {
                if (_debug != null)
                    return _debug == true ? true : false;
                _debug = Read["Debug"] == "true" ? true : false;
                return _debug == true ? true : false;
            }
        }
        private bool? _debug;

        #endregion

    }
}

