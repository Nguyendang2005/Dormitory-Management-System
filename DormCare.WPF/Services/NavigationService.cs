using System;

namespace DormCare.WPF.Services
{
    public class NavigationService
    {
        public event Action<object>? OnCurrentViewChanged;

        private object? _currentView;
        public object? CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnCurrentViewChanged?.Invoke(_currentView!);
            }
        }

        public void NavigateTo(object viewModel)
        {
            CurrentView = viewModel;
        }
    }
}
