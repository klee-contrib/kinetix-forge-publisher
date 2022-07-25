
namespace Kinetix.Forge.Publisher.Dto
{

    /// <summary>
    /// Objet stockant les informations sur un utilisateur dans la config.
    /// </summary>
    public class UserConfig
    {
        /// <summary>
        /// Compte de l'utilisateur.
        /// </summary>
        public string Account
        {
            get;
            set;
        }

        /// <summary>
        /// Email de l'utilisateur.
        /// </summary>
        public string Email
        {
            get;
            set;
        }

        /// <summary>
        /// Compte du suiveur.
        /// </summary>
        public string ForwardAccount
        {
            get;
            set;
        }

        /// <summary>
        /// Email du suiveur.
        /// </summary>
        public string ForwardEmail
        {
            get;
            set;
        }
    }
}
