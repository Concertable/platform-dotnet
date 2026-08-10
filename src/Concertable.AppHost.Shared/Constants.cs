public static class AppHostConstants
{
    public static class Databases
    {
        public const string B2B = "B2BDb";
    }

    public static class ResourceNames
    {
        public const string B2BWeb = "b2b-web";
        public const string Workers = "workers";
        public const string B2BSeedingSimulator = "b2b-seeding-simulator";
    }

    public static class ServiceNames
    {
        private const string Prefix = "concertable-";

        public const string B2B = Prefix + "b2b";
    }
}
