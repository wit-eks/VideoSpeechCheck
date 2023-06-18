namespace Visprech.Core.Interfaces
{
    //TODO probably it would be better to create Transcript serializer (ReadableTranscriptionLine) and inject it here
    public interface ITranscriptionResultHandler
    {
        Task Save(List<(TimeSpan from, TimeSpan to, string text)> transcription, string id);
        Task<List<(TimeSpan from, TimeSpan to, string text)>> Load(string id);
        string ReadableTranscriptionLine(TimeSpan from, TimeSpan to, string text);
    } 
}
