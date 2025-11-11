using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Plateforme.Services
{
    /// <summary>
    /// Résultat d'une opération Git
    /// </summary>
    public class GitOperationResult
    {
        public bool Success { get; set; }
        public required string Message { get; set; }
        public required string RepoPath { get; set; }
    }

    public class ServiceGit
    {
        private readonly string _repoBaseDirectory;

        public ServiceGit(string repoBaseDirectory)
        {
            _repoBaseDirectory = repoBaseDirectory;

            // Créer le dossier Repo s'il n'existe pas
            if (!Directory.Exists(_repoBaseDirectory))
            {
                Directory.CreateDirectory(_repoBaseDirectory);
            }
        }

        /// <summary>
        /// Clone un repository ou le met à jour s'il existe déjà
        /// </summary>
        /// <param name="cloneUrl">L'URL de clone du repository</param>
        /// <param name="repoName">Le nom du repository</param>
        /// <param name="token">Token GitHub pour l'authentification (optionnel)</param>
        /// <returns>Résultat de l'opération incluant le succès, le message et le chemin</returns>
        public async Task<GitOperationResult> CloneOrPullRepositoryAsync(string cloneUrl, string repoName, string token = null)
        {
            try
            {
                string repoPath = Path.Combine(_repoBaseDirectory, repoName);

                // Modifier l'URL de clone pour inclure le token si fourni
                string authenticatedUrl = cloneUrl;
                if (!string.IsNullOrWhiteSpace(token))
                {
                    // Remplacer https:// par https://TOKEN@
                    authenticatedUrl = cloneUrl.Replace("https://", $"https://{token}@");
                }

                // Vérifier si le repository existe déjà
                if (Directory.Exists(repoPath))
                {
                    // Le repo existe, faire un pull
                    return await PullRepositoryAsync(repoPath, repoName);
                }
                else
                {
                    // Le repo n'existe pas, le cloner
                    return await CloneRepositoryAsync(authenticatedUrl, repoPath, repoName);
                }
            }
            catch (Exception ex)
            {
                return new GitOperationResult
                {
                    Success = false,
                    Message = $"❌ Erreur : {ex.Message}",
                    RepoPath = Path.Combine(_repoBaseDirectory, repoName)
                };
            }
        }

        /// <summary>
        /// Clone un nouveau repository
        /// </summary>
        private async Task<GitOperationResult> CloneRepositoryAsync(string cloneUrl, string repoPath, string repoName)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"clone \"{cloneUrl}\" \"{repoPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = _repoBaseDirectory
                };

                using (var process = new Process { StartInfo = processInfo })
                {
                    process.Start();

                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();

                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0)
                    {
                        return new GitOperationResult
                        {
                            Success = true,
                            Message = $"✅ Repository '{repoName}' cloné avec succès !",
                            RepoPath = repoPath
                        };
                    }
                    else
                    {
                        return new GitOperationResult
                        {
                            Success = false,
                            Message = $"❌ Erreur lors du clone de '{repoName}' :\n{error}",
                            RepoPath = repoPath
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new GitOperationResult
                {
                    Success = false,
                    Message = $"❌ Erreur lors du clone : {ex.Message}",
                    RepoPath = repoPath
                };
            }
        }

        /// <summary>
        /// Met à jour un repository existant avec git pull
        /// </summary>
        private async Task<GitOperationResult> PullRepositoryAsync(string repoPath, string repoName)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "pull",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = repoPath
                };

                using (var process = new Process { StartInfo = processInfo })
                {
                    process.Start();

                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();

                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0)
                    {
                        string message;
                        if (output.Contains("Already up to date"))
                        {
                            message = $"ℹ️ Repository '{repoName}' déjà à jour.";
                        }
                        else
                        {
                            message = $"✅ Repository '{repoName}' mis à jour avec succès !\n{output}";
                        }

                        return new GitOperationResult
                        {
                            Success = true,
                            Message = message,
                            RepoPath = repoPath
                        };
                    }
                    else
                    {
                        return new GitOperationResult
                        {
                            Success = false,
                            Message = $"❌ Erreur lors du pull de '{repoName}' :\n{error}",
                            RepoPath = repoPath
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new GitOperationResult
                {
                    Success = false,
                    Message = $"❌ Erreur lors du pull : {ex.Message}",
                    RepoPath = repoPath
                };
            }
        }

        /// <summary>
        /// Ouvre le repository dans Visual Studio Code
        /// </summary>
        /// <param name="repoPath">Le chemin du repository à ouvrir</param>
        /// <returns>True si VSCode a été lancé avec succès, False sinon</returns>
        public bool OpenInVSCode(string repoPath)
        {
            try
            {
                if (!Directory.Exists(repoPath))
                {
                    return false;
                }

                var processInfo = new ProcessStartInfo
                {
                    FileName = "code",
                    Arguments = $"\"{repoPath}\"",
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                Process.Start(processInfo);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Ouvre le repository dans Visual Studio
        /// </summary>
        /// <param name="repoPath">Le chemin du repository à ouvrir</param>
        /// <returns>True si Visual Studio a été lancé avec succès, False sinon</returns>
        public bool OpenInVisualStudio(string repoPath)
        {
            try
            {
                if (!Directory.Exists(repoPath))
                {
                    return false;
                }

                // Chercher un fichier solution (.sln) dans le répertoire
                var solutionFiles = Directory.GetFiles(repoPath, "*.sln", SearchOption.TopDirectoryOnly);

                if (solutionFiles.Length > 0)
                {
                    // Ouvrir la première solution trouvée
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = solutionFiles[0],
                        UseShellExecute = true
                    };

                    Process.Start(processInfo);
                    return true;
                }
                else
                {
                    // Pas de fichier .sln, ouvrir le dossier directement
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = repoPath,
                        UseShellExecute = true
                    };

                    Process.Start(processInfo);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Vérifie si git est installé sur le système
        /// </summary>
        public static async Task<bool> IsGitInstalledAsync()
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = new Process { StartInfo = processInfo })
                {
                    process.Start();
                    await process.WaitForExitAsync();
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
