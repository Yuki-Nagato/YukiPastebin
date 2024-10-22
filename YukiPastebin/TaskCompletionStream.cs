

namespace YukiPastebin {
    public class TaskCompletionStream : Stream {
        private readonly Stream innerStream;
        private readonly TaskCompletionSource disposeCompletionSource = new();

        public TaskCompletionStream(Stream innerStream) {
            this.innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
        }

        // Property to expose the Task that will complete when the stream is disposed
        public Task DisposeTask => disposeCompletionSource.Task;

        public override bool CanRead => innerStream.CanRead;

        public override bool CanSeek => innerStream.CanSeek;

        public override bool CanWrite => innerStream.CanWrite;

        public override long Length => innerStream.Length;

        public override long Position { get => innerStream.Position; set => innerStream.Position = value; }

        public override void Flush() => innerStream.Flush();

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException("Use ReadAsync instead.");

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => innerStream.ReadAsync(buffer, offset, count, cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) => innerStream.Seek(offset, origin);

        public override void SetLength(long value) => innerStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException("Use WriteAsync instead.");

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => innerStream.WriteAsync(buffer, offset, count, cancellationToken);

        protected override void Dispose(bool disposing) {
            if (disposing) {
                disposeCompletionSource.TrySetResult();  // Complete the Task
                innerStream.Dispose();
            }
            base.Dispose(disposing);
        }
    }

}
