using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.RegularExpressions;
using Visprech.Core.Exceptions;

namespace Visprech.Infrastructure.Config
{
    public class ConfigurationService
    {
        private readonly string _configFileName;
        private readonly Regex _configLineRegex = new Regex(@"[^\s]+");
        private readonly ILogger _logger;
        private readonly string _baseDir;

        public ConfigurationService(
            ILogger<ConfigurationService> logger,
            string baseDir
            ) 
        { 
            _logger = logger;
            _baseDir = baseDir;
            _configFileName = GetConfigFullPath(baseDir);
        }

        private string GetConfigFullPath(string baseDir)
        {
            var filePath = Path.Combine(baseDir, "app.conf");

            _logger.LogInformation("Configuration path set to: {ConfigurationPath}", filePath);

            return filePath;
        }

        public async Task<Configuration> GetOrCreateDefaultConfoguration()
        {
            if (!File.Exists(_configFileName))
            {
                CreateDefaultConfigFile();
            }

            return await LoadConfiguration();
        }

        private void CreateDefaultConfigFile()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Visprech.Infrastructure.Config.default-config.conf";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            using (var defaultConfigFile = new StreamWriter(_configFileName))
            {
                defaultConfigFile.Write(reader.ReadToEnd());
            }
        }

        private async Task<Configuration> LoadConfiguration()
        {
            var rawConfig = new RawConfiguration();

            var lines = await File.ReadAllLinesAsync(_configFileName);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith("#")) continue;
                var parameterWords = _configLineRegex
                    .Matches(line)
                    .Where(x => x.Success)
                    .Select(m => m.Value)
                    .ToList();
                var firstCommentIndex = parameterWords
                    .Select((w, i) => (isComment: w.StartsWith("#"), index: i))
                    .Where(wi => wi.isComment)
                    .Select(wi => wi.index)
                    .FirstOrDefault();
                if (firstCommentIndex > 0)
                {
                    parameterWords.RemoveRange(firstCommentIndex, parameterWords.Count - firstCommentIndex);
                }
                
                if (parameterWords.Count < 2)
                {
                    _logger.LogWarning("Wrong configuration line omitted: {Line}", line);
                    continue;
                }                

                SetParamter(parameterWords, ref rawConfig);
            }

            var cfg = MapConfiguration(rawConfig);

            ValidateConfiguration(cfg);

            return cfg;
        }

        private void ValidateConfiguration(Configuration cfg)
        {
            if (cfg.DesiredPhrases.Count == 0 && cfg.ProhibitedPhrases.Count == 0)
            {
                throw new ArgumentException("Configuration must have at least one phrase to check");
            }
        }

        private Configuration MapConfiguration(RawConfiguration rawConfig)
        {
            var config = new Configuration
            {
                FfmpegPtah = ValidateAndMakePathAbsolute(rawConfig.FfmpegPtah),
                OutputFilesPath = ValidateAndMakePathAbsolute(rawConfig.OutputFilesPath),
                WhisperFilesPath = ValidateAndMakePathAbsolute(rawConfig.WhisperFilesPath),
                Language = rawConfig.Language,
                GgmlType = rawConfig.GgmlType,

                ProhibitedPhrases = getList(rawConfig.ProhibitedPhrases),
                DesiredPhrases = getList(rawConfig.DesiredPhrases),

                ForcedTranscription = isStringTrue(rawConfig.ForcedTranscription),
                ForcedAudioExtraction = isStringTrue(rawConfig.ForcedAudioExtraction),
                ShowDetailsInReport = isStringTrue(rawConfig.ShowDetailsInReport),

                MaxLevensteinDistanceAcceptable = getNumber(rawConfig.MaxLevensteinDistanceAcceptable, nameof(rawConfig.MaxLevensteinDistanceAcceptable)),
                MinSearchingPhraseLen = getNumber(rawConfig.MinSearchingPhraseLen, nameof(rawConfig.MinSearchingPhraseLen)),
                AcceptableSimilarityInPercents = getNumber(rawConfig.AcceptableSimilarityInPercents, nameof(rawConfig.AcceptableSimilarityInPercents)),

                FfmpegZipUri = ValidateUri(rawConfig.FfmpegZipUri),
            };


            return config;

            bool isStringTrue(string str)
            {
                str = str.ToLower();
                if (str is "true" or "1" or "y" or "yes") return true;
                return false;
            }

            int getNumber(string str, string paramName)
            {
                if (int.TryParse(str, out int result))
                {
                    return result;
                }

                //TODO log error
                throw new ArgumentException($"Config file has wrong values. Correct {paramName}");
            }

            List<string> getList(string str)
            {
                return string.IsNullOrWhiteSpace(str)
                    ? new List<string>()
                    : str.Split(",")
                        .Select(v => v.Trim())
                        .Where(v => v.Length > 0)
                        .ToList();
            }
        }

        private string ValidateUri(string uri)
        {
            if (Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                return uri;

            _logger.LogError("The provided URI {Uri} is not well formatted", uri);

            throw new WrongConfigurationException($"The provided URI {uri} is not well formatted.");
        }

        private string ValidateAndMakePathAbsolute(string path)
        {
            var final = path;

            if (!Path.IsPathRooted(final)) 
            {
                final = Path.Combine(_baseDir, path);
            }

            _logger.LogInformation("Validating path {ConfigPath} and it's absolute variant {AbsolutePath}",
                path,
                final);

            try
            {
                DirectoryInfo dir = new DirectoryInfo(final);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "The provided path is not valid {Path}", final);
                throw new WrongConfigurationException($"The provided path is not valid {final}.");
            }

            return final;
        }

        private void SetParamter(List<string> parameterWords, ref RawConfiguration config)
        {
            var propertyName = parameterWords.First();

            var property = config.GetType().GetProperty(propertyName);

            if (property is null)
            {
                _logger.LogWarning("There is no config for given name {Parameter}", propertyName);
                return;
            }

            var value = string.Join(" ", parameterWords.Skip(1));

            property.SetValue(config, value);
        }
    }
}