using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using DormCare.DataAccess.Data;
using DormCare.DataAccess.Repositories;
using DormCare.Business.Services;
using DormCare.Domain.Entities;
using DormCare.WPF.Services;
using DormCare.WPF.ViewModels;
using DormCare.WPF.Views;

namespace DormCare.WPF
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            ConfigureServices(services);

            ServiceProvider = services.BuildServiceProvider();

            // Connect to SQL Server Database (DormCareDB)
            try
            {
                using (var scope = ServiceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<DormCareDbContext>();
                    DbInitializer.Initialize(dbContext);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cảnh báo kết nối SQL Server: {ex.Message}\nỨng dụng sẽ tiếp tục với cấu hình hiện tại.", "DormCare SQL Server", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            ShowLoginWindow();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // 1. DataAccess & DbContext (Scoped Lifetime)
            services.AddDbContext<DormCareDbContext>(ServiceLifetime.Scoped);
            services.AddScoped<UserRepository>();
            services.AddScoped<BuildingRepository>();
            services.AddScoped<RoomRepository>();
            services.AddScoped<BedRepository>();
            services.AddScoped<StudentRepository>();
            services.AddScoped<InvoiceRepository>();
            services.AddScoped<MaintenanceRepository>();

            // 2. Business Services (Scoped Lifetimes)
            services.AddScoped<AuthService>();
            services.AddScoped<BuildingService>();
            services.AddScoped<RoomService>();
            services.AddScoped<BedService>();
            services.AddScoped<StudentService>();
            services.AddScoped<ApplicationService>();
            services.AddScoped<InvoiceService>();
            services.AddScoped<PaymentService>();
            services.AddScoped<MaintenanceService>();
            services.AddScoped<NotificationService>();

            // 3. Presentation Infrastructure (Singleton Lifetime)
            services.AddSingleton<NavigationService>();
            services.AddSingleton<DialogService>();

            // 4. ViewModels & Views (Transient Lifetime)
            services.AddTransient<LoginViewModel>();
            services.AddTransient<LoginWindow>();
        }

        public void ShowLoginWindow()
        {
            var loginViewModel = ServiceProvider.GetRequiredService<LoginViewModel>();
            var loginWindow = new LoginWindow
            {
                DataContext = loginViewModel
            };

            loginViewModel.LoginSuccess += (user) =>
            {
                ShowMainWindow(user);
                loginWindow.Close();
            };

            loginWindow.Show();
        }

        public void ShowMainWindow(User currentUser)
        {
            var navService = ServiceProvider.GetRequiredService<NavigationService>();
            var studentService = ServiceProvider.GetRequiredService<StudentService>();
            var roomService = ServiceProvider.GetRequiredService<RoomService>();
            var buildingService = ServiceProvider.GetRequiredService<BuildingService>();
            var bedService = ServiceProvider.GetRequiredService<BedService>();
            var appService = ServiceProvider.GetRequiredService<ApplicationService>();
            var invoiceService = ServiceProvider.GetRequiredService<InvoiceService>();
            var maintenanceService = ServiceProvider.GetRequiredService<MaintenanceService>();
            var dialogService = ServiceProvider.GetRequiredService<DialogService>();

            var mainViewModel = new MainViewModel(
                currentUser,
                navService,
                studentService,
                roomService,
                buildingService,
                bedService,
                appService,
                invoiceService,
                maintenanceService,
                dialogService);

            var mainWindow = new Views.MainWindow
            {
                DataContext = mainViewModel
            };

            mainViewModel.RequestLogout += () =>
            {
                mainWindow.Close();
                ShowLoginWindow();
            };

            mainWindow.Show();
        }
    }
}
