using Visprech.Core.Interfaces;


namespace Visprech.Infrastructure.Config
{
    public class RawConfiguration
    {

        public string FfmpegPtah { get; set; } = @"ffmpeg\ffmpeg.exe";
        public string WhisperFilesPath { get; set; } = "whisper";
        public string OutputFilesPath { get; set; } = "output";
        public string Language { get; set; } = "auto";
        public string GgmlType { get; set; } = "Tiny";


        public string ProhibitedPhrases { get; set; }
        public string DesiredPhrases { get; set; }

        public string ForcedTranscription { get; set; } = "Y";
        public string ForcedAudioExtraction { get; set; } = "Y";
        public string ShowDetailsInReport { get; set; } = "Y";

        public string MaxLevensteinDistanceAcceptable { get; set; } = "2";
        public string MinSearchingPhraseLen { get; set; } = "3";
        public string AcceptableSimilarityInPercents { get; set; } = "0";
    }

    public class Configuration : IConfiguration
    {
        public string FfmpegPtah { get; set; }
        public string WhisperFilesPath { get; set; }
        public string Language { get; set; }
        public string GgmlType { get; set; }


        public List<string> ProhibitedPhrases { get; set; } = new();
        public List<string> DesiredPhrases { get; set; } = new();

        public bool ForcedTranscription { get; set; }
        public bool ForcedAudioExtraction { get; set; }
        public bool ShowDetailsInReport { get; set; }

        public int MaxLevensteinDistanceAcceptable { get; set; }
        public int MinSearchingPhraseLen { get; set; }
        public int AcceptableSimilarityInPercents { get; set; }
        public string OutputFilesPath { get; set; }
    }
}