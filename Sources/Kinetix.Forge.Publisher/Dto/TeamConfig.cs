namespace Kinetix.Forge.Publisher.Dto
{
    /// <summary>
    /// Configuration de l'équipe.
    /// </summary>
    public class TeamConfig
    {
        /// <summary>
        /// Utilisateurs de l'equipe. Les utilisateurs non déclarés ne recevront pas de courriel (OPT-IN).
        /// </summary>
        public UserConfig[] StandardUsers { get; set; }

        /// <summary>
        /// Utilisateurs gérant par défaut les warnings anonymes (auquel aucun utilisateur n'a été associé).
        /// </summary>
        public UserConfig[] DefaultUsers { get; set; }

        /// <summary>
        /// Utilisateurs étant systématiquement en copie des courriels envoyés.
        /// </summary>
        public UserConfig[] AdminUsers { get; set; }

        /// <summary>
        /// Utilisateurs de l'equipe. Les utilisateurs non déclarés ne recevront pas de courriel (OPT-IN).
        /// </summary>
        public UserConfig[] Forwarding { get; set; }
    }
}
