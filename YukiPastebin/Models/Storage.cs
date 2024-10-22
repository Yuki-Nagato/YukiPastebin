using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace YukiPastebin.Models {
    public record SimpleFile(string Name, long Size) {
        public long? Id { get; set; }
        [JsonIgnore]
        public SyncMessage? Message { get; set; }
    }
    public record SyncMessage(DateTimeOffset Time, string? Ip, [property: JsonIgnore] string SenderUuid, string Text, SimpleFile[] Files) {
        public long? Id { get; set; }
    }
    public class Storage {
        public BiMap<string, string> UuidAndConnectionIds { get; } = new();  // online clients
        public ConcurrentDictionary<long, SyncMessage> Messages { get; } = new();
        public ConcurrentDictionary<long, SimpleFile> Files { get; } = new();
        public ConcurrentDictionary<long, TaskCompletionSource<(TaskCompletionStream stream, string? contentType, long? contentLength)>> SemaphoreDict { get; } = new();

        public string ConnectionStart(string? uuid, HubCallerContext context) {
            if (string.IsNullOrWhiteSpace(uuid)) {
                uuid = Guid.NewGuid().ToString();
            }
            lock (UuidAndConnectionIds) {
                UuidAndConnectionIds.AddOrReplace(uuid, context.ConnectionId);
            }
            return uuid;
        }

        public void ConnectionClose(HubCallerContext context) {
            lock (UuidAndConnectionIds) {
                UuidAndConnectionIds.Remove(item2: context.ConnectionId);
            }
        }

        public long CreateMessage(string text, SimpleFile[] files, HubCallerContext context) {
            SyncMessage message;
            lock (UuidAndConnectionIds) {
                message = new(DateTimeOffset.Now, context.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress?.ToString(), UuidAndConnectionIds.Backward[context.ConnectionId], text, files);
            }
            long id = GenerateMessageId(message);
            foreach (SimpleFile file in message.Files) {
                file.Message = message;
                GenerateFileId(file);
            }
            return id;
        }

        public void DestroyMessage(long id) {
            if (Messages.TryRemove(id, out SyncMessage? message)) {
                foreach (SimpleFile file in message.Files) {
                    if (file.Id.HasValue) {
                        Files.TryRemove(file.Id.Value, out _);
                    }
                }
            }
        }

        public List<long> DownloadableMessageIds {
            get {
                List<long> result = new();
                foreach (SyncMessage message in Messages.Values) {
                    lock (UuidAndConnectionIds) {
                        if (UuidAndConnectionIds.Forward.ContainsKey(message.SenderUuid)) {
                            if (message.Id.HasValue) {
                                result.Add(message.Id.Value);
                            }
                        }
                    }
                }
                return result;
            }
        }

        public string FileIdToConnectionId(long fileId) {
            lock (UuidAndConnectionIds) {
                return UuidAndConnectionIds.Forward[Files[fileId].Message?.SenderUuid ?? throw new Exception()];
            }
        }
        public long GenerateFileId(SimpleFile file) {
            long id = file.Message?.Id ?? throw new Exception("The file does not belong to a message when generating its ID.");
            while (!Files.TryAdd(id, file)) {
                id++;
            }
            file.Id = id;
            return id;
        }

        public long GenerateMessageId(SyncMessage message) {
            long id = message.Time.ToUnixTimeMilliseconds();
            while (!Messages.TryAdd(id, message)) {
                id++;
            }
            message.Id = id;
            return id;
        }

        public long GenerateSemaphoreId(TaskCompletionSource<(TaskCompletionStream stream, string? contentType, long? contentLength)> source) {
            long id = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            while (!SemaphoreDict.TryAdd(id, source)) {
                id++;
            }
            return id;
        }
    }
}
