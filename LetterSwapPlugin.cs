using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using GroupMeClientPlugin.MessageCompose;
using Newtonsoft.Json.Linq;

namespace LetterSwapPlugin
{
    public class LetterSwapPlugin : GroupMeClientPlugin.IPluginBase, IMessageComposePlugin
    {
        public string EffectPluginName => "Letter Swap";

        public async Task<MessageSuggestions> ProvideOptions(string typedMessage)
        {
            var result = new MessageSuggestions();

            //result.TextOptions.Add(this.DoLetterSwap(typedMessage, result));
            await DoLetterSwap(typedMessage, result);

            return await Task.FromResult<MessageSuggestions>(result);
        }

        private async Task DoLetterSwap(string text, MessageSuggestions result)
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

            var json = proc.StandardOutput.ReadLine();
            Debug.WriteLine(json);
            var effects = JObject.Parse(json);
            foreach (System.Collections.Generic.KeyValuePair<string, JToken> effect in effects)
            {
                Debug.WriteLine(effect);
                result.TextOptions.Add((string)effect.Value);
            }
        }
    }
}