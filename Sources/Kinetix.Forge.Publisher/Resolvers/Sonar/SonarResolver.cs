using System.Collections.Generic;
using System.Net;
using Kinetix.Forge.Publisher.Dto;

namespace Kinetix.Forge.Publisher.Resolvers.Sonar
{
    /// <summary>
    /// Résolveur Sonar des auteurs d'issues.
    /// </summary>
    public class SonarResolver : IAuthorResolver
    {
        public SonarResolver()
        {
            /* Gestion du TLS avec .NET obsolète. */
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | (SecurityProtocolType)768 /* Tls11 */ | (SecurityProtocolType)3072 /* Tls12 */ | (SecurityProtocolType)12288 /* Tls13 */;
        }

        /// <inheritdoc/>
        public void ResolveAll(ICollection<IssueItem> issueList)
        {
            foreach (var issueItem in issueList)
            {
                /* Sonar fourni directement l'email dans le champ Author de l'issue. */
                issueItem.ResolvedAuthor.Email = issueItem.Author;
            }
        }
    }
}
