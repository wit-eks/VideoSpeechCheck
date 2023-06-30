using Microsoft.Extensions.Logging;
using System.IO.Compression;
using Visprech.Core.Exceptions;
using Visprech.Core.Interfaces;

namespace Visprech.Infrastructure.Services
{
    public class ZipFileExtractor : IZipFileExtractor
    {
        private readonly ILogger _logger;

        private readonly IMessageWriter _messageWriter;

        public ZipFileExtractor(ILogger<ZipFileExtractor> logger, IMessageWriter messageWriter)
        {
            _logger = logger;
            _messageWriter = messageWriter;
        }

        public async Task ExtractFile(string zipFilePath, string fileToExtract, string extractFileTo)
        {
            try
            {
                _logger.LogInformation("Extracting file {FileToExtract} from {ZipFile} to {DestinationFile}",
                    fileToExtract,
                    zipFilePath,
                    extractFileTo);

                _messageWriter.Write($"Extracting file {fileToExtract}...");

                using var zipFile = ZipFile.OpenRead(zipFilePath);

                var entry = zipFile
                    .Entries
                    .Where(e => e.Name.Equals(fileToExtract))
                    .FirstOrDefault();

                if (entry is null)
                {
                    _messageWriter.WriteWarn($"Oops, file {fileToExtract} not found in the zip file!");
                    _logger.LogError("There is no file {FileToExtract} in zip archive", fileToExtract);
                    throw new ProcessingException("File to extract was not found in a zip archive");
                }

                var task = Task.Run(() => entry.ExtractToFile(extractFileTo, overwrite: true));
                await task;

                _logger.LogInformation("Extraction of {FileToExtract} finished", fileToExtract);
                _messageWriter.Write("Extraction of {fileToExtract} finished");

                return;
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Extraction of {FileToExtract} from {ZipFile} failed. See exception details",
                    fileToExtract,
                    zipFilePath);
                throw;
            }
        }
    }
}
