using System.Threading;
using System.Threading.Tasks;

namespace CloutCast
{
    public interface IValidated
    {
        void ValidateAndThrow(params string[] ruleSetNames);
        Task ValidateAndThrowAsync(CancellationToken cancellationToken, params string[] ruleSetNames);
    }
}