using System;
using System.Windows;
using DormCare.Business.Services;
using DormCare.DataAccess.Data;
using DormCare.DataAccess.Repositories;
using DormCare.Domain.Entities;
using DormCare.WPF.Services;
using DormCare.WPF.ViewModels;
using DormCare.WPF.Views;
using DormCare.WPF.Views.Student;
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

            var services = new ServiceCollection();
            ConfigureServices(services);

            ServiceProvider = services.BuildServiceProvider();

            _ = TestDbConnectionAsync();

            ShowLoginWindow();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<DormCareDbContext>(options =>
            {
                options.UseSqlServer("Server=localhost;Database=DormCareDB;User Id=hau;Password=123456;TrustServerCertificate=True;Encrypt=False;");
            });

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
            services.AddTransient<StudentViewModel>();
            services.AddTransient<ApplicationViewModel>();
            services.AddTransient<InvoiceViewModel>();
            services.AddTransient<MaintenanceViewModel>();
            services.AddTransient<StudentDashboardViewModel>();
        }

        private async System.Threading.Tasks.Task TestDbConnectionAsync()
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
                loginWindow.Hide();
                OpenMainWindow(user, loginWindow);
            };

            loginWindow.Show();
        }

        private void OpenMainWindow(User user, Window loginWindow)
        {
            if (user.Role == "Student")
            {
                var studentVm = new StudentDashboardViewModel(
                    ServiceProvider.GetRequiredService<StudentService>(),
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
                studentWindow.Closed += (s, e) => loginWindow.Close();
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
                mainWindow.Closed += (s, e) => loginWindow.Close();
                mainWindow.Show();
            }
        }
    }
}
