using System;

namespace Nerva.Desktop.Helpers
{
    public static class Conversions
    {
        public static double FromAtomicUnits4Places(ulong i) => Math.Round((double)i / 1000000000000.0d, 4);

        public static double FromAtomicUnits(ulong i) =>  (double)i / 1000000000000.0d;

        public static ulong ToAtomicUnits(double i) => (ulong)(i * 1000000000000.0d);

        public static string WalletAddressShortForm(string a)
        {
            if (string.IsNullOrEmpty(a))
                return null;

            return $"{a.Substring(0, 10)}...{a.Substring(a.Length - 10, 10)}";
        }

        public static int VersionStringToInt(string vs)
        {
            int version = 0;

            try
            {
                string versionNoPeriods = vs.Replace(".", "").Replace(" ", "").Trim();
                bool isNumber = int.TryParse(versionNoPeriods, out int versionNumeric);
                if(isNumber)
                {
                    version = versionNumeric;
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("CV.VSTI", ex, false);
            }

            return version;
        }
    }
}