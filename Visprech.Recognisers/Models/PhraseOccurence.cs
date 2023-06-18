namespace Visprech.Core.Models
{
    public class PhraseOccurence
    {
        public string Phrase { get; set; }
        public TimeSpan FoundAt { get; set; }
        public int Accuracy { get; set; }
        public string Message { get; set; }
    }
}
