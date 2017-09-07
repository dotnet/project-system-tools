using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.RemoteControl
{
    internal interface IProjectSystemToolsSetttingsService
    {
        bool TryGetSetting<T>(string name, out T value);
        Task UpdateContinuouslyAsync(string serverPath, CancellationToken token);
    }
}
