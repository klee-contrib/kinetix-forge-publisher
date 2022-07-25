using System;
using System.Diagnostics;
using System.IO;
using Kinetix.Forge.Publisher.Dto;
using Kinetix.Forge.Publisher.Providers.Sonar;
using Kinetix.Forge.Publisher.Publishers;
using Kinetix.Forge.Publisher.Reporters;
using Kinetix.Forge.Publisher.Resolvers;
using Kinetix.Forge.Publisher.Resolvers.Sonar;
using Kinetix.Forge.Publisher.Resolvers.Tfs;
using Kinetix.Forge.Publisher.Users;
using Newtonsoft.Json;

namespace Kinetix.Forge.Publisher
{
    /// <summary>
    /// Programme de publication des warnings Sonar par mail, via les annotations TFS.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Entrée du programme.
        /// </summary>
        /// <param name="args">Arguments : chemin du fichier JSON de configuration.</param>
        public static void Main(string[] args)
        {
            LogUtils.Info("*** Kinetix.Forge.Publisher ***");
            LogUtils.Info();
            Stopwatch watch = Stopwatch.StartNew();
            try
            {
                var config = ReadConfig(args);
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

        private static PublisherConfig ReadConfig(string[] args)
        {
            /* Argument : chemin du fichier XML de configuration. */
            if (args.Length == 0)
            {
                PrintUsage();
                throw new ArgumentException("Un chemin de fichier de configuration JSON doit être fourni.");
            }

            return JsonConvert.DeserializeObject<PublisherConfig>(File.ReadAllText(args[0]));
        }

        private static void PrintUsage()
        {
            LogUtils.Info("Kinetix.Forge.Publisher.exe [configPath]");
            LogUtils.Info("   configPath  : Chemin du fichier JSON de configuration.");
        }
    }
}
