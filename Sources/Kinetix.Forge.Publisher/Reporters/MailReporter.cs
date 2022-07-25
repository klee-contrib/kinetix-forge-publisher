using System;
using System.Collections.Generic;
using System.Linq;
using Kinetix.Forge.Publisher.Dto;
using Kinetix.Forge.Publisher.Users;

namespace Kinetix.Forge.Publisher.Reporters
{
    /// <summary>
    /// Créé des rapports à envoyer par mail.
    /// </summary>
    public class MailReporter
    {
        private readonly UserManager _userManager;
        private readonly int _perUserMaxIssueCount;

        public MailReporter(UserManager userManager, int perUserMaxIssueCount)
        {
            _userManager = userManager;
            _perUserMaxIssueCount = perUserMaxIssueCount;
        }

        /// <summary>
        /// Agrège les issues dans des rapports.
        /// </summary>
        /// <param name="projetName">Nom du projet.</param>
        /// <param name="issues">Liste d'issues.</param>
        /// <returns>Liste de rapport.</returns>
        public ICollection<Report> CreateReports(string projetName, ICollection<IssueItem> issues)
        {
            /* Regroupe par utilisateur */
            var map = issues
                .GroupBy(x => x.ResolvedAuthor.ToString() ?? string.Empty)
                .ToDictionary(x => x.Key, x => x.ToList());

            /* Obtient email / gérer les mapping utilisateurs... */
            var reports = new List<Report>();
            foreach (var key in map.Keys)
            {
                /* Construit un rapport. */
                var report = new Report { ProjectName = projetName };
                var userIssues = map[key];
                report.WarningCount = userIssues.Count; // TODO

                /* Retrouver les destinataires à partir des infos sur l'utilisateur.. */
                var list = _userManager.RetrieveUserListByCriteria(userIssues.First().ResolvedAuthor);
                foreach (var userInfo in list)
                {
                    report.RecepientList.Add(userInfo);
                    LogUtils.Info($"User {userInfo.Email}");
                }

                /* Destinataire principal. */
                report.MainRecepient = report.RecepientList.FirstOrDefault() ?? UserManager.UserAnonymous;

                /* Issues par fichier */
                report.FileReportList = userIssues
                    .OrderBy(IssueSorter)
                    .Take(_perUserMaxIssueCount)
                    .GroupBy(x => x.FilePath)
                    .Select(x => new FileReport
                    {
                        FilePath = x.Key,
                        IssueList = x.ToList()
                    }).ToList();

                reports.Add(report);
            }

            return reports;
        }

        private static int IssueSorter(IssueItem item)
        {
            /* Tri par type puis par sévérité. */
            return 10 * GetTypeOrder(item.Type) + GetSeverityOrder(item.Type);
        }

        private static int GetTypeOrder(string type)
        {
            switch (type)
            {
                case "BUG":
                    return 1;
                case "VULNERABILITY":
                    return 2;
                case "CODE_SMELL":
                    return 3;
                default:
                    return 4;
            }
        }

        private static int GetSeverityOrder(string type)
        {
            switch (type)
            {
                case "BLOCKER":
                    return 1;
                case "CRITICAL":
                    return 2;
                case "MAJOR":
                    return 3;
                case "MINOR":
                    return 4;
                case "INFO":
                default:
                    return 5;
            }
        }
    }
}
