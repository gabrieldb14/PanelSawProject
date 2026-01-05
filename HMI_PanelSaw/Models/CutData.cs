using System.ComponentModel;

namespace HMI_PanelSaw.Models
{
    public class CutData : INotifyPropertyChanged
    {
        private int _cutNumber;
        private double _cutLength;
        private int _repetitions;

        public int CutNumber
        {
            get => _cutNumber;
            set
            {
                _cutNumber = value;
                OnPropertyChanged(nameof(CutNumber));
            }
        }

        public double CutLength
        {
            get => _cutLength;
            set
            {
                _cutLength = value;
                OnPropertyChanged(nameof(CutLength));
            }
        }

        public int Repetitions
        {
            get => _repetitions;
            set
            {
                _repetitions = value;
                OnPropertyChanged(nameof(Repetitions));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}