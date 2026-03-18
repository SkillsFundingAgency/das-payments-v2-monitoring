using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SFA.DAS.Payments.Monitoring.Metrics.Application.Submission;

namespace SFA.DAS.Payments.Monitoring.Metrics.Function.UnitTests
{
    [TestFixture]
    public class GenerateSubmissionSummaryMetricsHttpTriggerTests
    {
        [TestCase("")]
        [TestCase("A")]
        [TestCase("1a")]
        public async Task Invalid_collection_period_is_rejected(string collectionPeriod)
        {
            // Arrange
            var request = new Mock<HttpRequest>();
            request.Setup(x => x.Query["collectionPeriod"]).Returns(collectionPeriod);
            var service = new Mock<ISubmissionMetricsService>();

            // Act
            var result = await GenerateSubmissionSummaryMetricsHttpTrigger.Run(request.Object, service.Object);

            // Assert
            result.Should().BeAssignableTo<BadRequestResult>();
        }

        [TestCase("")]
        [TestCase("A")]
        [TestCase("1A")]
        public async Task Invalid_academic_year_is_rejected(string academicYear)
        {
            // Arrange
            var request = new Mock<HttpRequest>();
            request.Setup(x => x.Query["collectionPeriod"]).Returns("1");
            request.Setup(x => x.Query["academicYear"]).Returns(academicYear);
            var service = new Mock<ISubmissionMetricsService>();

            // Act
            var result = await GenerateSubmissionSummaryMetricsHttpTrigger.Run(request.Object, service.Object);

            // Assert
            result.Should().BeAssignableTo<BadRequestResult>();
        }

        [TestCase("")]
        [TestCase("1000A")]
        [TestCase("A")]
        [TestCase("10001212,A")]
        [TestCase("10001212;10002020")]
        public async Task Invalid_UKPRNs_are_rejected(string ukprns)
        {
            // Arrange
            var request = new Mock<HttpRequest>();
            request.Setup(x => x.Query["collectionPeriod"]).Returns("1");
            request.Setup(x => x.Query["academicYear"]).Returns("2526");
            request.Setup(x => x.Query["ukprns"]).Returns(ukprns);
            var service = new Mock<ISubmissionMetricsService>();

            // Act
            var result = await GenerateSubmissionSummaryMetricsHttpTrigger.Run(request.Object, service.Object);

            // Assert
            result.Should().BeAssignableTo<BadRequestResult>();
        }

        [Test]
        public async Task Provider_that_has_no_successful_job_for_collection_period_is_rejected()
        {
            // Arrange
            long ukprn = 10001212;
            byte collectionPeriod = 1;
            short academicYear = 2526;
            var request = new Mock<HttpRequest>();
            request.Setup(x => x.Query["collectionPeriod"]).Returns(collectionPeriod.ToString());
            request.Setup(x => x.Query["academicYear"]).Returns(academicYear.ToString());
            request.Setup(x => x.Query["ukprns"]).Returns(ukprn.ToString());
            var service = new Mock<ISubmissionMetricsService>();
            service.Setup(x => x.GetLatestSuccessfulJobIdForProvider(ukprn, academicYear, collectionPeriod))
                .Throws(new ArgumentException("Job not found"));

            // Act
            var result = await GenerateSubmissionSummaryMetricsHttpTrigger.Run(request.Object, service.Object);

            // Assert
            result.Should().BeAssignableTo<BadRequestResult>();
        }

        [Test]
        public async Task Metrics_are_submitted_for_a_single_provider()
        {
            // Arrange
            long ukprn = 10001212;
            byte collectionPeriod = 1;
            short academicYear = 2526;
            long jobId = 1234;
            var request = new Mock<HttpRequest>();
            request.Setup(x => x.Query["collectionPeriod"]).Returns(collectionPeriod.ToString());
            request.Setup(x => x.Query["academicYear"]).Returns(academicYear.ToString());
            request.Setup(x => x.Query["ukprns"]).Returns(ukprn.ToString());
            var service = new Mock<ISubmissionMetricsService>();
            service.Setup(x => x.GetLatestSuccessfulJobIdForProvider(ukprn, academicYear, collectionPeriod))
                .ReturnsAsync(jobId);
            service.Setup(x => x.BuildMetrics(ukprn, jobId, academicYear, collectionPeriod,
                It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Act
            var result = await GenerateSubmissionSummaryMetricsHttpTrigger.Run(request.Object, service.Object);

            // Assert
            result.Should().BeAssignableTo<OkResult>();
            service.Verify(x => x.BuildMetrics(ukprn, jobId, academicYear, collectionPeriod,
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Test]
        public async Task Metrics_are_submitted_for_multiple_providers()
        {
            // Arrange
            var ukprns = new List<long>
            {
                10001234, 10002211, 10003232
            };
            var ukprnsQueryString = "10001234,10002211,10003232";
            byte collectionPeriod = 1;
            short academicYear = 2526;
            long jobId = 1234;
            var request = new Mock<HttpRequest>();
            request.Setup(x => x.Query["collectionPeriod"]).Returns(collectionPeriod.ToString());
            request.Setup(x => x.Query["academicYear"]).Returns(academicYear.ToString());
            request.Setup(x => x.Query["ukprns"]).Returns(ukprnsQueryString);
            var service = new Mock<ISubmissionMetricsService>();
            foreach (var ukprn in ukprns)
            {
                service.Setup(x => x.GetLatestSuccessfulJobIdForProvider(ukprn, academicYear, collectionPeriod))
                    .ReturnsAsync(jobId);
                service.Setup(x => x.BuildMetrics(ukprn, jobId, academicYear, collectionPeriod,
                    It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            }

            // Act
            var result = await GenerateSubmissionSummaryMetricsHttpTrigger.Run(request.Object, service.Object);

            // Assert
            result.Should().BeAssignableTo<OkResult>();
            foreach (var ukprn in ukprns)
            {
                service.Verify(x => x.BuildMetrics(ukprn, jobId, academicYear, collectionPeriod,
                    It.IsAny<CancellationToken>()), Times.Once());
            }
        }
    }
}
