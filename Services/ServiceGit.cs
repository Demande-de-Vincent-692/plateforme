using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        /// <summary>
        /// Vérifie si le repository a des changements non commités
        /// </summary>
        /// <param name="repoPath">Le chemin du repository</param>
        /// <returns>True s'il y a des changements non commités, False sinon</returns>
        public async Task<bool> HasUncommittedChangesAsync(string repoPath)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "status --porcelain",
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
                    await process.WaitForExitAsync();

                    // Si output n'est pas vide, il y a des changements
                    return !string.IsNullOrWhiteSpace(output);
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Récupère la branche Git actuelle
        /// </summary>
        /// <param name="repoPath">Le chemin du repository</param>
        /// <returns>Le nom de la branche actuelle, ou null en cas d'erreur</returns>
        public async Task<string> GetCurrentBranchAsync(string repoPath)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "branch --show-current",
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
                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0)
                    {
                        return output.Trim();
                    }
                }
            }
            catch
            {
                // Ignorer les erreurs
            }
            return null;
        }

        /// <summary>
        /// Récupère toutes les branches (locales et distantes)
        /// </summary>
        /// <param name="repoPath">Le chemin du repository</param>
        /// <returns>Liste des branches avec indication si c'est la branche actuelle</returns>
        public async Task<List<GitBranch>> GetBranchesAsync(string repoPath)
        {
            var branches = new List<GitBranch>();

            try
            {
                // D'abord récupérer la branche actuelle
                string currentBranch = await GetCurrentBranchAsync(repoPath);

                // Récupérer toutes les branches (locales et distantes)
                var processInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "branch -a",
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
                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0)
                    {
                        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                        foreach (var line in lines)
                        {
                            string branchName = line.Trim().TrimStart('*').Trim();

                            // Ignorer HEAD
                            if (branchName.Contains("HEAD ->"))
                                continue;

                            bool isRemote = branchName.StartsWith("remotes/");
                            bool isCurrent = branchName == currentBranch ||
                                           (currentBranch != null && branchName == $"remotes/origin/{currentBranch}");

                            // Nettoyer le nom pour l'affichage
                            string displayName = branchName;
                            if (isRemote)
                            {
                                displayName = branchName.Replace("remotes/origin/", "origin/");
                            }

                            // Éviter les doublons (ne pas ajouter origin/branch si branch existe déjà localement)
                            if (isRemote)
                            {
                                string remoteBranchName = branchName.Replace("remotes/origin/", "");
                                if (branches.Any(b => !b.IsRemote && b.Name == remoteBranchName))
                                    continue;
                            }

                            branches.Add(new GitBranch
                            {
                                Name = branchName,
                                DisplayName = displayName,
                                IsRemote = isRemote,
                                IsCurrent = isCurrent
                            });
                        }
                    }
                }

                // Trier : branche actuelle en premier, puis locales, puis distantes
                branches = branches
                    .OrderByDescending(b => b.IsCurrent)
                    .ThenBy(b => b.IsRemote)
                    .ThenBy(b => b.DisplayName)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération des branches : {ex.Message}");
            }

            return branches;
        }

        /// <summary>
        /// Change de branche Git
        /// </summary>
        /// <param name="repoPath">Le chemin du repository</param>
        /// <param name="branchName">Le nom de la branche (peut être local ou remote)</param>
        /// <returns>Résultat de l'opération</returns>
        public async Task<GitOperationResult> CheckoutBranchAsync(string repoPath, string branchName)
        {
            try
            {
                // Vérifier s'il y a des changements non commités
                bool hasChanges = await HasUncommittedChangesAsync(repoPath);
                if (hasChanges)
                {
                    return new GitOperationResult
                    {
                        Success = false,
                        Message = "⚠️ Vous avez des changements non commités. Veuillez les commiter ou les annuler avant de changer de branche.",
                        RepoPath = repoPath
                    };
                }

                string gitCommand;
                bool isRemoteBranch = branchName.StartsWith("remotes/");

                if (isRemoteBranch)
                {
                    // Pour une branche distante, créer une branche locale qui track la distante
                    string localBranchName = branchName.Replace("remotes/origin/", "");
                    gitCommand = $"checkout -b {localBranchName} {branchName}";
                }
                else
                {
                    // Pour une branche locale, checkout simple
                    gitCommand = $"checkout {branchName}";
                }

                var processInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = gitCommand,
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

                    if (process.ExitCode == 0 || output.Contains("Switched to") || error.Contains("Switched to"))
                    {
                        string displayBranch = isRemoteBranch
                            ? branchName.Replace("remotes/origin/", "")
                            : branchName;

                        return new GitOperationResult
                        {
                            Success = true,
                            Message = $"✅ Switched to branch '{displayBranch}'",
                            RepoPath = repoPath
                        };
                    }
                    else
                    {
                        // Cas spécial : la branche existe déjà localement
                        if (error.Contains("already exists"))
                        {
                            string localBranchName = branchName.Replace("remotes/origin/", "");
                            return await CheckoutBranchAsync(repoPath, localBranchName);
                        }

                        return new GitOperationResult
                        {
                            Success = false,
                            Message = $"❌ Erreur lors du changement de branche :\n{error}",
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
                    Message = $"❌ Erreur : {ex.Message}",
                    RepoPath = repoPath
                };
            }
        }

        /// <summary>
        /// Fetch et pull les derniers changements depuis le repository distant
        /// </summary>
        /// <param name="repoPath">Le chemin du repository</param>
        /// <returns>Résultat de l'opération</returns>
        public async Task<GitOperationResult> FetchAndPullAsync(string repoPath)
        {
            try
            {
                // Étape 1 : git fetch pour récupérer les références
                var fetchProcessInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "fetch",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = repoPath
                };

                using (var fetchProcess = new Process { StartInfo = fetchProcessInfo })
                {
                    fetchProcess.Start();
                    string fetchOutput = await fetchProcess.StandardOutput.ReadToEndAsync();
                    string fetchError = await fetchProcess.StandardError.ReadToEndAsync();
                    await fetchProcess.WaitForExitAsync();

                    if (fetchProcess.ExitCode != 0)
                    {
                        return new GitOperationResult
                        {
                            Success = false,
                            Message = $"❌ Erreur lors du fetch :\n{fetchError}",
                            RepoPath = repoPath
                        };
                    }
                }

                // Étape 2 : git pull pour fusionner les changements
                var pullProcessInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "pull",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = repoPath
                };

                using (var pullProcess = new Process { StartInfo = pullProcessInfo })
                {
                    pullProcess.Start();
                    string pullOutput = await pullProcess.StandardOutput.ReadToEndAsync();
                    string pullError = await pullProcess.StandardError.ReadToEndAsync();
                    await pullProcess.WaitForExitAsync();

                    // Vérifier s'il y a des conflits
                    if (pullError.Contains("CONFLICT") || pullOutput.Contains("CONFLICT"))
                    {
                        return new GitOperationResult
                        {
                            Success = false,
                            Message = "⚠️ Conflits détectés lors du pull.\n\nVeuillez résoudre les conflits manuellement dans votre IDE,\npuis committez les modifications.",
                            RepoPath = repoPath
                        };
                    }

                    if (pullProcess.ExitCode == 0)
                    {
                        string message;
                        if (pullOutput.Contains("Already up to date") || pullOutput.Contains("Already up-to-date"))
                        {
                            message = "ℹ️ Already up to date. No changes to pull.";
                        }
                        else if (pullOutput.Contains("Fast-forward"))
                        {
                            message = "✅ Successfully pulled latest changes (Fast-forward).";
                        }
                        else
                        {
                            message = $"✅ Successfully pulled latest changes!\n{pullOutput}";
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
                            Message = $"❌ Erreur lors du pull :\n{pullError}",
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
                    Message = $"❌ Erreur : {ex.Message}",
                    RepoPath = repoPath
                };
            }
        }

        /// <summary>
        /// Récupère la liste des fichiers modifiés dans le repository
        /// </summary>
        /// <param name="repoPath">Le chemin du repository</param>
        /// <returns>Liste des fichiers modifiés avec leur statut</returns>
        public async Task<List<GitModifiedFile>> GetModifiedFilesAsync(string repoPath)
        {
            var modifiedFiles = new List<GitModifiedFile>();

            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "status --porcelain",
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
                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                    {
                        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                        foreach (var line in lines)
                        {
                            if (line.Length < 3)
                                continue;

                            string statusCode = line.Substring(0, 2);
                            string filePath = line.Substring(3).Trim();

                            string status = "";
                            string displayStatus = "";

                            // Interpréter les codes de statut Git
                            if (statusCode.Contains("M"))
                            {
                                status = "Modified";
                                displayStatus = "M";
                            }
                            else if (statusCode.Contains("A"))
                            {
                                status = "Added";
                                displayStatus = "A";
                            }
                            else if (statusCode.Contains("D"))
                            {
                                status = "Deleted";
                                displayStatus = "D";
                            }
                            else if (statusCode.Contains("R"))
                            {
                                status = "Renamed";
                                displayStatus = "R";
                            }
                            else if (statusCode.Contains("?"))
                            {
                                status = "Untracked";
                                displayStatus = "?";
                            }
                            else
                            {
                                status = "Changed";
                                displayStatus = "C";
                            }

                            modifiedFiles.Add(new GitModifiedFile
                            {
                                FilePath = filePath,
                                Status = status,
                                DisplayStatus = displayStatus
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération des fichiers modifiés : {ex.Message}");
            }

            return modifiedFiles;
        }

        /// <summary>
        /// Effectue un commit et un push des changements
        /// </summary>
        /// <param name="repoPath">Le chemin du repository</param>
        /// <param name="commitTitle">Le titre du commit</param>
        /// <param name="commitDescription">La description du commit (optionnel)</param>
        /// <returns>Résultat de l'opération</returns>
        public async Task<GitOperationResult> CommitAndPushAsync(string repoPath, string commitTitle, string commitDescription)
        {
            try
            {
                // Étape 1 : git add .
                var addProcessInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "add .",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = repoPath
                };

                using (var addProcess = new Process { StartInfo = addProcessInfo })
                {
                    addProcess.Start();
                    string addError = await addProcess.StandardError.ReadToEndAsync();
                    await addProcess.WaitForExitAsync();

                    if (addProcess.ExitCode != 0)
                    {
                        return new GitOperationResult
                        {
                            Success = false,
                            Message = $"❌ Erreur lors de l'ajout des fichiers :\n{addError}",
                            RepoPath = repoPath
                        };
                    }
                }

                // Étape 2 : git commit
                string commitMessage = commitTitle;
                if (!string.IsNullOrWhiteSpace(commitDescription))
                {
                    commitMessage = $"{commitTitle}\n\n{commitDescription}";
                }

                var commitProcessInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"commit -m \"{commitMessage.Replace("\"", "\\\"")}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = repoPath
                };

                using (var commitProcess = new Process { StartInfo = commitProcessInfo })
                {
                    commitProcess.Start();
                    string commitOutput = await commitProcess.StandardOutput.ReadToEndAsync();
                    string commitError = await commitProcess.StandardError.ReadToEndAsync();
                    await commitProcess.WaitForExitAsync();

                    if (commitProcess.ExitCode != 0)
                    {
                        return new GitOperationResult
                        {
                            Success = false,
                            Message = $"❌ Erreur lors du commit :\n{commitError}",
                            RepoPath = repoPath
                        };
                    }
                }

                // Étape 3 : git push
                var pushProcessInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "push",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = repoPath
                };

                using (var pushProcess = new Process { StartInfo = pushProcessInfo })
                {
                    pushProcess.Start();
                    string pushOutput = await pushProcess.StandardOutput.ReadToEndAsync();
                    string pushError = await pushProcess.StandardError.ReadToEndAsync();
                    await pushProcess.WaitForExitAsync();

                    if (pushProcess.ExitCode != 0)
                    {
                        return new GitOperationResult
                        {
                            Success = false,
                            Message = $"❌ Erreur lors du push :\n{pushError}\n\n💡 Essayez d'utiliser le bouton Fetch pour récupérer les dernières modifications avant de pousser.",
                            RepoPath = repoPath
                        };
                    }
                }

                return new GitOperationResult
                {
                    Success = true,
                    Message = "✅ Changements commités et poussés avec succès !",
                    RepoPath = repoPath
                };
            }
            catch (Exception ex)
            {
                return new GitOperationResult
                {
                    Success = false,
                    Message = $"❌ Erreur : {ex.Message}",
                    RepoPath = repoPath
                };
            }
        }

        /// <summary>
        /// Exécute le script setup.cmd dans le repository pour installer les dépendances
        /// </summary>
        /// <param name="repoPath">Le chemin du repository contenant setup.cmd</param>
        /// <returns>Résultat de l'opération incluant le succès et le message</returns>
        public async Task<GitOperationResult> RunSetupScriptAsync(string repoPath)
        {
            try
            {
                string setupScriptPath = Path.Combine(repoPath, "setup.cmd");

                // Vérifier que le script existe
                if (!File.Exists(setupScriptPath))
                {
                    return new GitOperationResult
                    {
                        Success = false,
                        Message = "⚠️ Le fichier setup.cmd n'existe pas dans ce projet.",
                        RepoPath = repoPath
                    };
                }

                // Lancer le script dans une fenêtre visible pour que l'utilisateur puisse suivre la progression
                var processInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/K \"cd /d \"{repoPath}\" && setup.cmd && exit\"",
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WorkingDirectory = repoPath
                };

                using (var process = new Process { StartInfo = processInfo })
                {
                    process.Start();

                    // Attendre la fin du processus
                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0)
                    {
                        return new GitOperationResult
                        {
                            Success = true,
                            Message = "✅ Dépendances installées avec succès !",
                            RepoPath = repoPath
                        };
                    }
                    else
                    {
                        return new GitOperationResult
                        {
                            Success = false,
                            Message = $"⚠️ L'installation des dépendances s'est terminée avec le code {process.ExitCode}.",
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
                    Message = $"❌ Erreur lors de l'exécution de setup.cmd : {ex.Message}",
                    RepoPath = repoPath
                };
            }
        }
    }

    /// <summary>
    /// Représente une branche Git
    /// </summary>
    public class GitBranch
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public bool IsRemote { get; set; }
        public bool IsCurrent { get; set; }
    }

    /// <summary>
    /// Représente un fichier modifié dans Git
    /// </summary>
    public class GitModifiedFile
    {
        public string FilePath { get; set; }
        public string Status { get; set; }
        public string DisplayStatus { get; set; }
    }
}
