using Visprech.Core.Interfaces;

namespace Visprech.Infrastructure.PhraseComparers
{
    public class DiacriticsCleaner : IDiacriticsCleaner
    {
        public string RemoveDiacritics(string input) 
            => Diacritics.Extensions.StringExtensions
            .RemoveDiacritics(input);
    }
}
