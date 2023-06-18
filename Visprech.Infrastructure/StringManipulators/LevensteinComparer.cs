using Fastenshtein;
using Visprech.Core.Interfaces;

namespace Visprech.Infrastructure.PhraseComparers
{
    public class LevensteinComparer : IPhraseComparer
    {
        private readonly int _isSimiliarMaxLen;
        private readonly int _similarityInPercents;
        private readonly bool _isPercentSimilaritySet;

        private Levenshtein _subject;

        public LevensteinComparer(IConfiguration configuration)
        {
            _isSimiliarMaxLen = configuration.MaxLevensteinDistanceAcceptable;
            _similarityInPercents = configuration.AcceptableSimilarityInPercents;
            _isPercentSimilaritySet = _similarityInPercents is > 0 and <= 100;
        }
        
        public void SetSubject(string subject)
        {
            _subject = new Levenshtein(subject);
        }

        public bool IsNotSimilarTo(string compareWith)
        {
            return _isPercentSimilaritySet 
                ? SimilarityPercent(compareWith) < _similarityInPercents
                : _subject.DistanceFrom(compareWith) > _isSimiliarMaxLen;
        }

        public bool IsSimilarTo(string compareWith)
        {
            return _isPercentSimilaritySet
                ? SimilarityPercent(compareWith) >= _similarityInPercents
                : _subject.DistanceFrom(compareWith) <= _isSimiliarMaxLen;
        }

        public int SimilarityPercent(string compareWith)
        {
            var d = _subject.DistanceFrom(compareWith);
            if (d >= _subject.StoredLength) { return 0; }
            if (d == 0) return 100;

            return 100 * (_subject.StoredLength - d) / _subject.StoredLength;
        }
    }
}
