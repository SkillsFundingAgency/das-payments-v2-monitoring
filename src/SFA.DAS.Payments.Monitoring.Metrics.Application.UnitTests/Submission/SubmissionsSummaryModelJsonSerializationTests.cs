using System;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SFA.DAS.Payments.Monitoring.Metrics.Model.Submission;

namespace SFA.DAS.Payments.Monitoring.Metrics.Application.UnitTests.Submission
{
    [TestFixture]
    public class SubmissionsSummaryModelJsonSerializationTests
    {
        [Test]
        public void SerializesWithoutThrowing_WhenDataLockMetricsTotalsHasBackReference()
        {
            var summary = new SubmissionsSummaryModel();
            summary.DataLockMetricsTotals = new DataLockCountsTotalsModel { SubmissionsSummary = summary };

            Action act = () => JsonSerializer.Serialize(summary);

            act.Should().NotThrow<JsonException>();
        }
    }
}
