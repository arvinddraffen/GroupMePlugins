using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using GroupMeClientPlugin.MessageCompose;
using Newtonsoft.Json.Linq;

namespace LetterSwapPlugin
{
    public class LetterSwapPlugin : GroupMeClientPlugin.PluginBase, IMessageComposePlugin
    {
        public string EffectPluginName => "Letter Swap";

        public override string PluginDisplayName => "Letter Swap Plugin";

        public override string PluginVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public async Task<MessageSuggestions> ProvideOptions(string typedMessage)
        {
            var result = new MessageSuggestions();

            await Task.Run(() => DoLetterSwap(typedMessage, result));

            return result;
        }

        private void DoLetterSwap(string text, MessageSuggestions result)
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
            if (!string.IsNullOrEmpty(json))
            {
                var effects = JObject.Parse(json);
                foreach (System.Collections.Generic.KeyValuePair<string, JToken> effect in effects)
                {
                    Debug.WriteLine(effect);
                    result.TextOptions.Add((string)effect.Value);
                }
            }
        }
    }
}