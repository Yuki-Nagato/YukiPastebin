using Microsoft.AspNetCore.SignalR;
using YukiPastebin.Models;

namespace YukiPastebin.Hubs {

    public class MessageHub : Hub {

        private ILogger<MessageHub> logger;
        private Storage storage;

        public MessageHub(ILogger<MessageHub> logger, Storage storage) {
            this.logger = logger;
            this.storage = storage;
        }

        public async Task Sync(IClientProxy client) {
            await client.SendAsync("Sync", storage.Messages.Values.OrderByDescending(message => message.Id), storage.DownloadableMessageIds);
        }

        public async Task SendMessage(string text, SimpleFile[] files) {
            storage.CreateMessage(text, files, Context);
            await Clients.Caller.SendAsync("CreateFileIds", files);
            await Sync(Clients.All);
        }

        public async Task DeleteMessage(long id) {
            storage.DestroyMessage(id);
            await Sync(Clients.All);
        }

        public async Task RequestClientToUpload(long fileId, long streamId) {
            await Clients.Client(storage.FileIdToConnectionId(fileId)).SendAsync("UploadRequest", fileId, streamId);
        }

        public override async Task OnConnectedAsync() {
            string uuid = storage.ConnectionStart(Context.GetHttpContext()?.Request.Query["uuid"], Context);
            await Clients.Caller.SendAsync("SetUuid", uuid);
            await Sync(Clients.Caller);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception) {
            storage.ConnectionClose(Context);
            await Sync(Clients.Others);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
