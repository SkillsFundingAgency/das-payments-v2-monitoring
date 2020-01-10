﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Moq;
using NUnit.Framework;
using SFA.DAS.Payments.Monitoring.Metrics.Application.Submission;
using SFA.DAS.Payments.Monitoring.Metrics.Data;
using SFA.DAS.Payments.Monitoring.Metrics.Domain.Submission;
using SFA.DAS.Payments.Monitoring.Metrics.Model;
using SFA.DAS.Payments.Monitoring.Metrics.Model.Submission;

namespace SFA.DAS.Payments.Monitoring.Metrics.Application.UnitTests.Submission
{
    [TestFixture]
    public class SubmissionMetricsServiceTests
    {
        private Autofac.Extras.Moq.AutoMock moqer;
        private List<TransactionTypeAmounts> dcEarnings;
        private List<TransactionTypeAmounts> dasEarnings;
        private List<TransactionTypeAmounts> requiredPayments;
        private DataLockTypeAmounts dataLocks;
        private decimal totalDataLockedEarnings;

        [SetUp]
        public void SetUp()
        {
            moqer = AutoMock.GetLoose();
            dcEarnings = TestsHelper.DefaultDcEarnings;
            dasEarnings = TestsHelper.DefaultDasEarnings;
            dataLocks = TestsHelper.DefaultDataLockedEarnings;
            requiredPayments = TestsHelper.DefaultRequiredPayments;
            totalDataLockedEarnings = 3000;
            moqer.Mock<IDcMetricsDataContext>()
                .Setup(ctx => ctx.GetEarnings(It.IsAny<long>(), It.IsAny<short>(), It.IsAny<byte>()))
                .ReturnsAsync(dcEarnings);
            var mockSubmissionSummary = moqer.Mock<ISubmissionSummary>();
            moqer.Mock<ISubmissionSummaryFactory>()
                .Setup(factory =>
                    factory.Create(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<short>(), It.IsAny<byte>()))
                .Returns(mockSubmissionSummary.Object);
            moqer.Mock<ISubmissionMetricsRepository>()
                .Setup(repo => repo.GetDasEarnings(It.IsAny<long>(), It.IsAny<long>()))
                .ReturnsAsync(dasEarnings);
            moqer.Mock<ISubmissionMetricsRepository>()
                .Setup(repo => repo.GetDasEarnings(It.IsAny<long>(), It.IsAny<long>()))
                .ReturnsAsync(dasEarnings);
            moqer.Mock<ISubmissionMetricsRepository>()
                .Setup(repo => repo.GetDataLockedEarnings(It.IsAny<long>(), It.IsAny<long>()))
                .ReturnsAsync(dataLocks);
            moqer.Mock<ISubmissionMetricsRepository>()
                .Setup(repo => repo.GetDataLockedEarningsTotal(It.IsAny<long>(), It.IsAny<long>()))
                .ReturnsAsync(TestsHelper.DefaultDataLockedTotal);
            moqer.Mock<ISubmissionMetricsRepository>()
                .Setup(repo => repo.GetRequiredPayments(It.IsAny<long>(), It.IsAny<long>()))
                .ReturnsAsync(requiredPayments);
            moqer.Mock<ISubmissionMetricsRepository>()
                .Setup(repo => repo.GetHeldBackCompletionPaymentsTotal(It.IsAny<long>(), It.IsAny<long>()))
                .ReturnsAsync(TestsHelper.DefaultHeldBackCompletionPayments);
            moqer.Mock<ISubmissionMetricsRepository>()
                .Setup(repo => repo.GetYearToDatePaymentsTotal(It.IsAny<long>(), It.IsAny<short>(), It.IsAny<byte>()))
                .ReturnsAsync(TestsHelper.DefaultYearToDateAmounts);
        }

        [Test]
        public async Task Includes_Earnings_In_Metrics()
        {
            var service = moqer.Create<SubmissionMetricsService>();
            await service.BuildMetrics(1234, 123, 1920, 1, CancellationToken.None).ConfigureAwait(false);

            moqer.Mock<ISubmissionSummary>()
                .Verify(x => x.AddEarnings(It.Is<List<TransactionTypeAmounts>>(lst => lst == dcEarnings),
                    It.Is<List<TransactionTypeAmounts>>(lst => lst == dasEarnings)), Times.Once);
        }

        [Test]
        public async Task Includes_DataLocks_In_Metrics()
        {
            var service = moqer.Create<SubmissionMetricsService>();
            await service.BuildMetrics(1234, 123, 1920, 1, CancellationToken.None).ConfigureAwait(false);

            moqer.Mock<ISubmissionSummary>()
                .Verify(x => x.AddDataLockedEarnings(It.Is<decimal>(total => total == totalDataLockedEarnings), It.Is<DataLockTypeAmounts>(amounts => amounts == dataLocks)), Times.Once);

        }

        [Test]
        public async Task Includes_Required_Payments_In_Metrics()
        {
            var service = moqer.Create<SubmissionMetricsService>();
            await service.BuildMetrics(1234, 123, 1920, 1, CancellationToken.None).ConfigureAwait(false);

            moqer.Mock<ISubmissionSummary>()
                .Verify(x => x.AddRequiredPayments(It.Is<List<TransactionTypeAmounts>>(amounts => amounts == requiredPayments)), Times.Once);

        }

        [Test]
        public async Task Includes_Held_Back_Completion_Payments_In_Metrics()
        {
            var service = moqer.Create<SubmissionMetricsService>();
            await service.BuildMetrics(1234, 123, 1920, 1, CancellationToken.None).ConfigureAwait(false);

            moqer.Mock<ISubmissionSummary>()
                .Verify(x => x.AddHeldBackCompletionPayments(It.Is<ContractTypeAmounts>(amounts => amounts.ContractType1 == TestsHelper.DefaultHeldBackCompletionPayments.ContractType1 &&
                                                                                                   amounts.ContractType2 == TestsHelper.DefaultHeldBackCompletionPayments.ContractType2)), Times.Once);
        }


        [Test]
        public async Task Includes_Year_To_Date_Payments_In_Metrics()
        {
            var service = moqer.Create<SubmissionMetricsService>();
            await service.BuildMetrics(1234, 123, 1920, 1, CancellationToken.None).ConfigureAwait(false);

            moqer.Mock<ISubmissionSummary>()
                .Verify(x => x.AddYearToDatePaymentTotals(It.Is<ContractTypeAmounts>(amounts => amounts.ContractType1 == TestsHelper.DefaultYearToDateAmounts.ContractType1 &&
                                                                                                   amounts.ContractType2 == TestsHelper.DefaultYearToDateAmounts.ContractType2)), Times.Once);
        }

        [Test]
        public async Task Saves_Submission_Metrics()
        {
            var service = moqer.Create<SubmissionMetricsService>();
            await service.BuildMetrics(1234, 123, 1920, 1, CancellationToken.None).ConfigureAwait(false);

            moqer.Mock<ISubmissionSummary>()
                .Verify(x => x.GetMetrics(), Times.Once);
            moqer.Mock<ISubmissionMetricsRepository>()
                .Verify(repo => repo.SaveSubmissionMetrics(It.IsAny<SubmissionSummaryModel>(), It.IsAny<CancellationToken>()), Times.Once);

        }
    }
}