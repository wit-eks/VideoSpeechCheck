using Microsoft.Extensions.Logging;
using Moq;
using Moq.AutoMock;
using Visprech.Core;
using Visprech.Core.Interfaces;
using Visprech.Core.Models;

namespace Visprech.Tests.Core
{
    public class TranscriptionProcessorTests
    {
        private AutoMocker _mocker;

        private TranscriptionProcessor? _tProcessor;
        private Mock<IConfiguration> _config;
        private Mock<IMessageWriter> _messageWriter;
        private string _subject;

        [SetUp]
        public void Setup()
        {
            var _mocker = new AutoMocker();

            _mocker.GetMock<IDiacriticsCleaner>()
                .Setup(m => m.RemoveDiacritics(It.IsAny<string>()))
                .Returns<string>(s => s);

            var pc = _mocker.GetMock<IPhraseComparer>();
            pc.Setup(m => m.SetSubject(It.IsAny<string>()))
                .Callback<string>(s => _subject = s);
            pc.Setup(m => m.IsSimilarTo(It.IsAny<string>()))
                .Returns<string>(compareWith => compareWith == _subject);
            pc.Setup(m => m.IsNotSimilarTo(It.IsAny<string>()))
                .Returns<string>(compareWith => compareWith != _subject);

            _config = _mocker.GetMock<IConfiguration>();
            _messageWriter = _mocker.GetMock<IMessageWriter>();

            _tProcessor = _mocker.CreateInstance<TranscriptionProcessor>();
        }

