using Kinetix.Forge.Publisher.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Kinetix.Forge.Publisher.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var userManager = new UserManager(new Dto.LdapConfig { DirectoryPath = "TODO" }, new Dto.TeamConfig
            {
                AdminUsers = new Dto.UserConfig[0],
                DefaultUsers = new Dto.UserConfig[1] { new Dto.UserConfig {
                    Email = "TODO"
                } },
                Forwarding = new Dto.UserConfig[0],
                StandardUsers = new Dto.UserConfig[0]
            }
            );

            var x = 1;
        }
    }
}
