using GroupMeClientApi.Models;
using GroupMeClientPlugin;
using GroupMeClientPlugin.GroupChat;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Gallery
{
    public class GalleryPlugin : GroupMeClientPlugin.PluginBase, IGroupChatPlugin
    {
        public string PluginName => this.PluginDisplayName;

        public override string PluginDisplayName => "Image Gallery (Classic)";

        public override string PluginVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public override Version ApiVersion => new Version(2, 0, 0);

        public Task Activated(IMessageContainer groupOrChat, CacheSession cacheSession, IPluginUIIntegration integration, Action<CacheSession> cleanup)
        {
            MainWindow mainWindow = new MainWindow(groupOrChat, cacheSession, integration, true);

            mainWindow.Closed += (s, e) =>
            {
                cleanup(cacheSession);
            };

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                mainWindow.Show();
            });

            return Task.CompletedTask;
        }
    }
}
