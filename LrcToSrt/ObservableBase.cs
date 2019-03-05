using System.ComponentModel;

namespace LrcToSrt
{
    /// <summary>
    /// 实现了INotifyPropertyChanged接口通知的轻量级基类
    /// </summary>
    public abstract class ObservableBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}