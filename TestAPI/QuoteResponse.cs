namespace TestAPI
{
    public class QuoteResponse
    {
        public int Count { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public int LastItemIndex { get; set; }
        public List<Quote> Results { get; set; }
    }
}
