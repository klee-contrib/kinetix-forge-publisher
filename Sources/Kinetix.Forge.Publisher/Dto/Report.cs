using System.Collections.Generic;

namespace Kinetix.Forge.Publisher.Dto
{

    /// <summary>
    /// Rapport de warning à destination d'un utilisateur.
    /// </summary>
    public class Report
    {
        /// <summary>
        /// Obtient ou définit le nom du projet.
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Obtient ou définit les destinataires.
        /// </summary>
        public UserInfo MainRecepient { get; set; }

        /// <summary>
        /// Obtient ou définit les destinataires.
        /// </summary>
        public ICollection<UserInfo> RecepientList { get; set; } = new List<UserInfo>();

        /// <summary>
        /// Obtient ou définit la liste des rapports de fichier.
        /// </summary>
        public ICollection<FileReport> FileReportList { get; set; } = new List<FileReport>();

        /// <summary>
        /// Obtient ou définit le corps du courriel.
        /// </summary>
        public string EmailBody { get; set; }

        /// <summary>
        /// Nombre de warnings.
        /// </summary>
        public int WarningCount { get; set; }
    }
}
