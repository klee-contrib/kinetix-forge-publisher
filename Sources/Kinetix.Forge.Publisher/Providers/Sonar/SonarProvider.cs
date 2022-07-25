using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Kinetix.Forge.Publisher.Dto;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Extensions.MonoHttp;

namespace Kinetix.Forge.Publisher.Providers.Sonar
{
    /// <summary>
    /// Fournisseur d'issues à partir d'un Sonarqube.
    /// </summary>
    public class SonarProvider : IIssueProvider
    {
        private readonly SonarConfig _config;
        private const int PageSize = 50;

        /// <summary>
        /// Créé une nouvelle instance de SonarProvider.
        /// </summary>
        /// <param name="config">Config Sonar.</param>
        public SonarProvider(SonarConfig config)
        {
            _config = config;
            _config.IssueTypes = _config.IssueTypes ?? new[] { "BUG", "VULNERABILITY", "SECURITY_HOTSPOT", "CODE_SMELL" };
            _config.MaxIssueCount = _config.MaxIssueCount ?? 200;
        }

        public ICollection<IssueItem> GetIssues()
        {
            var issueList = new List<IssueItem>();

            /* Créé le client sur l'API de Sonar. */
            var client = GetClient();

            /* Requête les issues en priorisant les types les plus importants. */
            foreach (var type in _config.IssueTypes)
            {
                LogUtils.Info($"Requêtes SONAR pour le type {type}...");
                GetIssuesForType(client, type, issueList);
            }

            return issueList;
        }


        public CoverageReport GetCoverageReport()
        {
            LogUtils.Info($"Requête SONAR couverture...");

            /* Construit la requête. */
            var client = GetClient();
            RestRequest request = new RestRequest("/api/measures/component");
            request.AddQueryParameter("componentKey", _config.SonarProjectKey);  // Filtrage sur le composant du projet.           
            request.AddQueryParameter("metricKeys", "test_failures,test_errors"); // Filtrage sur les métriques de couverture

            /* Lit la réponse. */
            var response = client.Execute(request);
            CheckResponse(response);

            var output = JObject.Parse(response.Content);
            var measures = output["component"]["measures"] as JArray;
            var metricMap = measures.ToDictionary(
                x => x["metric"].Value<string>(),
                x => x["value"].Value<int>());
            int nbError = metricMap["test_errors"];
            int nbFailure = metricMap["test_failures"];

            /* Construit le rapport. */
            var report = new CoverageReport
            {
                NbError = nbError,
                NbFailure = nbFailure
            };

            /* Calcule le lien. */
            ComputeCoverageSonarLink(report);

            return report;
        }

