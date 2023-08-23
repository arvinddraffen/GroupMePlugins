using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GroupMeClientPlugin.MessageCompose;

namespace TimeInChinaPlugin
{
    public class TimeInChinaPlugin : GroupMeClientPlugin.PluginBase, IMessageComposePlugin
    {
        public string EffectPluginName => this.PluginDisplayName;

        public override string PluginDisplayName => "Time in China";

        public override string PluginVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public override Version ApiVersion => new Version(2, 0, 0);

        public Task<MessageSuggestions> ProvideOptions(string typedMessage)
        {
            var results = new MessageSuggestions();
            results.TextOptions.Add(this.DoTimeDateInChina());
            results.TextOptions.Add(this.DoDateTimeInChina());
            results.TextOptions.Add(this.DoTimeInChina());
            results.TextOptions.Add(this.DoTimeInHunterTime());
            return Task.FromResult(results);
        }

        private string DoTimeDateInChina()
        {
            return "The time and date in China is: " + TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("China Standard Time")).ToString("hh:mm:ss tt, MM/dd/yyyy");
        }

        private string DoDateTimeInChina()
        {
            return "The date and time in China is: " + TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("China Standard Time")).ToString("MM/dd/yyyy, hh:mm:ss tt");
        }

        private string DoTimeInChina()
        {
            return "The time in China is: " + TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("China Standard Time")).ToString("hh:mm:ss tt");
        }

        private string DoTimeInHunterTime()
        {
            return "The time in \"Hunter time\" is: " + TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("China Standard Time")).ToString("hh:mm:ss tt");
        }
    }
}
