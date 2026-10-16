namespace SFA.DAS.Payments.Monitoring.Tests.Specs.Support
{
    public static class TestData
    {
        private static readonly Random rand = new Random();

        public static readonly long Ukprn = rand.Next(100000, 200000);
        public static readonly long LearnerUln = rand.Next(10000000, 20000000);
        public static readonly long DasLearnerUln = LearnerUln + 1;  
        public const short AcademicYear = 2526;
        public const byte CollectionPeriod = 1;
        public const byte YearToDateCollectionPeriod = 2; 
    }
}
