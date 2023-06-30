namespace Visprech.Core.Interfaces
{
    public interface IFileDownloader
    {
        Task DownloadFrom(string uri, string destination);
    }
}
