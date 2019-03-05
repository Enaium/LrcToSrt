using System;
using System.ComponentModel;

namespace LrcToSrt
{
    public class LRC : ObservableBase
    {
        public LRC(string path,int rank)
        {
            _Path = path;
            _Rank = rank;
        }

        public string Path
        {
            get { return _Path; }
            set
            {
                if (value != _Path) return;
                _Path = value;
                RaisePropertyChanged("Path");
            }
        }
        private string _Path;

        public int Rank
        {
            get { return _Rank; }
            set
            {
                if (value == _Rank) return;
                _Rank = value;
                RaisePropertyChanged("Rank");
            }
        }
        private int _Rank;

        public TimeSpan Length
        {
            get { return _Length; }
            set
            {
                if (value == _Length) return;
                _Length = value;
                RaisePropertyChanged("Length");
            }
        }
        private TimeSpan _Length;

        public int Delay
        {
            get { return _Delay; }
            set
            {
                if (value == _Delay) return;
                _Delay = value;
                RaisePropertyChanged("Delay");
            }
        }
        private int _Delay;
    }
}
