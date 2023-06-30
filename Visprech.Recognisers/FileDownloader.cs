using Microsoft.Extensions.Logging;
using System.Net;
using Visprech.Core.Interfaces;

namespace Visprech.Core
{
    public class FileDownloader : IFileDownloader
    {
        private readonly IMessageWriter _messageWriter;
        private readonly ILogger<FileDownloader> _logger;

        public FileDownloader(ILogger<FileDownloader> logger, IMessageWriter messageWriter)
        {
            _logger = logger;
            _messageWriter = messageWriter;
        }

        public async Task DownloadFrom(string uri, string destination)
        {
            _logger.LogInformation("Starting downloading from {Uri} to file {DestinationFile}", uri, destination);
            string destinationFileName = Path.GetFileName(destination);
            _messageWriter.Write($"Downloading of {destinationFileName} started...");
            
            using var client = new WebClient();

            client.DownloadProgressChanged += DownloadProgressChanged;
           
            await client.DownloadFileTaskAsync(new Uri(uri), destination);

            _logger.LogInformation("Downloading of {DestinationFile} finished", destination);

            _messageWriter.Write($"Downloading of {destinationFileName} finished");
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            _logger.LogInformation("Downloading done in {ProgressPercentage}%", e.ProgressPercentage);
            _messageWriter.Write($"Downloading done in {e.ProgressPercentage}%");
        }
    }
}