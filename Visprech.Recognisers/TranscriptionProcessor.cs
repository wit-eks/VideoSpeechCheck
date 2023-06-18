using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Visprech.Core.Interfaces;
using Visprech.Core.Models;

namespace Visprech.Core
{
    public class TranscriptionProcessor
    {
        private readonly static Regex _wordsRgx = new(@"[a-zA-Z-%0-9']+");

        private readonly List<(string word, TimeSpan positionFrom, TimeSpan positionTo)> _words = new();

        private List<PhraseOccurence> _occurencesOfProhibited = new();
        private List<PhraseOccurence> _occurencesOfDesired = new();

        private readonly bool _showDetails;
        private ReadOnlyCollection<string> _badWords;
        private ReadOnlyCollection<string> _goodWords;
        private readonly IConfiguration _configuration;
        private readonly IMessageWriter _messageWriter;
        private readonly IPhraseComparer _phraseComparer;
        private readonly IDiacriticsCleaner _diacriticsCleaner;
        private readonly ITranscriptionResultHandler _transcriptionResultHandler;

        private readonly ILogger<TranscriptionProcessor> _logger;

        public TranscriptionProcessor(
            IConfiguration configuration,
            IMessageWriter messageWriter,
            IPhraseComparer phraseComparer,
            IDiacriticsCleaner diacriticsCleaner,
            ITranscriptionResultHandler transcriptionResultHandler,
            ILogger<TranscriptionProcessor> logger)
        {
            _showDetails = configuration.ShowDetailsInReport;
            _configuration = configuration;
            _messageWriter = messageWriter;
            _phraseComparer = phraseComparer;
            _diacriticsCleaner = diacriticsCleaner;
            _transcriptionResultHandler = transcriptionResultHandler;
            _logger = logger;
        }

        private ReadOnlyCollection<string> NormalizePhrases(ReadOnlyCollection<string> stringsToFind)
        {
            return stringsToFind
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => _diacriticsCleaner.RemoveDiacritics(s.ToLower()).Trim())
                .ToList()
                .AsReadOnly();
        }

        private void LoadWords(List<(TimeSpan from, TimeSpan to, string text)> trans)
        {
            _words.Clear();
            
            foreach (var t in trans)
            {
                var formated = GetSentence(t.text);

                _words.AddRange(formated.Split(' ').Select(w => (w, t.from, t.to)));
            }
        }        

        private string GetSentence(string words)
        {
            var stripped = _diacriticsCleaner
                .RemoveDiacritics(words.ToLower())
                .Trim();
            return string.Join(" ", _wordsRgx.Matches(stripped).Select(_ => _.Value));           
        }

        private void ListTranscription(List<(TimeSpan from, TimeSpan to, string text)> transcription)
        {
            _messageWriter.WriteEmptyLine();
            _messageWriter.WriteNotyfication("First 15 lines of transcript");
            foreach (var t in transcription.Take(15)) 
            {
                var line = _transcriptionResultHandler
                    .ReadableTranscriptionLine(t.from, t.to, t.text);
                _messageWriter.Write(line);
            }
        }

        private void AssertThatDoNotInclude(ReadOnlyCollection<string> badWords, bool showDetails = false)
        {
            _messageWriter.Write(string.Empty);
            WriteDoubleLine();
            _messageWriter.Write("Checking lack of existence of prohibited phrases:");

            if (!_words.Any())
            {
                _messageWriter.WriteInternalError("Looks like transcription is empty");
                WriteSingleLine();
                return;
            }

            if (!badWords.Any())
            {
                _messageWriter.WriteNotyfication("The list of prohibited list is empty.");
                WriteSingleLine();
                return;
            }

            List<(string sentence, TimeSpan foundAt, int accuracy, string message)> occurences = new();

            foreach (var bw in badWords)
            {
                occurences.AddRange(FindSentence(GetSentence(bw))); //
            }

            List<string> notFound =
                badWords.Select(s => GetSentence(s))
                .Except(occurences.Select(o => o.sentence))
                .ToList();            

            if (occurences.Any())
            {
                _occurencesOfProhibited = occurences
                    .Select(o => new PhraseOccurence
                    {
                        Phrase = o.sentence,
                        Accuracy= o.accuracy,
                        FoundAt= o.foundAt,
                        Message = o.message 
                    }).ToList();

                _messageWriter.WriteFailure("WARNING: Found occurrences of not expected phrases");
                var g = occurences
                       .GroupBy(o => o.sentence)
                       .OrderBy(g => g.Min(_ => _.foundAt))
                       .ThenBy(g => g.Key);


                if (!showDetails)
                {
                    g.Select(g => $"{g.Key} - occurred {g.Count()} times.")
                       .ToList()
                       .ForEach(m => _messageWriter.Write(m));
                }
                else
                {
                    g.Select(g => $"{g.Key} - occurred {g.Count()} times.{Environment.NewLine}{getDetails(g)}")
                     .ToList()
                     .ForEach(m => _messageWriter.Write(m));
                }

                if (notFound.Any())
                {
                    _messageWriter.WriteWarn("WARNING: Only some prohibited phrases do not exist");
                    notFound
                        .OrderBy(_ => _)
                        .ToList()
                        .ForEach(o => _messageWriter.Write($"\t{o}"));
                }

            }
            else
            {
                _messageWriter.WriteSuccess("SUCCESS: None of the prohibited phrases has been found");
                notFound
                    .OrderBy(_ => _)
                    .ToList()
                    .ForEach(o => _messageWriter.Write($"\t{o}"));
            }

            WriteSingleLine();
            
            return;

            string getDetails(IGrouping<string, (string sentence, TimeSpan foundAt, int accuracy, string message)> group)
            {
                return string.Join(Environment.NewLine, group
                    .OrderByDescending(g => g.accuracy)
                    .ThenBy(g => g.foundAt)
                    .Select(g => $"\t{g.foundAt.ToString(@"hh\:mm\:ss")} {g.message}"));
            }

        }



