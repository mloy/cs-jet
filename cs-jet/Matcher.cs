namespace cs_jet
{
    public class Matcher
    {
        public string contains { get; set; }
        public string startsWith { get; set; }
        public string endsWith { get; set; }
        public string equals { get; set; }
        public string equalsNot { get; set; }
        public bool caseInsensitive { get; set; }
    }
}
