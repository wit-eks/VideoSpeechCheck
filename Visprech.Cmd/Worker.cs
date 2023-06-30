using Microsoft.Extensions.Logging;
using Visprech.Core;
using Visprech.Core.Interfaces;
using static Visprech.Cmd.ConsoleMessages;

namespace Visprech.Cmd
{
    public class Worker
    {
        private readonly TranscriptionProcessor _transcriptionProcessor;

        private readonly IMediaTranscriptor _mediaTranscriptor;
        private readonly IConfiguration _configuration;
        private readonly IMessageWriter _messageWriter;
        private readonly ILogger _logger;

        public Worker(
            TranscriptionProcessor transcriptionProcessor,
            IMediaTranscriptor mediaTranscriptor,
            ITranscriptionResultHandler transcriptionHandler,
            IConfiguration configuration,
            ILogger<Worker> logger,
            IMessageWriter messageWriter)
        {
            _transcriptionProcessor = transcriptionProcessor;
            _mediaTranscriptor = mediaTranscriptor;
            _configuration = configuration;
            _logger = logger;
            _messageWriter = messageWriter;
        }

        public async Task<int> Start(string[] args)
        {
            _logger.LogInformation("Worker started.");

            _messageWriter.WriteHeader(AppHeader);

            _logger.LogInformation("Validating arguments...");
            if(!TryValidateArguments(args, out var file, out int exitCode))
            {
                return exitCode;
            }

            _logger.LogInformation("Transcribing input file...");
            var transcription = await _mediaTranscriptor.Transcript(file);
            
            _logger.LogInformation("Checking transcript...");
            _transcriptionProcessor.Check(transcription);

            _logger.LogInformation("Job done!");

            return 0;
        }

        private bool TryValidateArguments(string[] args, out string? file, out int exitCode)
        {
            file = null;
            exitCode = 0;

            if (args.Length == 0)
            {
                const string Message = "Execute the program with a file to transcript";
                _messageWriter.WriteWarn(Message);
                _logger.LogInformation(Message);
                exitCode = 1;
                return false;
            }

            file = args[0];
            if (!File.Exists(file))
            {
                string message = $"Provided file must exist. This does not look like proper file: {file}";
                _messageWriter.WriteWarn(message);
                _logger.LogInformation(message);
                exitCode = 2;
                return false;
            }

            return true;
        }
    }
}