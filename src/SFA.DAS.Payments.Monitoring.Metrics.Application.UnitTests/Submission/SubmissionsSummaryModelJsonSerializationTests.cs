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

        [Test]
        public void Serialize_ExcludesSubmissionsSummaryFromDataLockMetricsTotals()
        {
            var summary = new SubmissionsSummaryModel();
            summary.DataLockMetricsTotals = new DataLockCountsTotalsModel { SubmissionsSummary = summary };

            var json = JsonSerializer.Serialize(summary);
            using var document = JsonDocument.Parse(json);

            var dataLockMetricsTotals = document.RootElement.GetProperty("DataLockMetricsTotals");

            dataLockMetricsTotals.TryGetProperty("SubmissionsSummary", out _).Should().BeFalse();
        }
    }
}
