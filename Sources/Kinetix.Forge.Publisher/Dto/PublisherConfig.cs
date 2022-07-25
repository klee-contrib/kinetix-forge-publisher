namespace Kinetix.Forge.Publisher.Dto
{
    public class PublisherConfig
    {
        /* Nom du projet affiché dans les rapports. */
        public string ProjectName { get; set; }

        /// <summary>
        /// Config Sonar.
        /// </summary>
        public SonarConfig Sonar { get; set; } = new SonarConfig();

        /// <summary>
        /// Config TFS.
        /// </summary>
        public TfsConfig Tfs { get; set; } = new TfsConfig();

        /// <summary>
        /// Config LDAP.
        /// </summary>
        public LdapConfig Ldap { get; set; } = new LdapConfig();

        /* Nombre maximum d'issues à envoyer à un utilisateur. */
        public int? PerUserMaxIssueCount { get; set; }

        /// <summary>
        /// Config Mail publisher.
        /// </summary>
        public MailPublisherConfig MailPublisher { get; set; } = new MailPublisherConfig();

        /// <summary>
        /// Résolveur d'utilisateur.
        /// TFS : retrouve le WindowsID à partir du commit du fichier.
        /// Sonar : retrouve l'email de l'utilisateur à partir de Sonar directement.
        /// Le LDAP est ensuite utilisé pour retrouver les infos complètes (email + nom).
        /// </summary>
        public string Resolver { get; set; }

        /// <summary>
        /// Configuration de l'équipe.
        /// </summary>
        public TeamConfig Team { get; set; }
    }
}
