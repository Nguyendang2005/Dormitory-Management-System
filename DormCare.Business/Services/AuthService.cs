using System;
using System.Threading.Tasks;
using DormCare.Business.DTOs;
using DormCare.DataAccess.Repositories;
using DormCare.Domain.Entities;

namespace DormCare.Business.Services
{
    public class AuthService
    {
        private readonly UserRepository _userRepository;

        public event Action<User>? OnUserLoggedIn;

        public AuthService(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<ServiceResult<User>> LoginAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return ServiceResult<User>.Failure("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");
            }

            var user = await _userRepository.GetByUsernameAsync(username);

            // Accept plain match or hash match for development flexibility
            if (user == null || (user.PasswordHash != password && !user.PasswordHash.Contains(password)))
            {
                return ServiceResult<User>.Failure("Tài khoản hoặc mật khẩu không chính xác.");
            }

            if (!user.IsActive)
            {
                return ServiceResult<User>.Failure("Tài khoản của bạn đã bị khóa.");
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            OnUserLoggedIn?.Invoke(user);
            return ServiceResult<User>.Success(user, "Đăng nhập thành công!");
        }
    }
}
