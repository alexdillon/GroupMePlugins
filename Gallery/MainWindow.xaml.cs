using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using GroupMeClientApi.Models;
using GroupMeClientApi.Models.Attachments;
using GroupMeClientPlugin.GroupChat;

namespace Gallery
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(IMessageContainer groupChat, IQueryable<Message> cachedMessages, ICachePluginUIIntegration uiIntegration, bool classicStyle)
        {
            InitializeComponent();

            this.GroupChat = groupChat;
            this.CachedMessages = cachedMessages;
            this.UIIntegration = uiIntegration;
            this.ClassicStyle = classicStyle;

            this.MessagesWithAttachments =
                this.CachedMessages
                    .AsEnumerable()
                    .Where(m => m.Attachments.Count > 0)
                    .OrderByDescending(m => m.CreatedAtTime);

            this.HttpListener = new HttpListener();
            this.HttpListener.Prefixes.Add($"http://+:80/Temporary_Listen_Addresses/{this.ServerId}/");
            this.HttpListener.Start();

            this.CancellationTokenSource = new CancellationTokenSource();
            Task.Run(this.RunServer, this.CancellationTokenSource.Token);

            Debug.WriteLine($"http://+:80/Temporary_Listen_Addresses/{this.ServerId}/");

            this.ScriptingHelper = new ObjectForScriptingHelper(this);
            this.webBrowser.ObjectForScripting = this.ScriptingHelper;

            this.webBrowser.Navigate($"http://127.0.0.1:80/Temporary_Listen_Addresses/{this.ServerId}/gallery.html");

            this.Title = $"Image Gallery for {this.GroupChat.Name}";
        }

        private IMessageContainer GroupChat { get; }

        private IQueryable<Message> CachedMessages { get; }

        private IEnumerable<Message> MessagesWithAttachments { get; }

        private ICachePluginUIIntegration UIIntegration { get; }

        private ObjectForScriptingHelper ScriptingHelper { get; }

        private int ImagesPerPage { get; } = 25;

        private string ServerId { get; } = Guid.NewGuid().ToString();

        private HttpListener HttpListener { get; }

        private CancellationTokenSource CancellationTokenSource { get; }

        private bool ClassicStyle { get; }

        public void OpenContextView(string id)
        {
            var message = this.MessagesWithAttachments.FirstOrDefault(m => m.Id == id);

            if (message == null)
            {
                return;
            }

            this.UIIntegration.GotoContextView(message, this.GroupChat);

            Application.Current.Dispatcher.Invoke(() =>
            {
                Application.Current.MainWindow.Activate();
            });
        }

        private async Task RunServer()
        {
            while (!this.CancellationTokenSource.IsCancellationRequested)
            {
                var context = await this.HttpListener.GetContextAsync();
                var prefix = $"/Temporary_Listen_Addresses/{this.ServerId}/";
                var file = context.Request.Url.AbsolutePath.Replace(prefix, "");

                Debug.WriteLine("File is " + file);

                const string JavascriptMimeType = "application/javascript";

                switch (file)
                {
                    case "infinite-scroll.pkgd.min.js":
                        this.SendFile(Properties.Resources.infinite_scroll_pkgd_min, context.Response, JavascriptMimeType);
                        break;

                    case "jquery-3.4.1.min.js":
                        this.SendFile(Properties.Resources.jquery_3_4_1_min, context.Response, JavascriptMimeType);
                        break;

                    case "masonry.pkgd.min.js":
                        this.SendFile(Properties.Resources.masonry_pkgd_min, context.Response, JavascriptMimeType);
                        break;

                    case "imagesloaded.pkgd.min.js":
                        this.SendFile(Properties.Resources.imagesloaded_pkgd_min, context.Response, JavascriptMimeType);
                        break;

                    case "gallery.html":
                        this.SendPage(1, context.Response);
                        break;

                    case "viewImage.html":
                        var msgId = context.Request.QueryString["id"];
                        this.SendEmbeddedPage(msgId, context.Response);
                        break;

                    default:
                        var pageNumber = file.Replace(".html", "");
                        if (int.TryParse(pageNumber, out var pageNumberInt))
                        {
                            this.SendPage(pageNumberInt, context.Response);
                        }
                        else
                        {
                            Debug.WriteLine($"{pageNumber} is not a pagenumber");
                            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            context.Response.Close();
                        }

                        break;
                }
            }
        }

        private void SendFile(string data, HttpListenerResponse response, string mimeType)
        {
            byte[] file = Encoding.ASCII.GetBytes(data);

            response.StatusCode = (int)HttpStatusCode.OK;
            response.StatusDescription = "OK";
            response.ContentType = mimeType;
            response.OutputStream.Write(file, 0, file.Length);
            response.OutputStream.Close();
            response.Close();
        }

        private void SendPage(int friendlyPageNumber, HttpListenerResponse response)
        {
            if (this.MessagesWithAttachments == null)
            {
                return;
            }

            var pageNumber = friendlyPageNumber - 1;

            var file = this.ClassicStyle ? Properties.Resources.gallery : Properties.Resources.moderngallery;
            var range = this.MessagesWithAttachments.Skip(pageNumber * this.ImagesPerPage).Take(this.ImagesPerPage);

            var images = new StringWriter();

            foreach (var msg in range)
            {
                var imageUrl = this.GetAttachmentContentUrl(msg.Attachments);
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    var size = this.ClassicStyle ? "preview" : "large";

                    var entry = $@"<div class=""floatImage"">
                                    <img src=""{imageUrl}.{size}"" onClick=""openModal('{msg.Id}');""/>
                                </div>";

                    images.WriteLine(entry);

                }
            }

            file = file.Replace("{IMAGES}", images.ToString());

            var sendBytes = Encoding.ASCII.GetBytes(file);
            response.StatusCode = (int)HttpStatusCode.OK;
            response.StatusDescription = "OK";
            response.ContentType = "text/html";
            response.OutputStream.Write(sendBytes, 0, sendBytes.Length);
            response.OutputStream.Close();
            response.Close();
        }

        private void SendEmbeddedPage(string messageId, HttpListenerResponse response)
        {
            var file = Properties.Resources.popup;
            var message = this.MessagesWithAttachments.FirstOrDefault(m => m.Id == messageId);

            if (message == null)
            {
                return;
            }

            var imageUrl = this.GetAttachmentContentUrl(message.Attachments);
            var likersList = new StringWriter();
            try
            {
                foreach (var likerId in message.FavoritedBy)
                {
                    Member member = null;

                    // member is either a Group Member, Other Chat User, or This User
                    if (this.GroupChat is Group g)
                    {
                        member = g.Members.FirstOrDefault(m => m.UserId == likerId);
                    }
                    else if (this.GroupChat is Chat c)
                    {
                        member = 
                            (c.OtherUser.Id == likerId ? c.OtherUser : null) ??
                            (c.WhoAmI().Id == likerId ? c.WhoAmI() : null);
                    }
                        
                    if (member != null)
                    {
                        var entry = $@"<img src=""{member.ImageOrAvatarUrl}.avatar"" class=""likerImage""/>";
                        likersList.WriteLine(entry);
                    }
                }
            }
            catch (Exception)
            {
            }

            file = file.Replace("{SENDER}", message.Name);
            file = file.Replace("{DATE}", message.CreatedAtTime.ToString());
            file = file.Replace("{AVATARURL}", $"{message.AvatarUrl}.avatar");
            file = file.Replace("{MESSAGE}", message.Text);
            file = file.Replace("{ID}", message.Id);
            //file = file.Replace("{LIKERS}", likersList.ToString());

            file = file.Replace("{IMAGE}", imageUrl);

            var sendBytes = Encoding.ASCII.GetBytes(file);
            response.StatusCode = (int)HttpStatusCode.OK;
            response.StatusDescription = "OK";
            response.ContentType = "text/html";
            response.OutputStream.Write(sendBytes, 0, sendBytes.Length);
            response.OutputStream.Close();
            response.Close();
        }

        private string GetAttachmentContentUrl(IEnumerable<Attachment> attachments)
        {
            var imageUrl = string.Empty;
            foreach (var attachment in attachments)
            {
                if (attachment is ImageAttachment imageAttachment)
                {
                    imageUrl = $"{imageAttachment.Url}";
                    break;
                }
                else if (attachment is LinkedImageAttachment linkedImageAttachment)
                {
                    imageUrl = $"{linkedImageAttachment.Url}";
                    break;
                }
                else if (attachment is VideoAttachment videoAttachment)
                {
                    imageUrl = videoAttachment.PreviewUrl;
                    break;
                }
            }

            return imageUrl;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.CancellationTokenSource.Cancel();
            try
            {
                this.HttpListener.Stop();
            }
            catch (Exception)
            {
            }
        }
    }
}
