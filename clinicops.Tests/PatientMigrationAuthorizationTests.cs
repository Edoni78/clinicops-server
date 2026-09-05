using System.Reflection;
using ClinicOps.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ClinicOps.Tests.PatientMigration
{
    public class PatientMigrationAuthorizationTests
    {
        [Fact]
        public void ControllerRequiresClinicAdminRole()
        {
            var attr = typeof(PatientMigrationController)
                .GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(attr);
            Assert.Equal("ClinicAdmin", attr!.Roles);
        }

        [Fact]
        public void ControllerIsUnderApiRoute()
        {
            var route = typeof(PatientMigrationController)
                .GetCustomAttribute<RouteAttribute>();

            Assert.Equal("api/PatientMigration", route?.Template);
        }
    }
}
