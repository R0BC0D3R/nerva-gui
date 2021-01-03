using System;
using System.IO;
using AngryWasp.Helpers;
using AngryWasp.Logger;
using AngryWasp.Serializer;
using Nerva.Toolkit.Helpers;

namespace Nerva.Toolkit.Config
{
    public class Configuration
	{
        #region Configuration properties

        public string ToolsPath { get; set; } 

        public bool Testnet { get; set; }
        
        public Daemon Daemon { get; set; }

        public Wallet Wallet { get; set; }

        public bool ReconnectToDaemonProcess { get; set; }

        public string AddressBookPath { get; set; }

        #endregion

        #region Not serialized
        
        [SerializerExclude]
        public bool NewDaemonOnStartup { get; set; } = true;

        [SerializerExclude]
        public bool LogCliWallet { get; set; } = false;

        [SerializerExclude]
        public bool LogCliDaemon{ get; set; } = false;

        #endregion

        public static Configuration New()
        {
            return new Configuration
            {
                ToolsPath = Path.Combine(OS.HomeDirectory, "nerva", "cli"),
                AddressBookPath = Path.Combine(OS.HomeDirectory, "nerva", "address-book.xml"),
                Testnet = false,

                Daemon = Daemon.New(true),
                Wallet = Wallet.New(),

                ReconnectToDaemonProcess = true
            };
        }

        #region Integration code

        private static string loadedConfigFile;
        private static Configuration instance;

        public static string LoadedConfigFile => loadedConfigFile;

        public static Configuration Instance => instance;

        public static void Load(string file, out bool newFile)
        {
            loadedConfigFile = file;

            if (!File.Exists(loadedConfigFile))
            {
                instance = Configuration.New();
                newFile = true;
            }
            else
            {
                Log.Instance.Write($"Configuration loaded from '{loadedConfigFile}'");
                var os = new ObjectSerializer();
                instance = os.Deserialize<Configuration>(XHelper.LoadDocument(loadedConfigFile));
                newFile = false;
            }
        }

        public static void Save()
        {
            new ObjectSerializer().Serialize(instance, loadedConfigFile);
            Log.Instance.Write($"Configuration saved to '{loadedConfigFile}'");
        }

        #endregion
    }
}