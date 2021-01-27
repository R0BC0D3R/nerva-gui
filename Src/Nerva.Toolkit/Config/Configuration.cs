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

        public string AddressBookPath { get; set; }

        #endregion

        public static Configuration New()
        {
            return new Configuration
            {
                ToolsPath = Path.Combine(storageDirectory, "cli"),
                AddressBookPath = Path.Combine(storageDirectory, "address-book.xml"),
                Testnet = false,

                Daemon = Daemon.New(false),
                Wallet = Wallet.New()
            };
        }

        #region Integration code

        private static string loadedConfigFile;
        private static Configuration instance;

        private static readonly string storageDirectory = Path.Combine(OS.HomeDirectory, ".nerva-gui");

        public static string LoadedConfigFile => loadedConfigFile;

        public static Configuration Instance => instance;

        public static string StorageDirectory => storageDirectory;

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