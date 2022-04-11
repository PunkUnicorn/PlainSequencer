using System.Threading.Tasks;

namespace PlainSequencer
{
    public interface IApplication
    {
        Task<bool> RunAsync(object startModel);
    }
}
