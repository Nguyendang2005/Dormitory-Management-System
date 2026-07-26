using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DormCare.Business.DTOs;
using DormCare.Business.Services;
using DormCare.WPF.Commands;
using DormCare.WPF.Services;
using DormCare.WPF.Views.Manager;

namespace DormCare.WPF.ViewModels
{
    public class InvoiceViewModel : BaseViewModel
    {
        private readonly InvoiceService _invoiceService;
        private readonly PaymentService _paymentService;
        private readonly StudentService _studentService;
        private readonly RoomService _roomService;
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

        // Summary Statistics Properties
        private int _totalInvoiceCount;
        public int TotalInvoiceCount
        {
            get => _totalInvoiceCount;
            set => SetProperty(ref _totalInvoiceCount, value);
        }

        private decimal _totalAmountSum;
        public decimal TotalAmountSum
        {
            get => _totalAmountSum;
            set => SetProperty(ref _totalAmountSum, value);
        }

        private decimal _unpaidAmountSum;
        public decimal UnpaidAmountSum
        {
            get => _unpaidAmountSum;
            set => SetProperty(ref _unpaidAmountSum, value);
        }

        private decimal _paidAmountSum;
        public decimal PaidAmountSum
        {
            get => _paidAmountSum;
            set => SetProperty(ref _paidAmountSum, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand CreateInvoiceCommand { get; }
        public ICommand ViewInvoiceDetailsCommand { get; }
        public ICommand UpdatePaymentCommand { get; }
        public ICommand DeleteInvoiceCommand { get; }
        public ICommand FilterUnpaidCommand { get; }
        public ICommand FilterAllCommand { get; }

        public InvoiceViewModel(
            InvoiceService invoiceService,
            PaymentService paymentService,
            StudentService studentService,
            RoomService roomService,
            DialogService dialogService,
            int? studentId = null)
        {
            Title = "Quản lý Hóa đơn & Điện nước";
            _invoiceService = invoiceService;
            _paymentService = paymentService;
            _studentService = studentService;
            _roomService = roomService;
            _dialogService = dialogService;
            _studentId = studentId;

            RefreshCommand = new AsyncRelayCommand(LoadInvoicesAsync);
            CreateInvoiceCommand = new RelayCommand(ExecuteCreateInvoice);
            ViewInvoiceDetailsCommand = new RelayCommand(ExecuteViewInvoiceDetails, () => SelectedInvoice != null);
            UpdatePaymentCommand = new RelayCommand(ExecuteUpdatePayment, () => SelectedInvoice != null && SelectedInvoice.Status != "Paid");
            DeleteInvoiceCommand = new AsyncRelayCommand(ExecuteDeleteInvoiceAsync, () => SelectedInvoice != null && SelectedInvoice.Status != "Paid" && SelectedInvoice.Status != "Overdue");
            FilterUnpaidCommand = new RelayCommand(() => SelectedStatusFilter = "Unpaid");
            FilterAllCommand = new RelayCommand(() => SelectedStatusFilter = "All");

            _ = LoadInvoicesAsync();
        }

        public async Task LoadInvoicesAsync()
        {
            IsBusy = true;
            try
            {
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
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplyFilters()
        {
            var query = _allInvoices.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(i => i.InvoiceCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                         i.StudentName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                         i.StudentCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                         i.RoomNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                         i.BuildingName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SelectedStatusFilter) && SelectedStatusFilter != "All")
            {
                query = query.Where(i => i.Status.Equals(SelectedStatusFilter, StringComparison.OrdinalIgnoreCase));
            }

            Invoices = new ObservableCollection<InvoiceDto>(query);

            // Update Summary Statistics based on total loaded invoices
            TotalInvoiceCount = _allInvoices.Count;
            TotalAmountSum = _allInvoices.Sum(i => i.TotalAmount);
            PaidAmountSum = _allInvoices.Sum(i => i.TotalPaid);
            UnpaidAmountSum = _allInvoices.Sum(i => i.RemainingBalance);
        }

        private void ExecuteCreateInvoice()
        {
            var createVm = new CreateInvoiceViewModel(_invoiceService, _studentService, _roomService);
            var dialog = new CreateInvoiceWindow
            {
                DataContext = createVm,
                Owner = Application.Current?.MainWindow
            };

            createVm.RequestClose += async (success) =>
            {
                dialog.DialogResult = success;
                dialog.Close();
                if (success)
                {
                    _dialogService.ShowInformation("Tạo hóa đơn mới thành công!");
                    await LoadInvoicesAsync();
                }
            };

            dialog.ShowDialog();
        }

        private void ExecuteViewInvoiceDetails()
        {
            if (SelectedInvoice == null) return;

            var detailVm = new InvoiceDetailViewModel(_invoiceService, SelectedInvoice.Id);
            var dialog = new InvoiceDetailWindow
            {
                DataContext = detailVm,
                Owner = Application.Current?.MainWindow
            };

            detailVm.RequestClose += () => dialog.Close();
            dialog.ShowDialog();
        }

        private void ExecuteUpdatePayment()
        {
            if (SelectedInvoice == null) return;

            var paymentVm = new PaymentViewModel(_paymentService, SelectedInvoice);
            var dialog = new PaymentWindow
            {
                DataContext = paymentVm,
                Owner = Application.Current?.MainWindow
            };

            paymentVm.RequestClose += async (success) =>
            {
                dialog.DialogResult = success;
                dialog.Close();
                if (success)
                {
                    _dialogService.ShowInformation("Cập nhật thanh toán thành công!");
                    await LoadInvoicesAsync();
                }
            };

            dialog.ShowDialog();
        }

        private async Task ExecuteDeleteInvoiceAsync()
        {
            if (SelectedInvoice == null) return;

            bool confirm = _dialogService.ShowConfirmation($"Bạn có chắc chắn muốn xóa hóa đơn '{SelectedInvoice.InvoiceCode}' của sinh viên {SelectedInvoice.StudentName}?");
            if (!confirm) return;

            IsBusy = true;
            var result = await _invoiceService.DeleteInvoiceAsync(SelectedInvoice.Id);
            IsBusy = false;

            if (result.IsSuccess)
            {
                _dialogService.ShowInformation("Xóa hóa đơn thành công!");
                await LoadInvoicesAsync();
            }
            else
            {
                _dialogService.ShowError(result.Message);
            }
        }
    }
}
