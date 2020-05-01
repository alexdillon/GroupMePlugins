using GroupMeClientApi.Models;
using GroupMeClientPlugin.GroupChat;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Gallery
{
    public class ModernGalleryPlugin : GroupMeClientPlugin.PluginBase, IGroupChatPlugin
    {
        public string PluginName => this.PluginDisplayName;

        public override string PluginDisplayName => "Image Gallery";

        public override string PluginVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public override Version ApiVersion => new Version(2, 0, 0);

        public Task Activated(IMessageContainer groupOrChat, IQueryable<Message> cacheForGroupOrChat, IQueryable<Message> globalCache, IPluginUIIntegration integration)
        {
            MainWindow mainWindow = new MainWindow(groupOrChat, cacheForGroupOrChat, integration, false);

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                mainWindow.Show();
            });

            return Task.CompletedTask;
        }
    }
}
