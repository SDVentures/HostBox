namespace HostBox
{
    internal class CommandLineArgs
    {
        public bool CommandLineArgsValid;

        public string Path { get; set; }

        public string PlaceholderPattern { get; set; }

        public string EnvPlaceholderPattern { get; set; }

        public string SharedLibrariesPath { get; set; }

        public bool StartConfirmationRequired { get; set; }

        public bool FinishConfirmationRequired { get; set; }

        public bool Web { get; set; }
    }
}