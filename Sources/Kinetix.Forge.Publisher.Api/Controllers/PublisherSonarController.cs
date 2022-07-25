using System;
using System.Diagnostics;
using System.Web.Http;
using Kinetix.Forge.Publisher.Dto;
using Kinetix.Forge.Publisher.Providers.Sonar;
using Kinetix.Forge.Publisher.Publishers;
using Kinetix.Forge.Publisher.Reporters;
using Kinetix.Forge.Publisher.Resolvers;
using Kinetix.Forge.Publisher.Resolvers.Sonar;
using Kinetix.Forge.Publisher.Resolvers.Tfs;
using Kinetix.Forge.Publisher.Users;

namespace Kinetix.Forge.Publisher.Api.Controllers
{
    public class PublisherSonarController : ApiController
    {
        public string Post([FromBody] PublisherConfig config)
        {
            // TODO asynchrone

            LogUtils.Info("*** Kinetix.Forge.Publisher ***");
            LogUtils.Info();
            Stopwatch watch = Stopwatch.StartNew();
            try
            {
                var userManager = new UserManager(config.Ldap, config.Team);
                new Starter(
                    new SonarProvider(config.Sonar),
                    GetResolver(config),
                    new MailReporter(userManager, config.PerUserMaxIssueCount ?? 30),
                    new MailPublisher(userManager, config.MailPublisher),
                    config.ProjectName)
                    .Start();
            }
            catch (Exception e)
            {
                LogUtils.Info("Erreur non gérée : ");
                LogUtils.Info(e.ToString());
            }
            watch.Stop();

            LogUtils.Info();
            LogUtils.Info($"Exécution terminée en {watch.Elapsed.TotalSeconds:0}s.");

            return "OK";
        }

        private static IAuthorResolver GetResolver(PublisherConfig config)
        {
            switch (config.Resolver)
            {
                case "TFS":
                    return new TfsResolver(config.Tfs);
                case "Sonar":
                    return new SonarResolver();
                default:
                    throw new NotSupportedException("Le type de résolveur doit être renseigné à TFS ou Sonar.");
            }
        }
    }
}
