namespace Nerva.Toolkit
{	
	public static class Version
	{
        public const string VERSION = "0.3.0.0";
        public const string CODE_NAME = "";

        public static readonly string LONG_VERSION = VERSION + (string.IsNullOrEmpty(CODE_NAME) ? "" : ": " + CODE_NAME);
    }
}