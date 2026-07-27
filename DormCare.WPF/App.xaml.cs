using System;
using System.Threading.Tasks;
using System.Windows;
using DormCare.Business.Services;
using DormCare.DataAccess.Data;
using DormCare.DataAccess.Repositories;
using DormCare.Domain.Entities;
using DormCare.WPF.Services;
using DormCare.WPF.ViewModels;
using DormCare.WPF.Views;
using DormCare.WPF.Views.Student;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DormCare.WPF
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Setup global unhandled exception handling to prevent app crash
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var services = new ServiceCollection();
            ConfigureServices(services);

            ServiceProvider = services.BuildServiceProvider();

            _ = TestDbConnectionAsync();

            ShowLoginWindow();
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                $"Hệ thống ghi nhận lỗi:\n{e.Exception.Message}\n\nChi tiết:\n{e.Exception.InnerException?.Message}",
                "Lỗi Hệ Thống",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            
            e.Handled = true; // Prevent app shutdown
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi nghiêm trọng hệ thống:\n{ex.Message}",
                    "Lỗi Hệ Thống",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Unobserved Task Exception: {e.Exception.Message}");
            e.SetObserved();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            string connectionString = GetWorkingConnectionString();

            services.AddDbContext<DormCareDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
                });
            }, ServiceLifetime.Transient, ServiceLifetime.Transient);

            services.AddScoped<UserRepository>();
            services.AddScoped<StudentRepository>();
            services.AddScoped<BuildingRepository>();
            services.AddScoped<RoomRepository>();
            services.AddScoped<BedRepository>();
            services.AddScoped<InvoiceRepository>();
            services.AddScoped<MaintenanceRepository>();

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
            services.AddScoped<OccupancyService>();

            services.AddSingleton<DialogService>();
            services.AddSingleton<NavigationService>();

            services.AddTransient<LoginViewModel>();
            services.AddTransient<BuildingViewModel>();
            services.AddTransient<RoomViewModel>();
            services.AddTransient<BedViewModel>();
            services.AddTransient<AvailableRoomViewModel>();
            services.AddTransient<OccupancyStatisticsViewModel>();
            services.AddTransient<StudentViewModel>(provider => new StudentViewModel(
                provider.GetRequiredService<StudentService>(),
                provider.GetRequiredService<DormCareDbContext>(),
                provider.GetRequiredService<DialogService>(),
                new User() // Dummy user for DI registration
            ));
            services.AddTransient<ApplicationViewModel>();
            services.AddTransient<InvoiceViewModel>();
            services.AddTransient<MaintenanceViewModel>();
            services.AddTransient<StudentDashboardViewModel>();
        }

        private string GetWorkingConnectionString()
        {
            string[] connectionStrings = new string[]
            {
                "Server=DANG;Database=DormCareDB;User Id=sa;Password=123456;TrustServerCertificate=True;Encrypt=False;Connect Timeout=3;",
                "Server=.;Database=DormCareDB;User Id=sa;Password=123456;TrustServerCertificate=True;Encrypt=False;Connect Timeout=3;",
                "Server=localhost;Database=DormCareDB;User Id=sa;Password=123456;TrustServerCertificate=True;Encrypt=False;Connect Timeout=3;",
                "Server=.\\SQLEXPRESS;Database=DormCareDB;User Id=sa;Password=123456;TrustServerCertificate=True;Encrypt=False;Connect Timeout=3;",
                "Server=.\\SQLEXPRESS;Database=DormCareDB;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=3;",
                "Server=.;Database=DormCareDB;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=3;",
                "Server=(localdb)\\mssqllocaldb;Database=DormCareDB;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=3;"
            };

            foreach (var connStr in connectionStrings)
            {
                try
                {
                    using var conn = new SqlConnection(connStr);
                    conn.Open();
                    // Remove Connect Timeout=3 constraint for production use after connection verified
                    return connStr.Replace(";Connect Timeout=3;", ";");
                }
                catch
                {
                    // Continue trying next connection string format
                }
            }

            // Fallback default
            return "Server=DANG;Database=DormCareDB;User Id=sa;Password=123456;TrustServerCertificate=True;Encrypt=False;";
        }

        private async Task TestDbConnectionAsync()
        {
            try
            {
                using var scope = ServiceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<DormCareDbContext>();
                await DbInitializer.InitializeAsync(dbContext);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DB Init Warning: {ex.Message}");
            }
        }

        private void ShowLoginWindow()
        {
            var loginVm = ServiceProvider.GetRequiredService<LoginViewModel>();
            var loginWindow = new LoginWindow { DataContext = loginVm };

            loginVm.LoginSuccess += (user) =>
            {
                try
                {
                    OpenMainWindow(user, loginWindow);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi mở giao diện chính: {ex.Message}\n{ex.InnerException?.Message}", "Lỗi Hệ Thống", MessageBoxButton.OK, MessageBoxImage.Error);
                    loginWindow.Show();
                }
            };

            loginWindow.Show();
        }

        private void OpenMainWindow(User user, Window loginWindow)
        {
            if (user.Role == "Student")
            {
                var studentVm = new StudentDashboardViewModel(
                    ServiceProvider.GetRequiredService<StudentService>(),
                    ServiceProvider.GetRequiredService<RoomService>(),
                    ServiceProvider.GetRequiredService<ApplicationService>(),
                    ServiceProvider.GetRequiredService<InvoiceService>(),
                    ServiceProvider.GetRequiredService<PaymentService>(),
                    ServiceProvider.GetRequiredService<DialogService>(),
                    user
                );
                var studentWindow = new Window
                {
                    Title = "DormCare — Cổng Thông Tin Sinh Viên",
                    Content = new StudentDashboard { DataContext = studentVm },
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Width = 1200,
                    Height = 750
                };
                studentWindow.Closed += (s, e) =>
                {
                    Shutdown();
                };

                loginWindow.Hide();
                studentWindow.Show();
            }
            else
            {
                var mainVm = new MainViewModel(
                    user,
                    ServiceProvider.GetRequiredService<AuthService>(),
                    ServiceProvider.GetRequiredService<DialogService>(),
                    ServiceProvider.GetRequiredService<BuildingService>(),
                    ServiceProvider.GetRequiredService<RoomService>(),
                    ServiceProvider.GetRequiredService<BedService>(),
                    ServiceProvider.GetRequiredService<StudentService>(),
                    ServiceProvider.GetRequiredService<ApplicationService>(),
                    ServiceProvider.GetRequiredService<InvoiceService>(),
                    ServiceProvider.GetRequiredService<PaymentService>(),
                    ServiceProvider.GetRequiredService<MaintenanceService>(),
                    ServiceProvider.GetRequiredService<OccupancyService>()
                );

                var mainWindow = new MainWindow { DataContext = mainVm };
                mainVm.RequestLogout += () =>
                {
                    mainWindow.Close();
                    ShowLoginWindow();
                };
                mainWindow.Closed += (s, e) =>
                {
                    Shutdown();
                };

                loginWindow.Hide();
                mainWindow.Show();
            }
        }
    }
}
