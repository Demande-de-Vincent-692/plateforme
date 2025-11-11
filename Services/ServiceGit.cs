using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Plateforme.Services
{
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
        /// <returns>Message indiquant le résultat de l'opération</returns>
        public async Task<string> CloneOrPullRepositoryAsync(string cloneUrl, string repoName, string token = null)
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
                return $"❌ Erreur : {ex.Message}";
            }
        }

        /// <summary>
        /// Clone un nouveau repository
        /// </summary>
        private async Task<string> CloneRepositoryAsync(string cloneUrl, string repoPath, string repoName)
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
                        return $"✅ Repository '{repoName}' cloné avec succès !";
                    }
                    else
                    {
                        return $"❌ Erreur lors du clone de '{repoName}' :\n{error}";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"❌ Erreur lors du clone : {ex.Message}";
            }
        }

        /// <summary>
        /// Met à jour un repository existant avec git pull
        /// </summary>
        private async Task<string> PullRepositoryAsync(string repoPath, string repoName)
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
                        if (output.Contains("Already up to date"))
                        {
                            return $"ℹ️ Repository '{repoName}' déjà à jour.";
                        }
                        else
                        {
                            return $"✅ Repository '{repoName}' mis à jour avec succès !\n{output}";
                        }
                    }
                    else
                    {
                        return $"❌ Erreur lors du pull de '{repoName}' :\n{error}";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"❌ Erreur lors du pull : {ex.Message}";
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
