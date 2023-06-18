using Moq;
using Visprech.Core.Interfaces;
using Visprech.Infrastructure.PhraseComparers;

namespace Visprech.Tests.Recognisers.Extensions
{
    public class LevensteinComparerTests
    {
        private LevensteinComparer _comparer;
        private Mock<IConfiguration> _configuration;

        [SetUp]
        public void Setup()
        {
            _configuration = new Mock<IConfiguration>();
            
            _configuration.SetupGet(c => c.MaxLevensteinDistanceAcceptable).Returns(2);

            _comparer = new LevensteinComparer(_configuration.Object);
        }

        [TestCase("word", "zxcv")]
        [TestCase("word", "zxcvzxcv")]
        [TestCase("word", "zxcvzxcvzxcvzxcv")]
        [TestCase("word", "xx")]
        [TestCase("word", "e")]
        public void Checked_versus_toatally_different_word__returns_zero(string word, string compareWith)
        {
            _comparer.SetSubject(word);                       

            Assert.That(_comparer.SimilarityPercent(compareWith), Is.EqualTo(0));
        }

        [TestCase("word", "word")]
        [TestCase("zxcvzxcv", "zxcvzxcv")]
        [TestCase("word123", "word123")]
        [TestCase("aaaa", "aaaa")]
        public void Checked_versus_the_same_word__returns_100(string word, string compareWith)
        {
            _comparer.SetSubject(word);

            Assert.That(_comparer.SimilarityPercent(compareWith), Is.EqualTo(100));
        }

        [TestCase("word", "word1")]
        [TestCase("ranczo", "rancz")]
        [TestCase("ranczo", "rnczo")]
        [TestCase("ranczo", "raczo")]
        [TestCase("ranczo", "anczo")]
        [TestCase("word123", "word124")]
        [TestCase("aaaa", "abaa")]
        [TestCase("zxcvzxcvzxcvzxcv", "zxcvzxcv2xcvzxcv")]
        public void Checked_versus_one_letter_different__returns_something_from_70_to_99(string word, string compareWith)
        {
            _comparer.SetSubject(word);

            var exepectedSimilarity = 100 * (word.Length - 1) / word.Length;
            Console.WriteLine($"{word} vs {compareWith} : expected {exepectedSimilarity}");
            Assert.That(_comparer.SimilarityPercent(compareWith), Is.EqualTo(exepectedSimilarity));
            Assert.That(_comparer.SimilarityPercent(compareWith), Is.GreaterThanOrEqualTo(70));
            Assert.That(_comparer.SimilarityPercent(compareWith), Is.LessThanOrEqualTo(99));
        }
        [TestCase("word", "wxxx")]
        [TestCase("ranczo", "wwnwww")]
        [TestCase("word123", "aaaabb3")]
        [TestCase("aaaaaa", "abcdef")]
        public void Checked_versus_one_letter_that_matches__returns_one_per_input_len(string word, string compareWith)
        {
            _comparer.SetSubject(word);

            var exepectedSimilarity = 100 / word.Length;
            Console.WriteLine($"{word} vs {compareWith} : expected {exepectedSimilarity}");
            Assert.That(_comparer.SimilarityPercent(compareWith), Is.EqualTo(exepectedSimilarity));
        }
    }
}