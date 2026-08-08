using SmartShip.Identity.Domain.Entities;
namespace SmartShip.Identity.Application.Interfaces.Repositories;

public interface IOtpVerificationRepository
{
    Task<OtpVerification?> GetByEmailAndPurposeAsync(string email, string purpose);
    Task AddAsync(OtpVerification otpVerification);
    void Update(OtpVerification otpVerification);
    void Delete(OtpVerification otpVerification);
    Task<IEnumerable<OtpVerification>> GetByUserIdAsync(int userId);
}