using System.Linq;
using Kinetix.Forge.Publisher.Providers;
using Kinetix.Forge.Publisher.Publishers;
using Kinetix.Forge.Publisher.Reporters;
using Kinetix.Forge.Publisher.Resolvers;

namespace Kinetix.Forge.Publisher
{
    public class Starter
    {
        private readonly string _projectName;
        private readonly IIssueProvider _provider;
        private readonly IAuthorResolver _resolver;
        private readonly MailReporter _reporter;
        private readonly MailPublisher _publisher;

        public Starter(IIssueProvider provider, IAuthorResolver resolver, MailReporter reporter, MailPublisher publisher, string projectName)
        {
            _projectName = projectName;
            _provider = provider;
            _resolver = resolver;
            _reporter = reporter;
            _publisher = publisher;
        }

        public void Start()
        {
            HandleIssues();
            HandleCoverage();
        }

        private void HandleIssues()
        {
            /* 1) Obtenir les issues de Sonar */
            LogUtils.Info($"Obtentions des issues de Sonar...");
            var issueList = _provider.GetIssues();
            LogUtils.Info($"Obtentions des issues de Sonar terminée.");
            if (!issueList.Any())
            {
                LogUtils.Info($"Aucune issue Sonar : fin de l'exécution.");
                return;
            }
            LogUtils.Info($"{issueList.Count} issues retournées.");
            LogUtils.Info();

            /* 2) Obtenir l'autheur des issues */
            LogUtils.Info($"Résolution des auteurs...");
            _resolver.ResolveAll(issueList);
            LogUtils.Info($"Résolution des auteurs terminée.");
            LogUtils.Info();

            // Imprime les issues
            foreach (var issue in issueList)
            {
                LogUtils.Info();
                LogUtils.Info($"Issue {issue.Rule}");
                LogUtils.Info($"{issue.FilePath} {issue.Line}");
                LogUtils.Info($"{issue.Message}");
                LogUtils.Info($"Auteur {issue.ResolvedAuthor}");
            }
            LogUtils.Info();

            /* 3) Agrége les issues dans des rapports individuels */
            LogUtils.Info($"Génération des rapports...");
            var reports = _reporter.CreateReports(_projectName, issueList);
            LogUtils.Info($"Génération des rapports terminée.");
            LogUtils.Info($"{reports.Count} rapports générés.");
            LogUtils.Info();

            /* 4) Construire le mail. */
            foreach (var report in reports)
            {
                LogUtils.Info();
                LogUtils.Info($"Rapport pour {report.MainRecepient.Name} ({report.MainRecepient.Email})");
                LogUtils.Info($"{report.FileReportList.Count} fichiers à traiter.");
            }
            LogUtils.Info();
            LogUtils.Info($"Génération des courriels...");
            _publisher.GenerateIssueMail(reports);
            LogUtils.Info($"Génération des courriels terminée.");
        }

        private void HandleCoverage()
        {
            /* 1) Obtenir le rapport de couverture de Sonar */
            LogUtils.Info($"Obtention du rapport de couverture de Sonar...");
            var report = _provider.GetCoverageReport();
            LogUtils.Info($"Obtention du rapport de couverture de Sonar terminé.");


            if (report.IsCoverageGreen)
            {
                LogUtils.Info($"Couverture dans le vert : pas de notification.");
                LogUtils.Info();
                return;
            }

            LogUtils.Info($"{report.NbError} tests en erreur.");
            LogUtils.Info($"{report.NbFailure} tests en échec.");
            LogUtils.Info();

            /* 2) Construire le mail. */
            LogUtils.Info();
            LogUtils.Info($"Génération du courriel...");
            report.ProjectName = _projectName;
            _publisher.GenerateMail(report);
            LogUtils.Info($"Génération du courriel terminé...");
        }
    }
}
