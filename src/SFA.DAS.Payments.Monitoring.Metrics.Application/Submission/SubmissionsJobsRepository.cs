using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SFA.DAS.Payments.Application.Infrastructure.Logging;
using SFA.DAS.Payments.Application.Infrastructure.Telemetry;
using SFA.DAS.Payments.Model.Core.Entities;
using SFA.DAS.Payments.Monitoring.Metrics.Data;

namespace SFA.DAS.Payments.Monitoring.Metrics.Application.Submission
{
    public interface ISubmissionJobsRepository
    {
        Task<List<LatestSuccessfulJobModel>> GetLatestSuccessfulJobsForCollectionPeriod(short academicYear, byte collectionPeriod);
        Task<LatestSuccessfulJobModel> GetLatestSuccessfulJobForProvider(long jobId,long ukprn, short academicYear, byte collectionPeriod, Guid correlationId);
        Task<LatestSuccessfulJobModel> GetLatestCollectionPeriod();
    }

    public class SubmissionJobsRepository : InstrumentedMetricsRepository, ISubmissionJobsRepository
    {
        private readonly ISubmissionJobsDataContext dataContext;
        private readonly ITelemetry telemetry;
        private readonly IPaymentLogger logger;

        public SubmissionJobsRepository(ISubmissionJobsDataContext dataContext, ITelemetry telemetry, IPaymentLogger logger)
            : base(telemetry)
        {
            this.dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
            this.telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<LatestSuccessfulJobModel> GetLatestCollectionPeriod()
        {
            return await dataContext.LatestSuccessfulJobs
                .OrderByDescending(l => l.AcademicYear)
                .ThenByDescending(l => l.CollectionPeriod)
                .Take(1)
                .FirstOrDefaultAsync();
        }
        public async Task<List<LatestSuccessfulJobModel>> GetLatestSuccessfulJobsForCollectionPeriod(short academicYear, byte collectionPeriod)
        {
            return await dataContext.LatestSuccessfulJobs
                .Where(x => x.AcademicYear == academicYear &&
                            x.CollectionPeriod == collectionPeriod)
                .ToListAsync();
        }

        public async Task<LatestSuccessfulJobModel> GetLatestSuccessfulJobForProvider(long jobId, long ukprn, short academicYear, byte collectionPeriod, Guid correlationId)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                return await dataContext.LatestSuccessfulJobs
                    .Where(x =>
                        x.AcademicYear == academicYear &&
                        x.CollectionPeriod == collectionPeriod &&
                        x.Ukprn == ukprn)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                logger.LogError($"Error retrieving latest successful job for UKPRN {ukprn} academic year {academicYear} collection period {collectionPeriod} correlation ID {correlationId}", ex);
                throw;
            }
            finally
            {
                stopwatch.Stop();

                SendMetricsTelemetry("GetLatestSuccessfulJobForProvider", jobId, ukprn, correlationId, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
