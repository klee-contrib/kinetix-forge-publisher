using System.Collections.Generic;

namespace Kinetix.Forge.Publisher.Dto
{
    /// <summary>
    /// Rapport sur un fichier en particulier.
    /// </summary>
    public class FileReport
    {
        /// <summary>
        /// Chemin du fichier.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Obtient ou définit la liste des messages.
        /// </summary>
        public ICollection<IssueItem> IssueList { get; set; } = new List<IssueItem>();
    }
}
