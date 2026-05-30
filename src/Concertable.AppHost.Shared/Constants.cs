public static class AppHostConstants
{
    public static class Databases
    {
        public const string Auth = "AuthDb";
        public const string B2B = "B2BDb";
        public const string Customer = "CustomerDb";
        public const string Search = "SearchDb";
        public const string Payment = "PaymentDb";
    }

    public static class ResourceNames
    {
        public const string B2BWeb = "b2b-web";
        public const string CustomerWeb = "customer-web";
        public const string SearchWeb = "search-web";
        public const string SearchWorkers = "search-workers";
        public const string Auth = "auth";
        public const string PaymentWeb = "payment-web";
        public const string PaymentWorkers = "payment-workers";
        public const string Workers = "workers";
        public const string StripeCli = "stripe-cli";
        public const string B2BSeedingSimulator = "b2b-seeding-simulator";
    }

    public static class Ports
    {
        public const string B2BWeb = "https://localhost:7086";
        public const string CustomerWeb = "https://localhost:7090";
        public const string SearchWeb = "https://localhost:7087";
        public const string PaymentWeb = "https://localhost:7088";
        public const string Auth = "https://localhost:7083";
        public const string CustomerSpa = "https://localhost:5174";
        public const string VenueSpa = "https://localhost:5175";
        public const string ArtistSpa = "https://localhost:5176";
        public const string BusinessSpa = "https://localhost:5177";
    }
}
