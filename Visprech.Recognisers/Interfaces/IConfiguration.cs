namespace Visprech.Core.Interfaces
{
    public interface IConfiguration
    {
        int AcceptableSimilarityInPercents { get; set; }
        List<string> DesiredPhrases { get; set; }
        string FfmpegPtah { get; set; }
        bool ForcedAudioExtraction { get; set; }
        bool ForcedTranscription { get; set; }
        string GgmlType { get; set; }
        string Language { get; set; }
        int MaxLevensteinDistanceAcceptable { get; set; }
        int MinSearchingPhraseLen { get; set; }
        List<string> ProhibitedPhrases { get; set; }
        bool ShowDetailsInReport { get; set; }
        string WhisperFilesPath { get; set; }
        string OutputFilesPath { get; set;}
    }
}
