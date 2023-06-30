namespace Visprech.Core.Interfaces
{
    public interface IMessageWriter
    {
        void Write(string text);
        void WriteEmptyLine();
        void WriteNotyfication(string text);
        void WriteMainNotyfication(string text);
        void WriteSuccess(string text);
        void WriteWarn(string text);
        void WriteFailure(string text);
        void WriteHeader(string text);
        void WriteInternalError(string text);
    }
}
