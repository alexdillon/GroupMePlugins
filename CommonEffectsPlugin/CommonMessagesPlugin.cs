using System.Reflection;
using System.Threading.Tasks;
using GroupMeClientPlugin.MessageCompose;

namespace CommonMessagesPlugin
{
    public class CommonMessagesPlugin : GroupMeClientPlugin.PluginBase, IMessageComposePlugin
    {
        public string EffectPluginName => "Common Messages";

        public override string PluginDisplayName => "Common Messages Plugin";

        public override string PluginVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public Task<MessageSuggestions> ProvideOptions(string typedMessage)
        {
            var result = new MessageSuggestions();
            result.TextOptions.Add(@"¯\_(ツ)_/¯");

            return Task.FromResult<MessageSuggestions>(result);
        }
    }
}
