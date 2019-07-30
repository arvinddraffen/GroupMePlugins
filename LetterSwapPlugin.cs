using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using GroupMeClientPlugin.MessageCompose;

namespace LetterSwapPlugin
{
    public class LetterSwapPlugin : GroupMeClientPlugin.IPluginBase, IMessageComposePlugin
    {
        public string EffectPluginName => "Letter Swap";

        public async Task<MessageSuggestions> ProvideOptions(string typedMessage)
        {
            var result = new MessageSuggestions();
            result.TextOptions.Add(this.DoLetterSwap(typedMessage));

            return await Task.FromResult<MessageSuggestions>(result);
        }

        private string DoLetterSwap(string text)
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "PhraseManipulation-plugin.exe"),
                    Arguments = text,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                }
            };

            proc.Start();
            return proc.StandardOutput.ReadLine();
        }

        private static Task ExitedAsync(Process p)
        {
            var tcs = new TaskCompletionSource<object>();
            p.Exited += (s,e) => tcs.TrySetResult(null);
            if (p.HasExited) tcs.TrySetResult(null);
            return tcs.Task;
        }
    }
}