        [Test]
        public void Check__when_check_lists_are_empty__notifies_it()
        {
            SetupEmptySearchPhrases();

            _tProcessor.Check(CreateTranscriptFrom("abcq cdeq efgq"));

            CollectionAssert.IsEmpty(_tProcessor.OccurencesOfDesired);
            CollectionAssert.IsEmpty(_tProcessor.OccurencesOfProhibited);

            _messageWriter.Verify(m => m.WriteNotyfication(It.IsAny<string>()), Times.AtLeast(2));
            _messageWriter.Verify(m => m.WriteSuccess(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Check__when_only_desired_are_checked__shows_success_and_notify_missing_prohibited_list()
        {
            const string desired1 = "yes yes";
            const string desired2 = "this is ok";
            var desired = new List<string>() { desired1, desired2 };
            SetupSearchPhrases(goodWords: desired);

            _tProcessor.Check(CreateTranscriptFrom($"dummmyyy {desired1} dummmyyy word {desired2} or dummy"));

            CollectionAssert.IsEmpty(_tProcessor.OccurencesOfProhibited);
            CollectionAssert.IsNotEmpty(_tProcessor.OccurencesOfDesired);
            

            _messageWriter.Verify(m => m.WriteNotyfication(It.IsAny<string>()), Times.AtLeastOnce);
            _messageWriter.Verify(m => m.WriteSuccess(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void Check__when_only_prohibited_are_checked__shows_success_and_notify_missing_desired_list()
        {
            const string no1 = "onooooo";
            const string no2 = "correct this";
            var prohibited = new List<string>() { no1, no2 };
            SetupSearchPhrases(badWords: prohibited);

            _tProcessor.Check(CreateTranscriptFrom($"abcq cdeq sdfgsdfgb efgq"));

            CollectionAssert.IsEmpty(_tProcessor.OccurencesOfDesired);
            CollectionAssert.IsEmpty(_tProcessor.OccurencesOfProhibited);

            _messageWriter.Verify(m => m.WriteNotyfication(It.IsAny<string>()), Times.AtLeastOnce);
            _messageWriter.Verify(m => m.WriteSuccess(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void Check__finds_prohibited_words()
        {
            var prohibited = new List<string>() { "bad1", "bad2" };
            SetupSearchPhrases(badWords: prohibited);

            _tProcessor.Check(CreateTranscriptFrom("absdfgscq bad1 efsdfggq bad2"));

            var cought = GetPhrases(_tProcessor
                .OccurencesOfProhibited);

            CollectionAssert.AreEquivalent(prohibited, cought);
            CollectionAssert.IsEmpty(_tProcessor.OccurencesOfDesired);
        }

        [Test]
        public void Check__finds_desired_words()
        {
            var desired = new List<string>() { "good1", "goooood" };
            SetupSearchPhrases(null, desired);

            _tProcessor.Check(CreateTranscriptFrom("absdfgscq good1 efsdfggq goooood"));

            var cought = GetPhrases(_tProcessor
                .OccurencesOfDesired);

            CollectionAssert.AreEquivalent(desired, cought);
            CollectionAssert.IsEmpty(_tProcessor.OccurencesOfProhibited);
        }

        [TestCase("it's", "goood")]
        [TestCase("it's ok matey", "goood")]
        public void Check__finds_desired_words_with_apostrophe(string desired1, string desired2)
        {
            var desired = new List<string>() { desired1, desired2 };
            SetupSearchPhrases(goodWords: desired);

            _tProcessor.Check(CreateTranscriptFrom($"dummmyyy {desired1} dummmyyy word or {desired2}"));

            var cought = GetPhrases(_tProcessor
                .OccurencesOfDesired);

            CollectionAssert.AreEquivalent(desired, cought);
            CollectionAssert.IsEmpty(_tProcessor.OccurencesOfProhibited);
        }

        [TestCase("oh yes")]
        [TestCase("it would be good to have this")]
        public void Check__finds_desired_phrase_composed_from_many_wards(string desiredWord)
        {
            var desired = new List<string>() { desiredWord };
            SetupSearchPhrases(goodWords: desired);

            _tProcessor.Check(CreateTranscriptFrom($"dummmyyy dummmyyy word or {desiredWord} dummmyyy"));

            var cought = GetPhrases(_tProcessor
                .OccurencesOfDesired);

            CollectionAssert.AreEquivalent(desired, cought);
            CollectionAssert.IsEmpty(_tProcessor.OccurencesOfProhibited);
        }

        [TestCase("oh yes")]
        [TestCase("it would be good to have this")]
        public void Check__finds_desired_phrase_composed_from_many_words_at_the_end(string desiredWord)
        {
            var desired = new List<string>() { desiredWord };
            SetupSearchPhrases(goodWords: desired);

            _tProcessor.Check(CreateTranscriptFrom($"dummmyyy dummmyyy word or {desiredWord}"));

            var cought = GetPhrases(_tProcessor
                .OccurencesOfDesired);

            CollectionAssert.AreEquivalent(desired, cought);
            CollectionAssert.IsEmpty(_tProcessor.OccurencesOfProhibited);
        }

        [TestCase("oh nooo")]
        [TestCase("it would not be good to have this")]
        public void Check__finds_prohibited_phrase_composed_from_many_wards(string prohibitedWord)
        {
            var prohibited = new List<string>() { prohibitedWord };
            SetupSearchPhrases(badWords: prohibited);

            _tProcessor.Check(CreateTranscriptFrom($"dummmyyy dummmyyy word or {prohibitedWord} dummmyyy"));

            var cought = GetPhrases(_tProcessor
                .OccurencesOfProhibited);

            CollectionAssert.AreEquivalent(prohibited, cought);
            CollectionAssert.IsEmpty(_tProcessor.OccurencesOfDesired);
        }

        [TestCase("oh nooo")]
        [TestCase("it would not be good to have this")]
        public void Check__finds_prohibited_phrase_composed_from_many_words_at_the_end(string prohibitedWord)
        {
            var prohibited = new List<string>() { prohibitedWord };
            SetupSearchPhrases(badWords: prohibited);

            _tProcessor.Check(CreateTranscriptFrom($"dummmyyy dummmyyy word or {prohibitedWord}"));

            var cought = GetPhrases(_tProcessor
                .OccurencesOfProhibited);

            CollectionAssert.AreEquivalent(prohibited, cought);
            CollectionAssert.IsEmpty(_tProcessor.OccurencesOfDesired);
        }

        [Test]
        public void Check_notification__when_no_prohobited_phrases_exist__shows_success()
        {
            const string no1 = "onooooo";
            const string no2 = "correct this";
            var prohibited = new List<string>() { no1, no2 };
            SetupSearchPhrases(badWords: prohibited);

            _tProcessor.Check(CreateTranscriptFrom($"dummmyyy dummmyyy word or dummy"));

            CollectionAssert.IsEmpty(_tProcessor.OccurencesOfProhibited);
            CollectionAssert.IsEmpty(_tProcessor.OccurencesOfDesired);

            _messageWriter.Verify(m => m.WriteSuccess(It.IsAny<string>()), Times.AtLeastOnce);
            _messageWriter.Verify(m => m.WriteFailure(It.IsAny<string>()), Times.Never);
            _messageWriter.Verify(m => m.WriteWarn(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Check_notification__when_one_prohobited_phrases_exist__shows_failure()
        {
            const string no1 = "onooooo";
            const string no2 = "correct this";
            var prohibited = new List<string>() { no1, no2 };
            SetupSearchPhrases(badWords: prohibited);

            _tProcessor.Check(CreateTranscriptFrom($"dummmyyy dummmyyy {no1} word or dummy"));

            CollectionAssert.IsNotEmpty(_tProcessor.OccurencesOfProhibited);

            _messageWriter.Verify(m => m.WriteSuccess(It.IsAny<string>()), Times.Never);
            _messageWriter.Verify(m => m.WriteFailure(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Test]
        public void Check_notification__when_all_prohobited_phrases_exist__shows_failure()
        {
            const string no1 = "onooooo";
            const string no2 = "correct this";
            var prohibited = new List<string>() { no1, no2 };
            SetupSearchPhrases(badWords: prohibited);

            _tProcessor.Check(CreateTranscriptFrom($"dummmyyy dummmyyy {no1} word {no2} or dummy"));

            CollectionAssert.IsNotEmpty(_tProcessor.OccurencesOfProhibited);

            _messageWriter.Verify(m => m.WriteSuccess(It.IsAny<string>()), Times.Never);
            _messageWriter.Verify(m => m.WriteFailure(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Test]
        public void Check_notyfication__when_all_desired_phrases_exist__shows_success()
        {
            const string desired1 = "yes yes";
            const string desired2 = "this is ok";
            var desired = new List<string>() { desired1, desired2 };
            SetupSearchPhrases(goodWords: desired);

            _tProcessor.Check(CreateTranscriptFrom($"dummmyyy {desired1} dummmyyy word {desired2} or dummy"));

            CollectionAssert.IsEmpty(_tProcessor.OccurencesOfProhibited);

            _messageWriter.Verify(m => m.WriteSuccess(It.IsAny<string>()), Times.AtLeastOnce);
            _messageWriter.Verify(m => m.WriteFailure(It.IsAny<string>()), Times.Never);
            _messageWriter.Verify(m => m.WriteWarn(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Check_notification__when_one_of_desired_phrases_is_missing__shows_failure_and_warning()
        {
            const string desired1 = "yes yes";
            const string desired2 = "this is ok";
            var desired = new List<string>() { desired1, desired2 };
            SetupSearchPhrases(goodWords: desired);

            _tProcessor.Check(CreateTranscriptFrom($"dummmyyy {desired1} dummmyyy word or dummy"));

            var cought = GetPhrases(_tProcessor
                .OccurencesOfProhibited);

            CollectionAssert.IsEmpty(_tProcessor.OccurencesOfProhibited);

            _messageWriter.Verify(m => m.WriteSuccess(It.IsAny<string>()), Times.Never);
            _messageWriter.Verify(m => m.WriteFailure(It.IsAny<string>()), Times.AtLeastOnce);

        }

        [Test]
        public void Check_notification__when_no_desired_phrases_exist__shows_failure()
        {
            const string desired1 = "yes yes";
            const string desired2 = "this is ok";
            var desired = new List<string>() { desired1, desired2 };
            SetupSearchPhrases(goodWords: desired);

            _tProcessor.Check(CreateTranscriptFrom($"dummmyyy dummmyyy word or dummy"));

            var cought = GetPhrases(_tProcessor
                .OccurencesOfProhibited);

            CollectionAssert.IsEmpty(_tProcessor.OccurencesOfProhibited);

            _messageWriter.Verify(m => m.WriteSuccess(It.IsAny<string>()), Times.Never);
            _messageWriter.Verify(m => m.WriteFailure(It.IsAny<string>()), Times.AtLeastOnce);
        }

        private static List<(TimeSpan from, TimeSpan to, string text)>
            CreateTranscriptFrom(string text)
        {
            return new List<(TimeSpan from, TimeSpan to, string text)>
            {
                { (TimeSpan.Zero, TimeSpan.FromSeconds(10), text) }
            };
        }


        private void SetupEmptySearchPhrases()
        {
            SetupSearchPhrases(null);
        }

        private void SetupSearchPhrases(List<string> badWords = null, List<string> goodWords = null)
        {
            _config
               .SetupGet(m => m.ProhibitedPhrases)
               .Returns(badWords ?? new List<string>());
            _config
               .SetupGet(m => m.DesiredPhrases)
               .Returns(goodWords ?? new List<string>());
        }

        private List<string> GetPhrases(IEnumerable<PhraseOccurence> phrases) 
            => phrases.Select(p => p.Phrase).ToList();

    }
}
