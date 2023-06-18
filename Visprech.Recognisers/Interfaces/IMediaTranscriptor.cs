namespace Visprech.Core.Interfaces
{
    public interface IMediaTranscriptor
    {
        Task<List<(TimeSpan from, TimeSpan to, string text)>> Transcript(string fileMediaPath);
    }
 
}
