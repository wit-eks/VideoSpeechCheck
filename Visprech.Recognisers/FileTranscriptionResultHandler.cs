using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Visprech.Core.Interfaces;

namespace Visprech.Core
{
    public class FileTranscriptionResultHandler : ITranscriptionResultHandler
    {
        private readonly static Regex _transRgx = new(@"^\[(?<from>[0-9:.]*)\] \[(?<to>[0-9:.]*)\]\:(?<text>.*)$");


        private readonly IMessageWriter _messageWriter;
        private readonly string _outputFolder;
        private readonly ILogger _logger;

        public FileTranscriptionResultHandler(
            IMessageWriter messageWriter,
            IConfiguration configuration,
            ILogger<FileTranscriptionResultHandler> logger)
        {
            _messageWriter = messageWriter;
            _outputFolder = configuration.OutputFilesPath;
            _logger = logger;
        }

        string TimeSpanToString(TimeSpan ts)
        {
            var ds = ts.Milliseconds / 100;
            const string tsFormat = @"hh\:mm\:ss";
            return ts.ToString(tsFormat) + $".{ds}";
        }


        public async Task<List<(TimeSpan from, TimeSpan to, string text)>> Load(string fileName)
        {
            var path = GetFilePath(fileName);

            if (!File.Exists(path))
            {
                _messageWriter.WriteFailure($"File with transcription does not exits: {path}");
                throw new FileNotFoundException("File with transcription does not exits", path);
            }

            _logger.LogInformation("Loading transcript {Transcript} from {Path}", Path.GetFileNameWithoutExtension(fileName), path);


            List<(TimeSpan from, TimeSpan to, string text)> transcriptions = new();

            const int BufferSize = 128;
            using (var fileStream = File.OpenRead(path))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
            {
                string line;
                while ((line = await streamReader.ReadLineAsync()) != null)
                {
                    if (TryGetTranscription(line, out var transcription))
                    {
                        transcriptions.Add(transcription);
                    }
                }
            }

            return transcriptions;
        }

        public async Task Save(List<(TimeSpan from, TimeSpan to, string text)> transcription, string fileName)
        {
            var path = GetFilePath(fileName);

            Directory.CreateDirectory(_outputFolder);

            _logger.LogInformation("Saving transcript {Transcript} to {Path}", Path.GetFileNameWithoutExtension(fileName), path);

            using var outTxtFile = new StreamWriter(path);

            var lastTimeNoted = TimeSpan.Zero;
            foreach (var t in transcription)
            {
                var line = ReadableTranscriptionLine(t.from, t.to, t.text);
                await outTxtFile.WriteLineAsync(line);
            }
        }

        public string ReadableTranscriptionLine(TimeSpan from, TimeSpan to, string text) 
        {
            var st = TimeSpanToString(from);
            var en = TimeSpanToString(to);
            return $"[{st}] [{en}]: {text}";
        }

        private string GetFilePath(string fileName)
        {
            return Path.Combine(_outputFolder, fileName);
        }


        private bool TryGetTranscription(string input, out (TimeSpan from, TimeSpan to, string text) transcription)
        {
            var m = _transRgx.Match(input);

            (TimeSpan from, TimeSpan to, string text) zero = (TimeSpan.Zero, TimeSpan.Zero, string.Empty);

            if (string.IsNullOrWhiteSpace(input))
            {
                _messageWriter.Write($"Empty line omitted: {input}");
                transcription = zero;
                return false;
            }

            if (!m.Success)
            {
                _messageWriter.Write($"Line omitted: {input}");
                transcription = zero;
                return false;
            }

            try
            {
                TimeSpan from = TimeSpan.Parse(m.Groups["from"].Value);
                TimeSpan to = TimeSpan.Parse(m.Groups["to"].Value);


                if (m.Groups["text"].Value.TrimStart().Substring(0, 1) == "[")
                {
                    _messageWriter.Write($"Not speech line omitted: {input}");
                    transcription = zero;
                    return false;
                }

                string text = m.Groups["text"].Value;

                transcription = (from, to, text);
                return true;
            }
            catch (Exception ex)
            {
                _messageWriter.WriteWarn($"Line omitted because of exception, line: {input}.");
                _messageWriter.WriteWarn($"Exception: {ex.Message}.");
                transcription = zero;
                return false;
            }
        }
    }
}