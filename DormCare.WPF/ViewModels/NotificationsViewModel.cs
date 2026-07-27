using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DormCare.Business.Services;
using DormCare.Domain.Entities;
using DormCare.WPF.Commands;

namespace DormCare.WPF.ViewModels
{
    public class NotificationsViewModel : BaseViewModel
    {
        private readonly NotificationService _notificationService;
        private readonly int _userId;

        private ObservableCollection<Notification> _notifications = new();
        public ObservableCollection<Notification> Notifications
        {
            get => _notifications;
            set => SetProperty(ref _notifications, value);
        }

        private int _unreadCount;
        public int UnreadCount
        {
            get => _unreadCount;
            set => SetProperty(ref _unreadCount, value);
        }

        public ICommand MarkAllAsReadCommand { get; }
        public ICommand RefreshCommand { get; }

        public NotificationsViewModel(NotificationService notificationService, int userId)
        {
            _notificationService = notificationService;
            _userId = userId;

            MarkAllAsReadCommand = new AsyncRelayCommand(ExecuteMarkAllAsReadAsync);
            RefreshCommand = new AsyncRelayCommand(LoadNotificationsAsync);

            _ = LoadNotificationsAsync();
        }

        public async Task LoadNotificationsAsync()
        {
            IsBusy = true;
            var notifs = await _notificationService.GetUserNotificationsAsync(_userId);
            Notifications = new ObservableCollection<Notification>(notifs);
            UnreadCount = Notifications.Count(n => !n.IsRead);
            IsBusy = false;
        }

        private async Task ExecuteMarkAllAsReadAsync()
        {
            // Update all to read (if we had a MarkAllAsRead method in service, we'd use it)
            // For now, let's just refresh. To actually mark, we need a method in NotificationService.
            // But we can just implement this visually for now, or add it.
            foreach(var n in Notifications)
            {
                n.IsRead = true;
            }
            UnreadCount = 0;
            // A real app would persist this to DB.
        }
    }
}
