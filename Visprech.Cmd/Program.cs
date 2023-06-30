﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Visprech.Cmd;
using Visprech.Core;
using Visprech.Core.Exceptions;
using Visprech.Core.Interfaces;
using Visprech.Infrastructure.Config;
using Visprech.Infrastructure.Services;
using Visprech.Infrastructure.MediaTranscriptors;
using Visprech.Infrastructure.MediaTranscriptors.Services;
using Visprech.Infrastructure.PhraseComparers;
using Serilog;
using Serilog.Extensions.Logging;
using static Visprech.Cmd.ConsoleMessages;

int exitCode = 0;

try
{
    var baseDir = AppDomain.CurrentDomain.BaseDirectory;

    var logPath = Path.Combine(baseDir, @"logs/visprech.log.txt");

    var logger = new LoggerConfiguration()
                      .MinimumLevel.Information()
                      .Enrich.FromLogContext()
                      .WriteTo.File(
                        logPath,
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: 10,
                        fileSizeLimitBytes: 1024 * 1024,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] ({SourceContext}.{Method}) {Message}{NewLine}{Exception}"
                        )
                      .CreateLogger();

    logger.Information("### Application started");

    try
    {
        var msConfigyartionServiceLogger = new SerilogLoggerFactory(logger)
            .CreateLogger<ConfigurationService>();
        var cs = new ConfigurationService(
            msConfigyartionServiceLogger,
            baseDir
            );
        Configuration conf = await cs.GetOrCreateDefaultConfoguration();

        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddSingleton<IConfiguration>(conf);
                services.AddSingleton<IMessageWriter, ConsoleWriter>();
                services.AddSingleton<IPhraseComparer, LevensteinComparer>();
                services.AddSingleton<IDiacriticsCleaner, DiacriticsCleaner>();
                services.AddSingleton<IFileDownloader, FileDownloader>();
                services.AddSingleton<IZipFileExtractor, ZipFileExtractor>();
                services.AddSingleton<FfmpegAudioPreparer>();
                services.AddSingleton<WhisperOpenAiTranscriptor>();
                services.AddSingleton<IMediaTranscriptor, FfmpegWhisperTranscriptor>();
                services.AddSingleton<TranscriptionProcessor>();
                services.AddSingleton<ITranscriptionResultHandler, FileTranscriptionResultHandler>();
                services.AddSingleton<Worker>();
                services.AddLogging(builder => {
                    builder.ClearProviders();
                    builder.AddSerilog(logger);
                });
            })
            .Build();

        var worker = host.Services.GetService<Worker>();
        exitCode = await worker.Start(args);
    }
    catch (WrongConfigurationException ce)
    {
        logger.Warning(ce, "Wrong configuration noted.");
        ConsoleWriter cw = new();
        cw.WriteFailure(ce.Message);
        exitCode = 21;
    }
    catch (ProcessingException pe)
    {
        logger.Error(pe, "Processing error noted");
        ConsoleWriter cw = new();
        cw.WriteInternalError(pe.Message);
        exitCode = 22;
    }
    catch (Exception e)
    {
        logger.Error(e, "Ohooo, unknown error");
        ConsoleWriter cw = new();
        cw.WriteInternalError(e.Message);
        exitCode = 30;
    }
    finally
    {
        logger.Information("### Application closed");
        logger.Dispose();        
    }
}
catch (Exception e)
{
    Console.BackgroundColor = ConsoleColor.DarkRed;
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("Unexpected error:");
    Console.WriteLine(e.Message);
    Console.ResetColor();
    Console.WriteLine();

    exitCode = 40;
}

Console.WriteLine();
Console.BackgroundColor = ConsoleColor.DarkGray;
Console.ForegroundColor = ConsoleColor.Cyan;
Console.Write(AnyKeyToExit);
Console.ResetColor();
Console.WriteLine();
Console.ReadKey(intercept: true);
Environment.Exit(exitCode);