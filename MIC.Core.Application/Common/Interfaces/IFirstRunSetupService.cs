using System.Threading.Tasks;

namespace MIC.Core.Application.Common.Interfaces;

public interface IFirstRunSetupService
{
    Task<bool> IsFirstRunAsync();
    Task CompleteFirstRunSetupAsync(string email, string password);
    Task<bool> IsSetupCompleteAsync();
    string GetRuntimeJwtSecretKey();
}
