using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GroupMeClientApi.Models;
using GroupMeClientPlugin;
using GroupMeClientPlugin.GroupChat;
using GroupPluginDemoWPF_MVVM;

namespace GroupMeStatsPlugin
{
    public class GroupMeStatsPlugin : GroupMeClientPlugin.PluginBase, GroupMeClientPlugin.GroupChat.IGroupChatPlugin
    {
        public string PluginName => this.PluginDisplayName;

        public override string PluginDisplayName => "GroupMe Stats Plugin";

        public override string PluginVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public override Version ApiVersion => new Version(2, 0, 0);

        public Task Activated(IMessageContainer groupOrChat, CacheSession cacheSession, IPluginUIIntegration integration, Action<CacheSession> cleanup)
        {
            var dataContext = new MainWindowViewModel(groupOrChat, cacheSession, integration);
            var window = new MainWindow
            {
                DataContext = dataContext,
            };

            window.Closing += (s, e) =>
            {
                cleanup(cacheSession);
            };

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                window.Show();
            });

            return Task.CompletedTask;
        }
    }
}
