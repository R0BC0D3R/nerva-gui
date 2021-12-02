using System.IO;
using AngryWasp.Helpers;
using AngryWasp.Logger;
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
                Log.Instance.Write("Config.SetMissingElements: Instance was null. Created new one");
            }
            else 
            {
                if(string.IsNullOrEmpty(instance.ToolsPath))
                {
                    instance.ToolsPath = Path.Combine(storageDirectory, "cli");
                    Log.Instance.Write("Config.SetMissingElements: ToolsPath was null. Set to: " + instance.ToolsPath);
                }

                if(string.IsNullOrEmpty(instance.AddressBookPath))
                {
                    instance.AddressBookPath = Path.Combine(storageDirectory, "address-book.xml");
                    Log.Instance.Write("Config.SetMissingElements: AddressBookPath was null. Set to: " + instance.AddressBookPath);
                }

                if(instance.Daemon == null)
                {
                    instance.Daemon = Daemon.New(false);
                    Log.Instance.Write("Config.SetMissingElements: Daemon was null. Created new one");
                }

                if(instance.Wallet == null)
                {
                    instance.Wallet = Wallet.New();
                    Log.Instance.Write("Config.SetMissingElements: Wallet was null. Created new one");
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
                    Log.Instance.Write($"Configuration loaded from '{loadedConfigFile}'");
                    var os = new ObjectSerializer();
                    instance = os.Deserialize<Configuration>(XHelper.LoadDocument(loadedConfigFile));
                }
                catch
                {
                    Log.Instance.Write(Log_Severity.Fatal, $"There is an error loading the config file. Delete file {file} and try again");
                }
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