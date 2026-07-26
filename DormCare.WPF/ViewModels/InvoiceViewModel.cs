using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DormCare.Business.DTOs;
using DormCare.Business.Services;
using DormCare.WPF.Commands;
using DormCare.WPF.Services;

namespace DormCare.WPF.ViewModels
{
    public class InvoiceViewModel : BaseViewModel
    {
        private readonly InvoiceService _invoiceService;
        private readonly DialogService _dialogService;
        private readonly int? _studentId;

        private ObservableCollection<InvoiceDto> _allInvoices = new();

        private ObservableCollection<InvoiceDto> _invoices = new();
        public ObservableCollection<InvoiceDto> Invoices
        {
            get => _invoices;
            set => SetProperty(ref _invoices, value);
        }

        private InvoiceDto? _selectedInvoice;
        public InvoiceDto? SelectedInvoice
        {
            get => _selectedInvoice;
            set => SetProperty(ref _selectedInvoice, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilters();
                }
            }
        }

        private string _selectedStatusFilter = "All";
        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand PayInvoiceCommand { get; }

        public InvoiceViewModel(InvoiceService invoiceService, DialogService dialogService, int? studentId = null)
        {
            Title = "Quản lý Hóa đơn & Điện nước";
            _invoiceService = invoiceService;
            _dialogService = dialogService;
            _studentId = studentId;

            RefreshCommand = new AsyncRelayCommand(LoadInvoicesAsync);
            PayInvoiceCommand = new AsyncRelayCommand(ExecutePayInvoiceAsync, () => SelectedInvoice != null);

            _ = LoadInvoicesAsync();
        }

        public async Task LoadInvoicesAsync()
        {
            IsBusy = true;
            if (_studentId.HasValue)
            {
                var dtos = await _invoiceService.GetInvoicesByStudentIdAsync(_studentId.Value);
                _allInvoices = new ObservableCollection<InvoiceDto>(dtos);
            }
            else
            {
                var dtos = await _invoiceService.GetAllInvoicesAsync();
                _allInvoices = new ObservableCollection<InvoiceDto>(dtos);
            }
            ApplyFilters();
            IsBusy = false;
        }

        private void ApplyFilters()
        {
            var query = _allInvoices.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(i => i.InvoiceCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                         i.StudentName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                         i.StudentCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SelectedStatusFilter) && SelectedStatusFilter != "All")
            {
                query = query.Where(i => i.Status.Equals(SelectedStatusFilter, StringComparison.OrdinalIgnoreCase));
            }

            Invoices = new ObservableCollection<InvoiceDto>(query);
        }

        private async Task ExecutePayInvoiceAsync()
        {
            if (SelectedInvoice == null) return;
            if (!_dialogService.ShowConfirmation($"Xác nhận thanh toán cho hóa đơn {SelectedInvoice.InvoiceCode} với số tiền {SelectedInvoice.TotalAmount:N0} VNĐ?")) return;

            IsBusy = true;
            var result = await _invoiceService.MarkAsPaidAsync(SelectedInvoice.Id);
            IsBusy = false;

            if (result.IsSuccess)
            {
                _dialogService.ShowInformation(result.Message);
                await LoadInvoicesAsync();
            }
            else
            {
                _dialogService.ShowError(result.Message);
            }
        }
    }
}