        private IEnumerable<(string sentence, TimeSpan foundAt, int accuracy, string message)> 
            FindSentence(string sentence)
        {
            var res = new List<(string sentence, TimeSpan foundAt, int accuracy, string message)>();

            if (sentence.Length <= 3)
            {
                _messageWriter.WriteWarn($"WARNING: Word is too short {sentence}. It will not be checked");
                return res;
            }
            
            var howManyWordsInSentence = sentence.Split(' ').Count();
            _phraseComparer.SetSubject(sentence);
            
            for (int i = 0; i < _words.Count() - (howManyWordsInSentence - 1); i++)
            {
                string compareWith = 
                    string.Join(" ", _words
                        .Skip(i)
                        .Take(howManyWordsInSentence)
                        .Select(w => w.word));
                
                if (_phraseComparer.IsNotSimilarTo(compareWith)) continue;
                
                var similarity = _phraseComparer.SimilarityPercent(compareWith);
                res.Add((sentence, _words[i].positionFrom, similarity, $"Phrase >{sentence}< is similar to >{compareWith}< in {similarity}%"));
            }

            return res;
        }

        private void AssertThatInclude(ReadOnlyCollection<string> goodWords, bool showDetails= false)
        {
            _messageWriter.Write(string.Empty);
            WriteDoubleLine();
            _messageWriter.Write("Checking existence of desired phrases:");

            if (!_words.Any())
            {
                _messageWriter.WriteInternalError("Looks like transcription is empty");
                WriteSingleLine();
                return;
            }

            if (!goodWords.Any())
            {
                _messageWriter.WriteNotyfication("The list of desired list is empty.");
                WriteSingleLine();
                return;
            }

            List<(string sentence, TimeSpan foundAt, int accuracy, string message)> occurences = new();

            foreach (var bw in goodWords)
            {
                occurences.AddRange(FindSentence(GetSentence(bw))); //
            }

            var missing = goodWords
                .Select(w => GetSentence(w))
                .Except(occurences.Select(o => o.sentence).Distinct());

            if (missing.Any())
            {
                _messageWriter.WriteFailure("WARNING: Desired phrases missing:");
                missing
                    .OrderBy(_ => _)
                    .ToList()                    
                    .ForEach(o => _messageWriter.Write($"\t{o}"));
            }

            if (occurences.Any()) 
            {
                _occurencesOfDesired = occurences
                    .Select(o => new PhraseOccurence
                    {
                        Phrase = o.sentence,
                        Accuracy = o.accuracy,
                        FoundAt = o.foundAt,
                        Message = o.message
                    }).ToList();

                if (missing.Any()) 
                { 
                    _messageWriter.WriteWarn("WARNING: Only some desired phrases have been found"); 
                }
                else
                {
                    _messageWriter.WriteSuccess("SUCCESS: Found occurrences of all desired phrases");
                }
                
                var g = occurences
                       .GroupBy(o => o.sentence)
                       .OrderBy(g => g.Min(_ => _.foundAt))
                       .ThenBy(g => g.Key);


                if (!showDetails)
                {
                    g.Select(g => $"{g.Key} - occurred {g.Count()} times.")
                       .ToList()
                       .ForEach(m => _messageWriter.Write(m));
                }
                else
                {
                    g.Select(g => $"{g.Key} - occurred {g.Count()} times.{Environment.NewLine}{getDetails(g)}")
                     .ToList()
                     .ForEach(m => _messageWriter.Write(m));
                }
            }

            WriteSingleLine();

            return;

            string getDetails(IGrouping<string, (string sentence, TimeSpan foundAt, int accuracy, string message)> group)
            {
                return string.Join(Environment.NewLine, group
                    .OrderByDescending(g => g.accuracy)
                    .ThenBy(g => g.foundAt)
                    .Select(g => $"\t{g.foundAt.ToString(@"hh\:mm\:ss")} {g.message}"));
            }
        }


        private void WriteDoubleLine()
        {
            _messageWriter.Write("====================================================");
        }
        private void WriteSingleLine()
        {
            _messageWriter.Write("----------------------------------------------------");
        }

        public void Check(List<(TimeSpan from, TimeSpan to, string text)> transcription)
        {
            try
            {
                _logger.LogInformation("Transcript check started.");

                _badWords = NormalizePhrases(_configuration.ProhibitedPhrases.AsReadOnly());
                _goodWords = NormalizePhrases(_configuration.DesiredPhrases.AsReadOnly());
                _occurencesOfDesired.Clear();
                _occurencesOfProhibited.Clear();

                LoadWords(transcription);

                ListTranscription(transcription);

                _logger.LogInformation("Asserting that there are desired phrases.");
                AssertThatInclude(_goodWords, showDetails: _showDetails);
                
                _logger.LogInformation("Asserting that there is no bad phrases.");
                AssertThatDoNotInclude(_badWords, showDetails: _showDetails);

                _logger.LogInformation("Transcript check finished.");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error when checking transcript file");
                throw;
            }
        }

        public ReadOnlyCollection<PhraseOccurence> OccurencesOfProhibited
            => _occurencesOfProhibited.AsReadOnly();
        public ReadOnlyCollection<PhraseOccurence> OccurencesOfDesired
            => _occurencesOfDesired.AsReadOnly();
    }
}