        private static void CheckResponse(IRestResponse response)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"Sonar dit not desponded OK : {response.ErrorMessage}");
            }
        }

        private RestClient GetClient()
        {
            return new RestClient(_config.SonarUrl)
            {
                /* User Token SonarQube. Nécessaire pour avoir l'information "Author" des issues. */
                Authenticator = new HttpBasicAuthenticator(_config.SonarToken, string.Empty)
            };
        }

        private void GetIssuesForType(RestClient client, string types, List<IssueItem> issueList)
        {
            /* Condition de sortie : nombre maximum d'issues atteint. */
            if (issueList.Count >= _config.MaxIssueCount)
            {
                return;
            }

            /* Requête tant que la page n'est pas vide. */
            var pageIdx = 0;
            while (true)
            {
                pageIdx++;
                var issueArray = GetIssuePage(client, pageIdx, types);

                /* Condition de sortie : page vide. */
                if (issueArray.Count == 0)
                {
                    break;
                }

                foreach (var sonarIssue in issueArray)
                {
                    /* Condition de sortie : nombre maximum d'issues atteint. */
                    if (issueList.Count >= _config.MaxIssueCount)
                    {
                        break;
                    }

                    try
                    {
                        var issueItem = ParseIssue(sonarIssue);

                        /* Filtrage des issues à ignorer. */
                        if (ShouldIgnore(issueItem))
                        {
                            continue;
                        }

                        issueList.Add(issueItem);
                    }
                    catch (Exception ex)
                    {
                        LogUtils.Info($"Erreur parsing issue {ex}");
                        LogUtils.Info(sonarIssue.ToString());
                    }
                }

                /* Condition de sortie : nombre maximum d'issues atteint. */
                if (issueList.Count > _config.MaxIssueCount)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Indique si l'issue être ignorée.
        /// </summary>
        /// <param name="issueItem">Issue.</param>
        /// <returns><code>True</code> si à ignorer.</returns>
        private bool ShouldIgnore(IssueItem issueItem)
        {
            if (_config.RulesBlackList == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(issueItem.Rule))
            {
                return false;
            }

            return _config.RulesBlackList.Any(x => x == issueItem.Rule);
        }

        private IssueItem ParseIssue(JToken sonarIssue)
        {
            /* Calcule le chemin du fichier. */
            var component = sonarIssue["component"].Value<string>();
            var filePath = ComputeFilePath(component);

            /* Autres valeurs. */
            var message = sonarIssue["message"].Value<string>();
            var rule = sonarIssue["rule"].Value<string>();
            var type = sonarIssue["type"].Value<string>();
            var severity = sonarIssue["severity"].Value<string>();
            var sonarKey = sonarIssue["key"].Value<string>();
            var author = sonarIssue["author"] == null ? "" : sonarIssue["author"].Value<string>();
            var line = GetLine(sonarIssue);
            var issueItem = new IssueItem
            {
                SonarKey = sonarKey,
                Component = component,
                Message = message,
                Rule = rule,
                Type = type,
                Severity = severity,
                FilePath = filePath,
                Line = line,
                Author = author
            };

            ComputeIssueSonarLink(issueItem);

            return issueItem;
        }

        private JArray GetIssuePage(RestClient client, int pageIdx, string types)
        {
            LogUtils.Info($"Requête SONAR page {pageIdx}...");
            RestRequest request = new RestRequest("/api/issues/search");
            request.AddQueryParameter("componentKeys", _config.SonarProjectKey); // Filtrage sur le composant du projet.
            request.AddQueryParameter("languages", _config.Languages);
            request.AddQueryParameter("ps", PageSize.ToString()); // Taille de la page.
            request.AddQueryParameter("p", pageIdx.ToString()); // Index de la page
            request.AddQueryParameter("s", "SEVERITY"); // Tri sur la gravité
            request.AddQueryParameter("asc", "false"); // Tri descendant
            request.AddQueryParameter("types", types); // Filtrage sur les bugs.
            request.AddQueryParameter("statuses", "OPEN"); // Filtre pour avoir les issues ouvertes
            if (_config.SinceLeakPeriod == true)
            {
                request.AddQueryParameter("sinceLeakPeriod", "true"); // Filtre pour avoir les issues depuis la leak period
            }


            IRestResponse response = client.Execute(request);
            CheckResponse(response);

            var output = JObject.Parse(response.Content);

            /* Transforme le résultat. */
            return output["issues"] as JArray;
        }

        private void ComputeIssueSonarLink(IssueItem item)
        {
            item.SonarLink = $"{_config.SonarUrl}/project/issues?id={HttpUtility.UrlEncode(item.Component)}&open={item.SonarKey}&resolved=false&types={item.Type}&rules={HttpUtility.UrlEncode(item.Rule)}";
        }

        private void ComputeCoverageSonarLink(CoverageReport report)
        {
            Func<string, string> linkBuilder = metric => $"{_config.SonarUrl}/component_measures?id={HttpUtility.UrlEncode(_config.SonarProjectKey)}&metric={metric}&view=list";
            report.FailureSonarLink = linkBuilder("test_failures");
            report.ErrorSonarLink = linkBuilder("test_errors");
        }

        private static int GetLine(JToken sonarIssue)
        {
            var lineToken = sonarIssue["line"];
            if (lineToken != null)
            {
                return lineToken.Value<int>();
            }

            var textRangeToken = sonarIssue["textRange"];
            if (textRangeToken != null)
            {
                return textRangeToken["startLine"].Value<int>();

            }

            return -1; // TODO constante.
        }

        private string ComputeFilePath(string component)
        {
            /* Résolution du chemin du fichier. */
            // Exemple :            
            // "component" : "acme:Acme.SuperProjet:Sources/Acme.SuperProjet.Wpfapp/Acme.SuperProjet.Model/Model/SafeDictionary.cs"
            // Project Key : "acme:Acme.SuperProjet"
            // FilePath : Sources/Acme.SuperProjet.Wpfapp/Acme.SuperProjet.Model/Model/SafeDictionary.cs
            /* On remplace la clé du Project par le chemin */
            var filePath = component.Replace(_config.SonarProjectKey + ":", string.Empty);
            return filePath;
        }
    }
}
