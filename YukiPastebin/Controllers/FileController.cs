using Microsoft.AspNetCore.Mvc;
using YukiPastebin.Hubs;
using YukiPastebin.Models;

namespace YukiPastebin.Controllers {


    [Route("[controller]/[action]")]
    public class FileController : Controller {
        private readonly ILogger<FileController> logger;
        private readonly MessageHub messageHub;
        private readonly Storage storage;

        public FileController(ILogger<FileController> logger, MessageHub messageHub, Storage storage) {
            this.logger = logger;
            this.messageHub = messageHub;
            this.storage = storage;
        }

        [Route("{fileId}/{fileName}")]
        public async Task<IActionResult> DownloadAsync(long fileId, string fileName, CancellationToken cancellationToken) {
            TaskCompletionSource<(TaskCompletionStream stream, string? contentType, long? contentLength)> downloadSource = new();
            long id = storage.GenerateSemaphoreId(downloadSource);
            logger.LogInformation("DownloadAsync: fileId: {}, fileName: {}, StreamId: {}.", fileId, fileName, id);
            if (fileId != 123456) {  // for testing
                await messageHub.RequestClientToUpload(fileId, id);
            }
            (TaskCompletionStream stream, string? contentType, long? contentLength) = await downloadSource.Task.WaitAsync(cancellationToken);
            storage.SemaphoreDict.TryRemove(id, out _);
            Response.ContentLength = contentLength;
            return File(stream, string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType, fileName, false);
        }

        [Route("{id}")]
        public async Task<IActionResult> UploadAsync(long id, CancellationToken cancellationToken) {
            if (!storage.SemaphoreDict.TryRemove(id, out var downloadSource)) {
                return NotFound(id);
            }
            TaskCompletionStream taskCompletionStream = new(Request.Body);
            downloadSource.SetResult((taskCompletionStream, Request.ContentType, Request.ContentLength));
            await taskCompletionStream.DisposeTask;
            return NoContent();
        }

    }
}
