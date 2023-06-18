﻿using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;
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

        public async Task Start(string[] args)
        {
            _logger.LogInformation("Worker started.");

            _messageWriter.WriteHeader(AppHeader);

            _logger.LogInformation("Validating arguments...");
            var file = ValidateArguments(args);

            _logger.LogInformation("Transcribing input file...");
            var transcription = await _mediaTranscriptor.Transcript(file);
            
            _logger.LogInformation("Checking transcript...");
            _transcriptionProcessor.Check(transcription);

            _logger.LogInformation("Job done!");

            _messageWriter.WriteEmptyLine();
            _messageWriter.WriteHeader(AnyKeyToExit);

            Console.ReadKey(intercept: true);
        }

        private string ValidateArguments(string[] args)
        {
            if (args.Length == 0)
            {
                const string Message = "Execute the program with a file to transcript";
                _messageWriter.WriteWarn(Message);
                _logger.LogInformation(Message);
                Environment.Exit(1);
            }

            string file = args[0];
            if (!File.Exists(file))
            {
                string message = $"Provided file must exist. This does not look like proper file: {file}";
                _messageWriter.WriteWarn(message);
                _logger.LogInformation(message);
                Environment.Exit(2);
            }

            return file;
        }
    }
}