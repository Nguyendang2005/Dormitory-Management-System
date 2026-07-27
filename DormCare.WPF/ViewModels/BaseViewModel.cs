using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DormCare.WPF.ViewModels
{
    /// <summary>Loại thông báo hiển thị inline trên UI.</summary>
    public enum StatusType { None, Success, Error, Warning, Info }

    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        // ─── Inline Status Notification ───────────────────────────────────────
        private string _statusMessage = string.Empty;
        /// <summary>Thông báo hiển thị inline (lỗi, thành công, cảnh báo).</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (SetProperty(ref _statusMessage, value))
                    OnPropertyChanged(nameof(HasStatusMessage));
            }
        }

        private StatusType _statusType = StatusType.None;
        public StatusType CurrentStatusType
        {
            get => _statusType;
            set => SetProperty(ref _statusType, value);
        }

        public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

        protected void SetError(string message)
        {
            CurrentStatusType = StatusType.Error;
            StatusMessage = message;
        }

        protected void SetSuccess(string message)
        {
            CurrentStatusType = StatusType.Success;
            StatusMessage = message;
        }

        protected void SetWarning(string message)
        {
            CurrentStatusType = StatusType.Warning;
            StatusMessage = message;
        }

        protected void ClearStatus()
        {
            StatusMessage = string.Empty;
            CurrentStatusType = StatusType.None;
        }
    }
}
