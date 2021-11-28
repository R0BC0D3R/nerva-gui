using System;
using AngryWasp.Logger;

namespace Nerva.Toolkit.Helpers
{
    public static class Conversions
    {
        public static double FromAtomicUnits(ulong i) => Math.Round((double)i / 1000000000000.0d, 4);

        public static ulong ToAtomicUnits(double i) => (ulong)(i * 1000000000000.0d);

        public static string WalletAddressShortForm(string a)
        {
            if (string.IsNullOrEmpty(a))
                return null;

            return $"{a.Substring(0, 10)}...{a.Substring(a.Length - 10, 10)}";
        }

        public static uint VersionStringToInt(string vs)
        {
            string[] split = vs.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            ushort[] converted = new ushort[split.Length];

            for (int i = 0; i < split.Length; i++)
                if (!ushort.TryParse(split[i], out converted[i]))
                {
                    Log.Instance.Write(Log_Severity.Error, "Attempt to parse poorly formatted version string");
                    return 0;
                }
            
            switch (split.Length)
            {
                case 3:
                    return (uint)((converted[0] << 24) + (converted[1] << 16) + converted[2]);
                case 4:
                    return (uint)((converted[0] << 24) + (converted[1] << 16) + (converted[1] << 8) + converted[2]);
                default:
                    Log.Instance.Write(Log_Severity.Error, $"Attempt to convert version string with {split.Length} values");
                    return 0;
            }
        }
    }
}