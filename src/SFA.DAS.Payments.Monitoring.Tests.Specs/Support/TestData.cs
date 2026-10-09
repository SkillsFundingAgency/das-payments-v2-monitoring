namespace SFA.DAS.Payments.Monitoring.Tests.Specs.Support
{
    public static class TestData
    {
        private static readonly Random rand = new Random();

        public static readonly long Ukprn = rand.Next(100000, 200000);
        public static readonly long LearnerUln = rand.Next(10000000, 20000000);
        public static readonly long DasLearnerUln = LearnerUln + 1;  // Ensure that the DAS learner ULN is different from the regular learner ULN
        //Needed for the GetInLearningCount method as the sql query selects by ukprn and learneruln,
        //so we need to make sure that the daslearneruln is different from the learneruln to avoid conflicts in the test data
        public const short AcademicYear = 2526;
        public const byte CollectionPeriod = 1;
        public const byte YearToDateCollectionPeriod = 2; //GetYearToDatePayments looks for payments from earlier periods,
                                                          //so 2 is being set as the reporting period so 1 falls within the year to date range
    }
}
