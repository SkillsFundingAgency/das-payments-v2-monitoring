using System;
using Microsoft.EntityFrameworkCore;
using SFA.DAS.Payments.Monitoring.Metrics.Data;

namespace SFA.DAS.Payments.Monitoring.Metrics.Application.UnitTests
{
    public class InMemorySubmissionJobsDataContext : SubmissionJobsDataContext
    {
        private readonly string databaseName = Guid.NewGuid().ToString();

        public InMemorySubmissionJobsDataContext() :
            base(new DbContextOptionsBuilder().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase(databaseName);
        }
    }
}
