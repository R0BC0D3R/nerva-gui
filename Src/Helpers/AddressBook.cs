using System.Collections.Generic;
using System.IO;
using AngryWasp.Helpers;
using AngryWasp.Serializer;
using Nerva.Desktop.Config;

namespace Nerva.Desktop.Helpers
{
    public class AddressBookEntry
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Address { get; set; }

        public string PaymentId { get; set; }
    }
     
    public class AddressBook
    {
        [SerializerInclude]
        private List<AddressBookEntry> entries;
        private static AddressBook instance;
        private static readonly string file = Path.Combine(Configuration.StorageDirectory, "AddressBook.xml");

        public List<AddressBookEntry> Entries => entries;
        public static AddressBook Instance => instance;

        public static AddressBook New()
        {
            return new AddressBook
            {
                entries = new List<AddressBookEntry>()
            };
        }

        public static void Load()
        {
            if (!File.Exists(file))
            {
                instance = New();
        
                Logger.LogDebug("AB.LOAD", $"Address Book created at '{file}'");
                new ObjectSerializer().Serialize(instance, file);
            }
            else
            {
                Logger.LogDebug("AB.LOAD", $"Address Book loaded from '{file}'");
                instance = new ObjectSerializer().Deserialize<AddressBook>(XHelper.LoadDocument(file));
            }
        }

        public static void Save()
        {
            new ObjectSerializer().Serialize(instance, file);
            Logger.LogDebug("AB.SAVE", $"Address Book saved to '{file}'");
        }
    }
}