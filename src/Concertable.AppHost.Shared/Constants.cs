public static class AppHostConstants
{
    public static class Databases
    {
        public const string Auth = "AuthDb";
        public const string B2B = "B2BDb";
        public const string Payment = "PaymentDb";
    }

    public static class ResourceNames
    {
        public const string B2BWeb = "b2b-web";
        public const string Auth = "auth";
        public const string PaymentWeb = "payment-web";
        public const string PaymentWorkers = "payment-workers";
        public const string Workers = "workers";
        public const string StripeCli = "stripe-cli";
        public const string B2BSeedingSimulator = "b2b-seeding-simulator";
    }

    public static class ServiceNames
    {
        private const string Prefix = "concertable-";

        public const string Auth = Prefix + "auth";
        public const string B2B = Prefix + "b2b";
        public const string Payment = Prefix + "payment";
    }
}
