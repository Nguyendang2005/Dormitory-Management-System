using System;
using System.Threading.Tasks;
using System.Windows.Input;
using DormCare.Business.DTOs;
using DormCare.Business.Services;
using DormCare.WPF.Commands;

namespace DormCare.WPF.ViewModels
{
    public class InvoiceDetailViewModel : BaseViewModel
    {
        private readonly InvoiceService _invoiceService;
        private readonly int _invoiceId;

        public event Action? RequestClose;

        private InvoiceDetailDto? _invoiceDetail;
        public InvoiceDetailDto? InvoiceDetail
        {
            get => _invoiceDetail;
            set => SetProperty(ref _invoiceDetail, value);
        }

        public ICommand CloseCommand { get; }
        public ICommand RefreshCommand { get; }

        public InvoiceDetailViewModel(InvoiceService invoiceService, int invoiceId)
        {
            Title = "Chi Tiết Hóa Đơn";
            _invoiceService = invoiceService;
            _invoiceId = invoiceId;

            CloseCommand = new RelayCommand(() => RequestClose?.Invoke());
            RefreshCommand = new AsyncRelayCommand(LoadInvoiceDetailAsync);

            _ = LoadInvoiceDetailAsync();
        }

        public async Task LoadInvoiceDetailAsync()
        {
            IsBusy = true;
            try
            {
                InvoiceDetail = await _invoiceService.GetInvoiceDetailsAsync(_invoiceId);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
