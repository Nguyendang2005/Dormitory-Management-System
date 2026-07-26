using System;
using System.Threading.Tasks;
using System.Windows.Input;
using DormCare.Business.DTOs;
using DormCare.Business.Services;
using DormCare.WPF.Commands;

namespace DormCare.WPF.ViewModels
{
    public class PaymentViewModel : BaseViewModel
    {
        private readonly PaymentService _paymentService;
        private readonly InvoiceDto _invoice;
        private readonly int? _currentUserId;

        public event Action<bool>? RequestClose;

        public string InvoiceCode => _invoice.InvoiceCode;
        public string StudentName => _invoice.StudentName;
        public decimal TotalAmount => _invoice.TotalAmount;
        public decimal TotalPaid => _invoice.TotalPaid;
        public decimal RemainingBalance => _invoice.RemainingBalance;

        private decimal _amountToPay;
        public decimal AmountToPay
        {
            get => _amountToPay;
            set
            {
                if (SetProperty(ref _amountToPay, value))
                {
                    OnPropertyChanged(nameof(VietQrImageUrl));
                }
            }
        }

        private string _paymentMethod = "BankTransfer";
        public string PaymentMethod
        {
            get => _paymentMethod;
            set
            {
                if (SetProperty(ref _paymentMethod, value))
                {
                    OnPropertyChanged(nameof(IsBankTransfer));
                    OnPropertyChanged(nameof(VietQrImageUrl));
                }
            }
        }

        public bool IsBankTransfer => PaymentMethod == "BankTransfer";

        public string TransferContent => $"PT07_{InvoiceCode}";

        public string VietQrImageUrl => $"https://img.vietqr.io/image/BIDV-123456789-compact2.png?amount={(long)Math.Max(0, AmountToPay)}&addInfo={Uri.EscapeDataString(TransferContent)}&accountName=BQL%20KY%20TUC%20XA%20DORMCARE";

        private string _transactionReference = string.Empty;
        public string TransactionReference
        {
            get => _transactionReference;
            set => SetProperty(ref _transactionReference, value);
        }

        private string _note = string.Empty;
        public string Note
        {
            get => _note;
            set => SetProperty(ref _note, value);
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand PayFullCommand { get; }
        public ICommand SubmitCommand { get; }
        public ICommand CancelCommand { get; }

        public PaymentViewModel(PaymentService paymentService, InvoiceDto invoice, int? currentUserId = null)
        {
            Title = $"Cập Nhật Thanh Toán — {invoice.InvoiceCode}";
            _paymentService = paymentService;
            _invoice = invoice;
            _currentUserId = currentUserId;

            AmountToPay = _invoice.RemainingBalance;
            Note = $"Thanh toán hóa đơn {_invoice.InvoiceCode}";

            PayFullCommand = new RelayCommand(() => AmountToPay = RemainingBalance);
            SubmitCommand = new AsyncRelayCommand(ExecuteSubmitAsync);
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke(false));
        }

        private async Task ExecuteSubmitAsync()
        {
            if (AmountToPay <= 0)
            {
                ErrorMessage = "Số tiền nhập vào phải lớn hơn 0.";
                return;
            }

            if (AmountToPay > RemainingBalance)
            {
                ErrorMessage = $"Số tiền vượt quá dư nợ còn lại ({RemainingBalance:N0} VNĐ).";
                return;
            }

            ErrorMessage = string.Empty;
            IsBusy = true;

            string refCode = string.IsNullOrWhiteSpace(TransactionReference)
                ? $"TXN-{DateTime.Now:yyyyMMddHHmmss}"
                : TransactionReference;

            var result = await _paymentService.ProcessPaymentAsync(
                _invoice.Id,
                AmountToPay,
                PaymentMethod,
                refCode,
                _currentUserId,
                Note
            );

            IsBusy = false;

            if (result.IsSuccess)
            {
                RequestClose?.Invoke(true);
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }
    }
}
