using GroupMeClientApi.Models;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Gallery
{
    public class GalleryPlugin : GroupMeClientPlugin.PluginBase, GroupMeClientPlugin.GroupChat.IGroupChatCachePlugin
    {
        public string PluginName => this.PluginDisplayName;

        public override string PluginDisplayName => "Image Gallery";

        public override string PluginVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public Task Activated(IMessageContainer groupOrChat, IQueryable<Message> cachedMessages)
        {
            MainWindow mainWindow = new MainWindow(groupOrChat, cachedMessages);

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                mainWindow.Show();
            });

            return Task.CompletedTask;
        }
    }
}
