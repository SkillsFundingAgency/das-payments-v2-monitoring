using Autofac;
using FluentAssertions;
using NUnit.Framework;
using SFA.DAS.Payments.Monitoring.Jobs.Client.Infrastructure.Ioc;

namespace SFA.DAS.Payments.Monitoring.Jobs.Client.UnitTests
{
    [TestFixture]
    public class JobStatusClientModuleTests
    {
        [Test]
        public void Should_register_IJobMessageClientFactory_As_Single_Instance()
        {
            // Arrange
            var builder = new ContainerBuilder();
            builder.RegisterModule<JobStatusClientModule>();
            var container = builder.Build();

            // Act
            var instance1 = container.Resolve<IJobMessageClientFactory>();
            var instance2 = container.Resolve<IJobMessageClientFactory>();

            // Assert
            instance1.Should().BeOfType<JobMessageClientFactory>();
            instance2.Should().BeSameAs(instance1);
        }
    }
}
