namespace UWUVCI_AIO_WPF.Models
{
    public class JsonAppSettings
    {
        public bool PathsSet { get; set; } = false;
        public string BasePath { get; set; } = "";
        public string OutPath { get; set; } = "";
        public string Ckey { get; set; } = "";
        public bool SetBaseOnce { get; set; } = false;
        public bool SetOutOnce { get; set; } = false;
        public bool UpgradeRequired { get; set; } = true;
        public string SysKey { get; set; } = "";
        public string SysKey1 { get; set; } = "";
        public bool dont { get; set; } = false;
        public bool ndsw { get; set; } = false;
        public bool snesw { get; set; } = false;
        public bool gczw { get; set; } = false;
        public string Ancast { get; set; } = "";
        // 0 requires the quiz; 1 records a successful pass.
        public int QuizScreen { get; set; } = 0;
    }

}
