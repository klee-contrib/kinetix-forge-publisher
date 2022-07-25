using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Kinetix.Forge.Publisher.Common;
using Kinetix.Forge.Publisher.Dto;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Kinetix.Forge.Publisher.Resolvers.Tfs
{
    /// <summary>
    /// Résolveur TFS des auteurs d'issues.
    /// </summary>
    public class TfsResolver : IAuthorResolver
    {
        /// <summary>
        /// Dégré de parallélisation des appels TFS.
        /// </summary>
        private const int TfsThreadCount = 5;

        private readonly ChangeSetStore _changeSetStore;
        private readonly AnnotedFileStore _annotedFileStore;

        public TfsResolver(TfsConfig config)
        {
            var projectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(config.TfsCollectionUrl));
            var versionControlServer = (VersionControlServer)projectCollection.GetService(typeof(VersionControlServer));
            _changeSetStore = new ChangeSetStore(versionControlServer);
            _annotedFileStore = new AnnotedFileStore(config.TfsWorkingDir);
        }

        /// <inheritdoc/>
        public void ResolveAll(ICollection<IssueItem> issueList)
        {
            /* Résoud les issues en multithread. */
            Parallel.ForEach(
                issueList,
                new ParallelOptions { MaxDegreeOfParallelism = TfsThreadCount },
                Resolve);
        }

        /// <summary>
        /// Résoud l'auteur d'une issue (champ AccountName rempli avec l'identifiant Windows).
        /// </summary>
        /// <param name="issueItem">Issue.</param>
        private void Resolve(IssueItem issueItem)
        {
            try
            {
                /* Annoter le fichier. */
                var annotedFile = _annotedFileStore.GetAnnotedFile(issueItem.FilePath);

                /* Ontient le changeset de la ligne de l'issue. */
                int line = issueItem.Line == -1 ? 1 : issueItem.Line; // TODO trouver plus élégant
                if (annotedFile.Count < line)
                {
                    LogUtils.Info($"WARNING : TFS Resolve : impossible d'annoter la ligne {line} du fichier {issueItem.FilePath} qui ne contient que {annotedFile.Count} lignes.");
                    return;
                }

                var changesetId = annotedFile[line];
                // TODO voir quand ça peut péter

                /* Trouver l'auteur du changeset */
                string committer = _changeSetStore.GetCommitter(changesetId);

                issueItem.ResolvedAuthor.AccountName = committer;
            }
            catch (Exception ex)
            {
                LogUtils.Info($"Error dans TFS Resolve : {ex}");
            }
        }

        /// <summary>
        /// Magasin de changeset : associe un changeset à un auteur.
        /// On utilise un dicionnaire gérant le multithreading pour paralléliser les appels.
        /// </summary>
        private class ChangeSetStore : Dictionary<int, string>
        {
            private readonly LockProvider _lockProvider = new LockProvider();
            private readonly VersionControlServer _versionControlServer;

            public ChangeSetStore(VersionControlServer versionControlServer)
            {
                _versionControlServer = versionControlServer;
            }

            public string GetCommitter(int changesetId)
            {
                lock (_lockProvider.GetLock(changesetId))
                {
                    /* Cherche la valeur dans le cache. */
                    if (this.TryGetValue(changesetId, out string commiter))
                    {
                        /* Valeur trouvée : on la retourne. */
                        return commiter;
                    }

                    /* Exécute une requête à TFS. */
                    commiter = GetCommiterCore(changesetId);

                    /* Stocke la valeur dans le cache. */
                    this[changesetId] = commiter;

                    /* Retourne la valeur. */
                    return commiter;
                }
            }

            private string GetCommiterCore(int changesetId)
            {
                LogUtils.Info($"TFS GetChangeset {changesetId}");
                /* Requête TFS pour avoir les informations sur le changeset */
                return _versionControlServer.GetChangeset(changesetId).Committer;
            }
        }

        /// <summary>
        /// Magasin de fichiers annotés : associe un chemin relatif à la branche à un fichier annoté.
        /// On utilise un dicionnaire gérant le multithreading pour paralléliser les appels.
        /// </summary>
        private class AnnotedFileStore : Dictionary<string, AnnotedFile>
        {
            private readonly LockProvider _lockProvider = new LockProvider();
            private readonly string _workingDir;

            public AnnotedFileStore(string workingDir)
            {
                _workingDir = workingDir;
            }

            public AnnotedFile GetAnnotedFile(string filePath)
            {
                lock (_lockProvider.GetLock(filePath))
                {
                    /* Cherche la valeur dans le cache. */
                    if (this.TryGetValue(filePath, out AnnotedFile annotedFile))
                    {
                        /* Valeur trouvée : on la retourne. */
                        return annotedFile;
                    }

                    /* Exécute une requête à TFS. */
                    annotedFile = GetAnnotedFileWithTry(filePath);

                    /* Stocke la valeur dans le cache. */
                    this[filePath] = annotedFile;

                    /* Retourne la valeur. */
                    return annotedFile;
                }
            }

            private AnnotedFile GetAnnotedFileWithTry(string filePath)
            {
                /* Robustesse : tente plusieurs fois d'annoter le fichier pour gérer les échecs. */
                const int nbEssai = 2;
                for (int i = 0; i < nbEssai; i++)
                {
                    var candidate = GetAnnotedFileCore(filePath);
                    if (!candidate.IsEmptyFile)
                    {
                        return candidate;
                    }
                }

                return new AnnotedFile();
            }

            private AnnotedFile GetAnnotedFileCore(string filePath)
            {
                LogUtils.Info($"TFS annotate {filePath}");

                /* Appel Team Foundation Power Tools en CLI et parse le résultat. */
                ProcessStartInfo startInfo = new ProcessStartInfo("tfpt.exe", "annotate /noprompt \"" + filePath + "\"")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WorkingDirectory = _workingDir
                };
                Process process = Process.Start(startInfo);

                /* Lit le résultat. */
                var annotedFile = new AnnotedFile();
                process.StandardOutput.ReadLine();
                while (!process.StandardOutput.EndOfStream)
                {
                    string line = process.StandardOutput.ReadLine();
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }
                    /* Le résultat est un tableau avec pour colonnes l'ID du changeset et la contenu de la ligne. */
                    int changeSetId = Convert.ToInt32(line.Split(' ')[0]);
                    annotedFile.Add(changeSetId);
                }
                process.WaitForExit();

                return annotedFile;
            }
        }

        /// <summary>
        /// Fichier annoté : index (numéro de ligne) vers changeset de la dernière modification de la ligne.
        /// </summary>
        private class AnnotedFile : List<int>
        {
            /// <summary>
            /// Indique si le fichier est vide.
            /// </summary>
            public bool IsEmptyFile
            {
                get
                {
                    return !this.Any();
                }
            }
        }
    }
}
