namespace Visprech.Core.Interfaces
{
    public interface IPhraseComparer
    {
        void SetSubject(string subject);
        bool IsSimilarTo(string compareWith);
        bool IsNotSimilarTo(string compareWith);
        int SimilarityPercent(string compareWith);
    }
}
