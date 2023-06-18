using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Visprech.Core.Exceptions;
using Visprech.Core.Interfaces;
using Whisper.net;
using Whisper.net.Ggml;

namespace Visprech.Infrastructure.MediaTranscriptors.Services
{
    public class WhisperOpenAiTranscriptor
    {
        private readonly string _language;
        private readonly string _oaiFolder;
        private readonly string _ggmlTypeStr;
        private readonly bool _force;
        private readonly IMessageWriter _messageWriter;
        private readonly ITranscriptionResultHandler _transcriptionResultHandler;
        private readonly ILogger _logger;

        public WhisperOpenAiTranscriptor(
            IConfiguration configuration,
            IMessageWriter messageWriter,
            ITranscriptionResultHandler transcriptionResultHandler,
            ILogger<WhisperOpenAiTranscriptor> logger)
        {
            _language = configuration.Language;
            _oaiFolder = configuration.WhisperFilesPath;
            _ggmlTypeStr = configuration.GgmlType;
            _force = configuration.ForcedTranscription;
            _messageWriter = messageWriter;
            _transcriptionResultHandler = transcriptionResultHandler;
            _logger = logger;
        }


        public async Task<List<(TimeSpan from, TimeSpan to, string text)>> TranscriptAudio(
            string audioFilePath)
        {
            if(!Enum.TryParse(_ggmlTypeStr, out GgmlType ggmlType))
            {
                throw new WrongConfigurationException("Provided GgmlType is out of range");
            }
            
            var ggml = await CheckAndDownloadDictionary(ggmlType);

            var res = await ParseWave(audioFilePath, ggml);

            return res;
        }

        private async Task<List<(TimeSpan from, TimeSpan to, string text)>> 
            ParseWave(string audioFilePath, string ggml)
        {
            try
            {
                string msg = "Transcribing started...";
                _messageWriter.Write(msg);
                _logger.LogInformation(msg);

                List<(TimeSpan from, TimeSpan to, string text)> transcription = new();

                var workingDir = Path.GetDirectoryName(audioFilePath);

                var mediaFileName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(audioFilePath));

                var transcriptName = mediaFileName + GetTranscriptFileExtension();

                var outputTransPath = Path.Combine(workingDir, transcriptName);

                _logger.LogInformation("Checking existence of transcript {TranscriptFile}", outputTransPath);
                
                bool fileExist = File.Exists(outputTransPath);
                if (fileExist && !_force)
                {
                    msg = $"Transcription has been done already: {transcriptName}";
                    _messageWriter.WriteWarn(msg);
                    _logger.LogInformation(msg);

                    var tarnscription = await _transcriptionResultHandler.Load(outputTransPath);

                    return tarnscription;
                }

                msg = $"{(_force && fileExist ? "[FORCED] " : string.Empty)}Transcribing audio file...";

                _logger.LogInformation(msg);
                _messageWriter.Write(msg);

                _messageWriter.Write($"Text will be written into: {outputTransPath}");

                using var whisperFactory = WhisperFactory.FromPath(ggml);
                _messageWriter.Write("Whisper Factory built.");

                using var processor = whisperFactory.CreateBuilder()
                     .WithLanguage(_language)
                     .Build();
                
                msg = $"Processor built with language {_language}.";
                _messageWriter.Write(msg);
                _logger.LogInformation(msg);

                using var fileStream = File.OpenRead(audioFilePath);
                _messageWriter.Write($"Input audio file opened ({audioFilePath})");

                msg = $"<<< Processing started";
                _messageWriter.Write(msg);
                _logger.LogInformation(msg);

                var sw = new Stopwatch();
                sw.Start();
                var lastTimeNoted = TimeSpan.Zero;
                await foreach (var result in processor.ProcessAsync(fileStream))
                {
                    var line = _transcriptionResultHandler.ReadableTranscriptionLine(
                        result.Start,
                        result.End,
                        result.Text);                        ;
                    transcription.Add((result.Start, result.End, result.Text));
                    _messageWriter.Write(line);
                    
                    lastTimeNoted = result.End;
                }
                sw.Stop();

                msg = $">>> Processing ended.";
                _messageWriter.WriteNotyfication(msg);
                _logger.LogInformation(msg);
                var speed = 1.0 * lastTimeNoted.TotalSeconds / sw.Elapsed.TotalSeconds;
                msg = $">>> Speed: {speed.ToString("N2")}x";
                _messageWriter.WriteNotyfication(msg);
                _logger.LogInformation(msg);

                _logger.LogInformation("Saving transcript...");
                await _transcriptionResultHandler.Save(transcription, transcriptName);

                return transcription;
            }
            catch (Exception ex)
            {
                _messageWriter.WriteInternalError("Exception occurred:");
                _messageWriter.WriteInternalError(ex.Message);
                throw new ProcessingException(" Filed when transcribing audio file", ex);
            }
        }



        private string GetTranscriptFileExtension()
        {
            return $".speech-{_ggmlTypeStr.ToLower()}.txt";
        }

        private async Task<string> CheckAndDownloadDictionary(GgmlType ggml)
        {
            try
            {
                var ggmlFile = $"ggml-{ggml.ToString().ToLower()}.bin";

                var ggmlPath = Path.Combine(_oaiFolder, ggmlFile);

                _messageWriter.Write($"Checking existence of ggml at: {ggmlPath}");
                Directory.CreateDirectory(_oaiFolder);

                if (!File.Exists(ggmlPath))
                {
                    _messageWriter.WriteNotyfication("downloading ggml...");
                    using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(ggml);
                    using var fileWriter = File.OpenWrite(ggmlPath);
                    await modelStream.CopyToAsync(fileWriter);
                    _messageWriter.Write("downloaded.");
                }
                else
                {
                    _messageWriter.Write("ggml exists.");
                }

                return ggmlPath;
            }
            catch (Exception ex)
            {
                _messageWriter.WriteInternalError("Exception occurred when try to get a ggml model:");
                _messageWriter.WriteInternalError(ex.Message);
                throw new ProcessingException("Filed when setting ggml model", ex);
            }
        }
    }
}