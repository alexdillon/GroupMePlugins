using GroupMeClientApi.Models;
using GroupMeClientPlugin.GroupChat;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Gallery
{
    public class ModernGalleryPlugin : GroupMeClientPlugin.PluginBase, GroupMeClientPlugin.GroupChat.IGroupChatCachePlugin
    {
        public string PluginName => this.PluginDisplayName;

        public override string PluginDisplayName => "Image Gallery";

        public override string PluginVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public Task Activated(IMessageContainer groupOrChat, IQueryable<Message> cachedMessages, ICachePluginUIIntegration integration)
        {
            MainWindow mainWindow = new MainWindow(groupOrChat, cachedMessages, integration, false);

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                mainWindow.Show();
            });

            return Task.CompletedTask;
        }
    }
}
