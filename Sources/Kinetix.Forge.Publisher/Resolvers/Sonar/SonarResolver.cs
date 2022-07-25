using System.Collections.Generic;
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
