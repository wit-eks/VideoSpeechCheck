using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Visprech.Core.Exceptions;
using Visprech.Core.Interfaces;

namespace Visprech.Infrastructure.MediaTranscriptors.Services
{
    public class FfmpegAudioPreparer
    {
        private const string destinationExtension = ".prepared.wav";

        private readonly bool _force;
        private readonly string _ffmpegFilePath;
        private readonly string _ffmpegZipUri;
        private readonly string _outputFolder;
        private readonly IMessageWriter _messageWriter;
        private readonly ILogger _logger;
        private readonly IFileDownloader _fileDownloader;
        private readonly IZipFileExtractor _zipfileExtractor;

        public FfmpegAudioPreparer(
            IConfiguration configuration,
            IMessageWriter messageWriter,
            ILogger<FfmpegAudioPreparer> logger,
            IFileDownloader fileDownloader,
            IZipFileExtractor zipfileExtractor)
        {
            _force = configuration.ForcedAudioExtraction;
            _ffmpegFilePath = configuration.FfmpegPtah;
            _ffmpegZipUri = configuration.FfmpegZipUri;
            _outputFolder = configuration.OutputFilesPath;
            _messageWriter = messageWriter;
            _logger = logger;
            _fileDownloader = fileDownloader;
            _zipfileExtractor = zipfileExtractor;
        }

        public async Task<string> PrepareFile(string inputFilePath)
        {
            await CheckIfExistsAndDownloadFfmpeg();

            string startMessage = $"Extracting audio stream from: {inputFilePath}";

            _logger.LogInformation(startMessage);
            _messageWriter.Write(startMessage);

            var outputFileName = Path.GetFileNameWithoutExtension(inputFilePath) + destinationExtension;
            var outputFilePath = Path.Combine(_outputFolder, outputFileName);

            Directory.CreateDirectory(_outputFolder);

            _logger.LogInformation("Checking existence of audio stream {AudioFile}", outputFilePath);

            bool fileExist = File.Exists(outputFilePath);
            if (fileExist && !_force)
            {
                _messageWriter.WriteWarn($"Extracted audio stream already exists: {outputFileName}");
                return outputFilePath;
            }

            var ffmpegExe = $"\"{_ffmpegFilePath}\"";

            var arguments = $" -y -i \"{inputFilePath}\" -acodec pcm_s16le -ac 1 -ar 16000 \"{outputFilePath}\"";
                        
            var processMessage = $"{(_force && fileExist ? "[FORCED] ": string.Empty)}Extracting audio stream using ffmpeg: {ffmpegExe}{arguments}";
            _logger.LogInformation(processMessage);
            _messageWriter.Write(processMessage);

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = ffmpegExe;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = false;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
  
                    process.OutputDataReceived += new DataReceivedEventHandler(OutputDataHandler);
                    process.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataHandler);
                    
                    //ffmpeg -i <file-name>.mp3 -acodec pcm_s16le -ac 1 -ar 16000 <file-name>.prepared.wav
                    if (!process.Start())
                    {
                        throw new ProcessingException("Looks like ffmpeg process is reused. Check running processes and try again.");
                    }

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    await process.WaitForExitAsync();
                }

                var finishedMessage = $"File prepared: {outputFilePath}";
                _logger.LogInformation(finishedMessage);
                _messageWriter.Write(finishedMessage);
                return outputFilePath;
            }
            catch (Exception ex)
            {
                _messageWriter.WriteInternalError(ex.ToString());
                throw;
            }
        }

        private async Task CheckIfExistsAndDownloadFfmpeg()
        {
            if (File.Exists(_ffmpegFilePath)) return;

            _messageWriter.WriteNotyfication("Downloading ffmpeg binary for the first time.");

            var downloadFolder = Path.Combine(Path.GetDirectoryName(_ffmpegFilePath), "_tmp_download");
            Directory.CreateDirectory(downloadFolder);

            var zippedFfmpeg = Path.Combine(downloadFolder, "ffmpeg.zip");

            await _fileDownloader.DownloadFrom(_ffmpegZipUri, zippedFfmpeg);

            await _zipfileExtractor.ExtractFile(zippedFfmpeg, "ffmpeg.exe", _ffmpegFilePath);

            Directory.Delete(downloadFolder, recursive: true);

            _messageWriter.Write("Downloading of ffmpeg finished");
        }

        private void ErrorDataHandler(
            object sendingProcess,
           DataReceivedEventArgs errLine)
        {
            if (string.IsNullOrEmpty(errLine.Data)) return;

            _logger.LogWarning("Audio conversion wrote error line {ErrorLine}", errLine.Data);
        }

        private void OutputDataHandler(
            object sendingProcess,
           DataReceivedEventArgs outLine)
        {
            if (string.IsNullOrEmpty(outLine.Data)) return;

            _logger.LogInformation("Audio conversion wrote line {Line}", outLine.Data);
        }
    }
}