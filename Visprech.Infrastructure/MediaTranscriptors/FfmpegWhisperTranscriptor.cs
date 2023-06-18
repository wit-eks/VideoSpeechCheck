using Visprech.Core.Interfaces;
using Visprech.Infrastructure.MediaTranscriptors.Services;

namespace Visprech.Infrastructure.MediaTranscriptors
{
    public class FfmpegWhisperTranscriptor : IMediaTranscriptor
    {
        private readonly FfmpegAudioPreparer _audioPreparer;
        private readonly WhisperOpenAiTranscriptor _openAiTranscriptor;

        public FfmpegWhisperTranscriptor(
            FfmpegAudioPreparer audioPreparer, 
            WhisperOpenAiTranscriptor openAiTranscriptor)
        {
            _audioPreparer = audioPreparer;
            _openAiTranscriptor = openAiTranscriptor;
        }

        public async Task<List<(TimeSpan from, TimeSpan to, string text)>> Transcript(string mediaFilePath)
        {
            var audioFile = await _audioPreparer.PrepareFile(mediaFilePath);

            var transcription = await _openAiTranscriptor.TranscriptAudio(audioFile);

            return transcription;
        }
    }
}
