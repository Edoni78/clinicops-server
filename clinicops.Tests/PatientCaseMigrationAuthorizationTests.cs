using System.Reflection;
using ClinicOps.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ClinicOps.Tests.PatientMigration
{
    public class PatientCaseMigrationAuthorizationTests
    {
        [Fact]
        public void ControllerRequiresClinicAdminRole()
        {
            var attr = typeof(PatientCaseMigrationController)
                .GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(attr);
            Assert.Equal("ClinicAdmin", attr!.Roles);
        }

        [Fact]
        public void ControllerIsUnderApiRoute()
        {
            var route = typeof(PatientCaseMigrationController)
                .GetCustomAttribute<RouteAttribute>();

            Assert.Equal("api/PatientCaseMigration", route?.Template);
        }
    }
}
