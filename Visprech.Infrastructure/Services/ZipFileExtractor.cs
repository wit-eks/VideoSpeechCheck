using Microsoft.Extensions.Logging;
using System.IO.Compression;
using Visprech.Core.Exceptions;
using Visprech.Core.Interfaces;

namespace Visprech.Infrastructure.Services
{
    internal class ZipFileExtractor : IZipFileExtractor
    {
        private readonly ILogger _logger;

        public ZipFileExtractor(ILogger<ZipFileExtractor> logger)
        {
            _logger = logger;
        }

        public async Task ExtractFile(string zipFilePath, string fileToExtract, string extractFileTo)
        {
            try
            {
                _logger.LogInformation("Extracting file {FileToExtract} from {ZipFile} to {DestinationFile}",
                    fileToExtract,
                    zipFilePath,
                    extractFileTo);

                var zipFile = ZipFile.OpenRead(zipFilePath);

                var entry = zipFile
                    .Entries
                    .Where(e => e.Name.Equals(fileToExtract))
                    .FirstOrDefault();

                if (entry is null)
                {
                    _logger.LogError("There is no file {FileToExtract} in zip archive", fileToExtract);
                    throw new ProcessingException("File to extract was not found in a zip archive");
                }

                var task = Task.Run(() => entry.ExtractToFile(extractFileTo, overwrite: true));
                await task;

                _logger.LogInformation("Extraction of {FileToExtract} finished", fileToExtract);

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
