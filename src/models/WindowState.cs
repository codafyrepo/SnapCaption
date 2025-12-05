using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SnapCaption.models
{
    public class MainWindowState : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private bool topmost = true;
        private bool captionLogEnabled = true;
        private int captionLogMax = 2;
        private bool latencyShow = false;
        private bool originalCaptionVisible = true;
        private bool nativeCaptionsVisible = false;
        private bool historySelectable = false;

        public bool Topmost
        {
            get => topmost;
            set
            {
                topmost = value;
                OnPropertyChanged("Topmost");
            }
        }
        public bool CaptionLogEnabled
        {
            get => captionLogEnabled;
            set
            {
                captionLogEnabled = value;
                OnPropertyChanged("CaptionLogEnabled");
            }
        }
        public int CaptionLogMax
        {
            get => captionLogMax;
            set
            {
                captionLogMax = value;
                OnPropertyChanged("CaptionLogMax");
            }
        }
        public bool DebugMode
        {
            get => latencyShow;
            set
            {
                latencyShow = value;
                OnPropertyChanged("DebugMode");
            }
        }
        public bool OriginalCaptionVisible
        {
            get => originalCaptionVisible;
            set
            {
                originalCaptionVisible = value;
                OnPropertyChanged("OriginalCaptionVisible");
            }
        }
        public bool NativeCaptionsVisible
        {
            get => nativeCaptionsVisible;
            set
            {
                nativeCaptionsVisible = value;
                OnPropertyChanged("NativeCaptionsVisible");
            }
        }
        public bool HistorySelectable
        {
            get => historySelectable;
            set
            {
                historySelectable = value;
                OnPropertyChanged("HistorySelectable");
            }
        }

        public void OnPropertyChanged([CallerMemberName] string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
            Translator.Setting?.Save();
        }
    }
}
