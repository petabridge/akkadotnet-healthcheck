using System;
using System.Collections.Generic;
using System.Text;
using Akka.Configuration;
using Akka.HealthCheck.Configuration;
using FluentAssertions;
using Xunit;

namespace Akka.HealthCheck.Tests.Configuration
{
    public class DefaultHealthCheckSettingsSpecs : TestKit.Xunit.TestKit
    {
        [Fact(DisplayName = "Should be able to load default Akka.HealthCheck HOCON")]
        public void Should_load_default_HealthCheck_HOCON()
        {
            HealthCheckSettings.DefaultConfig().Should().NotBe(Config.Empty);
        }
    }
}
