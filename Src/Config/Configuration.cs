using System;
using System.IO;
using AngryWasp.Helpers;
using AngryWasp.Serializer;
using Nerva.Desktop.Helpers;

namespace Nerva.Desktop.Config
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

        public static void SetMissingElements()
        {
            if(instance == null)
            {
                instance = Configuration.New();
                Logger.LogDebug("CON.SME", "Config.SetMissingElements: Instance was null. Created new one");
            }
            else 
            {
                if(string.IsNullOrEmpty(instance.ToolsPath))
                {
                    instance.ToolsPath = Path.Combine(storageDirectory, "cli");
                    Logger.LogDebug("CON.SME", "Config.SetMissingElements: ToolsPath was null. Set to: " + instance.ToolsPath);
                }

                if(string.IsNullOrEmpty(instance.AddressBookPath))
                {
                    instance.AddressBookPath = Path.Combine(storageDirectory, "address-book.xml");
                    Logger.LogDebug("CON.SME", "Config.SetMissingElements: AddressBookPath was null. Set to: " + instance.AddressBookPath);
                }

                if(instance.Daemon == null)
                {
                    instance.Daemon = Daemon.New(false);
                    Logger.LogDebug("CON.SME", "Config.SetMissingElements: Daemon was null. Created new one");
                }

                if(instance.Wallet == null)
                {
                    instance.Wallet = Wallet.New();
                    Logger.LogDebug("CON.SME", "Config.SetMissingElements: Wallet was null. Created new one");
                }
            }            
        }

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
                newFile = false;

                try
                {
                    Logger.LogDebug("CON.LD", $"Configuration loaded from '{loadedConfigFile}'");
                    var os = new ObjectSerializer();
                    instance = os.Deserialize<Configuration>(XHelper.LoadDocument(loadedConfigFile));
                }
                catch(Exception ex)
                {
                    ErrorHandler.HandleException("CON.LD", ex, $"There is an error loading the config file. Delete file {file} and try again", true);
                }
            }
        }

        public static void Save()
        {
            new ObjectSerializer().Serialize(instance, loadedConfigFile);
            Logger.LogDebug("CON.SV", $"Configuration saved to '{loadedConfigFile}'");
        }

        #endregion
    }
}