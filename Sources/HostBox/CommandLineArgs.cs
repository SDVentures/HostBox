namespace HostBox
{
    internal class CommandLineArgs
    {
        public bool CommandLineArgsValid;

        public string Path { get; set; }

        public string PlaceholderPattern { get; set; }

        public string SharedLibrariesPath { get; set; }

        public bool StartConfirmationRequired { get; set; }

        public bool FinishConfirmationRequired { get; set; }

#if NETCOREAPP3_1
        public bool Web { get; set; }
#endif
    }
}