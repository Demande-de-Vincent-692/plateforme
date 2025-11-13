using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Octokit;
using Plateforme.Services;

namespace Plateforme
{
    // Classe helper pour afficher les branches dans le ComboBox
    public class BranchDisplayItem
    {
        public string Icon { get; set; }
        public string DisplayName { get; set; }
        public string BranchName { get; set; }
        public bool IsCurrent { get; set; }
    }

    public partial class MainWindow : Window
    {
        private ServiceGitHub _serviceGitHub;
        private ServiceGit _serviceGit;
        private string _githubToken;
        private int _notificationCount = 0;
        private string _repoDirectory;
        private Repository _selectedRepository;
        private bool _isChangingBranch = false;

        public MainWindow()
        {
            InitializeComponent();

            // Charger le token depuis appsettings.json
            LoadConfiguration();

            _serviceGitHub = new ServiceGitHub("Demande-De-Vincent-692");

            // Créer le service Git avec le dossier "Repo" à la racine du projet
            // Remonter de 3 niveaux depuis bin/Debug/net8.0-windows/ pour atteindre la racine
            string projectRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName;
            _repoDirectory = Path.Combine(projectRoot, "Repo");
            _serviceGit = new ServiceGit(_repoDirectory);

            LoadProjects();
        }

        private void LoadConfiguration()
        {
            try
            {
                string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

                if (File.Exists(settingsPath))
                {
                    string jsonContent = File.ReadAllText(settingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(jsonContent);
                    _githubToken = settings?.GitHub?.Token ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                AddNotification($"⚠️ Failed to load configuration: {ex.Message}");
            }
        }

        private bool IsProjectInstalled(string repoName)
        {
            string repoPath = Path.Combine(_repoDirectory, repoName);
            return Directory.Exists(repoPath);
        }

        private async void LoadProjects()
        {
            try
            {
                AddNotification("🔍 Loading projects from GitHub...");

                // Récupérer les repos
                var repos = await _serviceGitHub.GetOrganizationRepositoriesAsync();
                AddNotification($"✅ {repos.Count} project(s) found!");

                // Vider les panneaux avant d'ajouter les nouveaux projets
                AvailableProjectsPanel.Children.Clear();
                InstalledProjectsPanel.Children.Clear();

                int availableCount = 0;
                int installedCount = 0;

                // Créer une carte pour chaque repo et la placer dans le bon onglet
                foreach (var repo in repos)
                {
                    // Vérifier si le projet est déjà installé
                    if (IsProjectInstalled(repo.Name))
                    {
                        // Créer la carte avec le tag "Installed"
                        Border card = CreateProjectCard(repo, "Installed");
                        InstalledProjectsPanel.Children.Add(card);
                        installedCount++;
                        AddNotification($"   💻 {repo.Name} (installed)");
                    }
                    else
                    {
                        // Créer la carte avec le tag "Available"
                        Border card = CreateProjectCard(repo, "Available");
                        AvailableProjectsPanel.Children.Add(card);
                        availableCount++;
                        AddNotification($"   🌐 {repo.Name} (available)");
                    }
                }

                // Mettre à jour les compteurs dans les badges
                AvailableCount.Text = availableCount.ToString();
                InstalledCount.Text = installedCount.ToString();

                AddNotification($"✅ Interface updated! {availableCount} available, {installedCount} installed");
            }
            catch (Exception ex)
            {
                AddNotification($"❌ Error: {ex.Message}");
            }
        }

        private Border CreateProjectCard(Repository repo, string cardType)
        {
            // Carte principale
            Border card = new Border
            {
                Style = (Style)FindResource("ProjectCardStyle"),
                Tag = cardType // "Available" ou "Installed"
            };

            // Grid pour organiser le contenu
            Grid cardGrid = new Grid();
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Contenu principal
            StackPanel contentPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };

            // Icône et nom du projet
            StackPanel headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            TextBlock icon = new TextBlock
            {
                Text = "📁",
                FontSize = 24,
                Margin = new Thickness(0, 0, 10, 0)
            };

            TextBlock nameText = new TextBlock
            {
                Text = repo.Name,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 200
            };

            headerPanel.Children.Add(icon);
            headerPanel.Children.Add(nameText);
            contentPanel.Children.Add(headerPanel);

            // Description
            TextBlock descriptionText = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(repo.Description) ? "No description available" : repo.Description,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 40,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            contentPanel.Children.Add(descriptionText);

            Grid.SetRow(contentPanel, 0);
            cardGrid.Children.Add(contentPanel);

            // Footer avec badge de visibilité
            StackPanel footerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            Border visibilityBadge = new Border
            {
                Background = repo.Private
                    ? new SolidColorBrush(Color.FromRgb(220, 38, 38))
                    : new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4, 8, 4)
            };

            TextBlock visibilityText = new TextBlock
            {
                Text = repo.Private ? "Private" : "Public",
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            };

            visibilityBadge.Child = visibilityText;
            footerPanel.Children.Add(visibilityBadge);

            Grid.SetRow(footerPanel, 1);
            cardGrid.Children.Add(footerPanel);

            card.Child = cardGrid;

            // Événement de clic
            card.MouseLeftButtonDown += (sender, e) => ProjectCard_Click(repo, card);

            return card;
        }

        private async void ProjectCard_Click(Repository repo, Border card)
        {
            string cardType = card.Tag as string;

            // Comportement différent selon le type de carte
            if (cardType == "Installed")
            {
                // Pour les projets installés : ouvrir la page de détails
                ShowDetailView(repo);
            }
            else if (cardType == "Available")
            {
                // Pour les projets disponibles : juste cloner sans lancer
                AddNotification($"\n🔵 Selected project: {repo.Name}");
                AddNotification($"   📝 Description: {repo.Description ?? "No description"}");

                // Modifier l'apparence de la carte pendant le traitement
                var originalBackground = card.Background;
                card.Background = new SolidColorBrush(Color.FromRgb(240, 240, 245));
                card.Cursor = Cursors.Wait;

                try
                {
                    AddNotification($"\n⬇️ Downloading repository '{repo.Name}'...");

                    // Cloner le repository
                    var result = await _serviceGit.CloneOrPullRepositoryAsync(repo.CloneUrl, repo.Name, _githubToken);

                    AddNotification($"{result.Message}\n");

                    if (result.Success)
                    {
                        AddNotification($"✅ Le projet '{repo.Name}' est maintenant disponible dans l'onglet 'Installed'.\n");
                    }
                }
                catch (Exception ex)
                {
                    AddNotification($"❌ Error: {ex.Message}\n");
                }
                finally
                {
                    // Restaurer l'apparence de la carte
                    card.Background = originalBackground;
                    card.Cursor = Cursors.Hand;

                    // Rafraîchir l'interface pour mettre à jour les onglets
                    LoadProjects();
                }
            }
        }

        private async void ShowDetailView(Repository repo)
        {
            // Sauvegarder le repo sélectionné
            _selectedRepository = repo;

            // Remplir les informations de la page de détails
            DetailProjectName.Text = repo.Name;
            DetailProjectDescription.Text = string.IsNullOrWhiteSpace(repo.Description)
                ? "No description available"
                : repo.Description;
            DetailProjectPath.Text = $"Repo/{repo.Name}";

            // Afficher la page de détails et cacher le TabControl
            MainTabControl.Visibility = Visibility.Collapsed;
            DetailView.Visibility = Visibility.Visible;

            AddNotification($"\n📖 Viewing details for: {repo.Name}");

            // Charger les branches Git
            await LoadBranchesAsync();

            // Vérifier le statut Git pour afficher l'indicateur et activer/désactiver le bouton Push
            await CheckGitStatusAsync();
        }

        private async Task LoadBranchesAsync()
        {
            if (_selectedRepository == null)
                return;

            string repoPath = Path.Combine(_repoDirectory, _selectedRepository.Name);

            try
            {
                AddNotification($"🔍 Loading branches...");

                // Récupérer les branches
                var branches = await _serviceGit.GetBranchesAsync(repoPath);

                if (branches.Count == 0)
                {
                    AddNotification($"⚠️ No branches found");
                    return;
                }

                // Désactiver temporairement l'événement SelectionChanged
                _isChangingBranch = true;

                // Préparer les items pour le ComboBox
                var displayItems = branches.Select(b => new BranchDisplayItem
                {
                    Icon = b.IsCurrent ? "✓" : (b.IsRemote ? "🌐" : "🌿"),
                    DisplayName = b.IsCurrent ? $"{b.DisplayName} (current)" : b.DisplayName,
                    BranchName = b.Name,
                    IsCurrent = b.IsCurrent
                }).ToList();

                // Remplir le ComboBox
                BranchSelector.ItemsSource = displayItems;

                // Sélectionner la branche actuelle
                var currentItem = displayItems.FirstOrDefault(i => i.IsCurrent);
                if (currentItem != null)
                {
                    BranchSelector.SelectedItem = currentItem;
                }

                // Réactiver l'événement
                _isChangingBranch = false;

                AddNotification($"✅ {branches.Count} branch(es) loaded");
            }
            catch (Exception ex)
            {
                _isChangingBranch = false;
                AddNotification($"❌ Error loading branches: {ex.Message}");
            }
        }

        private async void BranchSelector_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Ignorer si on est en train de charger les branches
            if (_isChangingBranch)
                return;

            var selectedItem = BranchSelector.SelectedItem as BranchDisplayItem;
            if (selectedItem == null || _selectedRepository == null)
                return;

            // Ignorer si c'est déjà la branche actuelle
            if (selectedItem.IsCurrent)
                return;

            string repoPath = Path.Combine(_repoDirectory, _selectedRepository.Name);

            try
            {
                // Désactiver le bouton Launch pendant le changement
                LaunchProjectButton.IsEnabled = false;
                BranchSelector.IsEnabled = false;

                AddNotification($"\n🔄 Switching to branch '{selectedItem.DisplayName}'...");

                // Changer de branche
                var result = await _serviceGit.CheckoutBranchAsync(repoPath, selectedItem.BranchName);

                AddNotification($"{result.Message}");

                if (result.Success)
                {
                    // Recharger les branches pour mettre à jour l'affichage
                    await LoadBranchesAsync();
                    AddNotification($"✅ Branch switched successfully!\n");

                    // Vérifier à nouveau le statut après changement de branche
                    await CheckGitStatusAsync();
                }
                else
                {
                    // En cas d'échec, remettre la sélection sur la branche actuelle
                    _isChangingBranch = true;
                    var currentItem = (BranchSelector.ItemsSource as System.Collections.Generic.List<BranchDisplayItem>)?
                        .FirstOrDefault(i => i.IsCurrent);
                    if (currentItem != null)
                    {
                        BranchSelector.SelectedItem = currentItem;
                    }
                    _isChangingBranch = false;
                }
            }
            catch (Exception ex)
            {
                AddNotification($"❌ Error: {ex.Message}\n");

                // Remettre la sélection sur la branche actuelle
                _isChangingBranch = true;
                var currentItem = (BranchSelector.ItemsSource as System.Collections.Generic.List<BranchDisplayItem>)?
                    .FirstOrDefault(i => i.IsCurrent);
                if (currentItem != null)
                {
                    BranchSelector.SelectedItem = currentItem;
                }
                _isChangingBranch = false;
            }
            finally
            {
                // Réactiver le bouton Launch
                LaunchProjectButton.IsEnabled = true;
                BranchSelector.IsEnabled = true;
            }
        }

        private async Task CheckGitStatusAsync()
        {
            if (_selectedRepository == null)
                return;

            string repoPath = Path.Combine(_repoDirectory, _selectedRepository.Name);

            try
            {
                // Vérifier si on est en cours de merge
                bool isMerging = _serviceGit.IsMergeInProgress(repoPath);

                if (isMerging)
                {
                    // Merge en cours → afficher un indicateur spécial
                    GitStatusIndicator.Visibility = Visibility.Visible;
                    GitStatusIndicator.Background = new SolidColorBrush(Color.FromRgb(254, 243, 199)); // Jaune clair
                    GitStatusIndicator.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Orange
                    GitStatusText.Text = "Merge in progress - conflicts resolved, ready to complete";
                    GitStatusText.Foreground = new SolidColorBrush(Color.FromRgb(180, 83, 9)); // Orange foncé
                    PushButton.IsEnabled = true;
                    return;
                }

                // Récupérer les fichiers modifiés
                var modifiedFiles = await _serviceGit.GetModifiedFilesAsync(repoPath);

                if (modifiedFiles.Count > 0)
                {
                    // Il y a des changements → afficher l'indicateur
                    GitStatusIndicator.Visibility = Visibility.Visible;
                    GitStatusIndicator.Background = new SolidColorBrush(Color.FromRgb(254, 226, 226)); // Rouge clair
                    GitStatusIndicator.BorderBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Rouge
                    GitStatusText.Text = $"{modifiedFiles.Count} uncommitted change(s)";
                    GitStatusText.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38)); // Rouge foncé
                    PushButton.IsEnabled = true;
                }
                else
                {
                    // Pas de changements → cacher l'indicateur
                    GitStatusIndicator.Visibility = Visibility.Collapsed;
                    PushButton.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                AddNotification($"⚠️ Could not check Git status: {ex.Message}");
            }
        }

        private async void PushButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRepository == null)
                return;

            string repoPath = Path.Combine(_repoDirectory, _selectedRepository.Name);

            try
            {
                // Vérifier si on est en cours de merge
                bool isMerging = _serviceGit.IsMergeInProgress(repoPath);

                if (isMerging)
                {
                    // On est en cours de merge → finaliser directement sans dialogue
                    AddNotification($"\n🔀 Merge in progress detected. Completing merge...");

                    // Désactiver les boutons pendant l'opération
                    PushButton.IsEnabled = false;
                    FetchButton.IsEnabled = false;
                    LaunchProjectButton.IsEnabled = false;
                    BranchSelector.IsEnabled = false;

                    // Finaliser le merge et pousser
                    var result = await _serviceGit.CompleteMergeAndPushAsync(repoPath);

                    AddNotification($"{result.Message}\n");

                    if (result.Success)
                    {
                        // Rafraîchir le statut Git
                        await CheckGitStatusAsync();
                    }

                    // Réactiver les boutons
                    FetchButton.IsEnabled = true;
                    LaunchProjectButton.IsEnabled = true;
                    BranchSelector.IsEnabled = true;

                    return;
                }

                // Pas de merge en cours → comportement normal
                AddNotification($"\n📋 Preparing commit dialog...");

                // Récupérer les fichiers modifiés
                var modifiedFiles = await _serviceGit.GetModifiedFilesAsync(repoPath);

                if (modifiedFiles.Count == 0)
                {
                    AddNotification($"⚠️ No changes to commit.\n");
                    return;
                }

                // Ouvrir le dialogue de commit
                var commitDialog = new CommitDialog(modifiedFiles)
                {
                    Owner = this
                };

                bool? dialogResult = commitDialog.ShowDialog();

                if (dialogResult == true && commitDialog.WasCommitted)
                {
                    // Désactiver les boutons pendant l'opération
                    PushButton.IsEnabled = false;
                    LaunchProjectButton.IsEnabled = false;
                    BranchSelector.IsEnabled = false;

                    AddNotification($"\n🔄 Committing and pushing changes...");
                    AddNotification($"   Title: {commitDialog.CommitTitle}");

                    // Effectuer le commit et le push
                    var result = await _serviceGit.CommitAndPushAsync(
                        repoPath,
                        commitDialog.CommitTitle,
                        commitDialog.CommitDescription
                    );

                    AddNotification($"{result.Message}\n");

                    if (result.Success)
                    {
                        // Rafraîchir le statut Git
                        await CheckGitStatusAsync();
                    }

                    // Réactiver les boutons
                    PushButton.IsEnabled = true;
                    LaunchProjectButton.IsEnabled = true;
                    BranchSelector.IsEnabled = true;
                }
                else
                {
                    AddNotification($"ℹ️ Commit cancelled.\n");
                }
            }
            catch (Exception ex)
            {
                AddNotification($"❌ Error: {ex.Message}\n");

                // Réactiver les boutons en cas d'erreur
                PushButton.IsEnabled = true;
                LaunchProjectButton.IsEnabled = true;
                BranchSelector.IsEnabled = true;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Retour à la vue principale
            DetailView.Visibility = Visibility.Collapsed;
            MainTabControl.Visibility = Visibility.Visible;

            AddNotification($"↩️ Back to projects list\n");
        }

        private async void FetchButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRepository == null)
                return;

            string repoPath = Path.Combine(_repoDirectory, _selectedRepository.Name);

            try
            {
                // Désactiver les boutons pendant l'opération
                FetchButton.IsEnabled = false;
                PushButton.IsEnabled = false;
                LaunchProjectButton.IsEnabled = false;
                BranchSelector.IsEnabled = false;

                AddNotification($"\n🔄 Fetching latest changes from remote...");

                // Effectuer le fetch et le pull
                var result = await _serviceGit.FetchAndPullAsync(repoPath);

                AddNotification($"{result.Message}\n");

                // Rafraîchir les branches et le statut Git (même en cas de conflit)
                await LoadBranchesAsync();
                await CheckGitStatusAsync();

                // Si il y a eu un conflit, informer l'utilisateur
                if (!result.Success && result.Message.Contains("Conflits détectés"))
                {
                    AddNotification($"💡 Après avoir résolu les conflits dans votre IDE, cliquez à nouveau sur 'Fetch' pour rafraîchir le statut.\n");
                }

                // Réactiver les boutons
                FetchButton.IsEnabled = true;
                LaunchProjectButton.IsEnabled = true;
                BranchSelector.IsEnabled = true;

                // PushButton sera réactivé par CheckGitStatusAsync() s'il y a des changements
            }
            catch (Exception ex)
            {
                AddNotification($"❌ Error: {ex.Message}\n");

                // Réactiver les boutons en cas d'erreur
                FetchButton.IsEnabled = true;
                PushButton.IsEnabled = true;
                LaunchProjectButton.IsEnabled = true;
                BranchSelector.IsEnabled = true;
            }
        }

        private async void LaunchProjectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRepository == null)
            {
                AddNotification($"❌ No project selected\n");
                return;
            }

            string repoPath = Path.Combine(_repoDirectory, _selectedRepository.Name);

            AddNotification($"\n🚀 Preparing to launch: {_selectedRepository.Name}");

            try
            {
                // Afficher la fenêtre de dialogue pour choisir l'IDE
                var ideDialog = new IDESelectionDialog
                {
                    Owner = this
                };

                bool? dialogResult = ideDialog.ShowDialog();

                if (dialogResult == true)
                {
                    // Installer les dépendances si demandé
                    if (ideDialog.InstallDependencies)
                    {
                        AddNotification($"\n📦 Installation des dépendances...");
                        AddNotification($"   ⏳ Exécution de setup.cmd (cela peut prendre plusieurs minutes)...\n");

                        var setupResult = await _serviceGit.RunSetupScriptAsync(repoPath);
                        AddNotification($"{setupResult.Message}\n");

                        // Continuer même si l'installation échoue
                        if (!setupResult.Success)
                        {
                            AddNotification($"⚠️ Le projet sera ouvert malgré l'échec de l'installation.\n");
                        }
                    }

                    bool ideOpened = false;
                    string ideName = "";

                    switch (ideDialog.SelectedIDE)
                    {
                        case IDEChoice.VSCode:
                            ideOpened = _serviceGit.OpenInVSCode(repoPath);
                            ideName = "Visual Studio Code";
                            break;

                        case IDEChoice.VisualStudio:
                            ideOpened = _serviceGit.OpenInVisualStudio(repoPath);
                            ideName = "Visual Studio";
                            break;
                    }

                    if (ideOpened)
                    {
                        AddNotification($"🚀 Ouverture du projet dans {ideName}...\n");
                    }
                    else
                    {
                        AddNotification($"⚠️ Impossible d'ouvrir {ideName}. Vérifiez qu'il est bien installé.\n");
                    }
                }
                else
                {
                    AddNotification($"ℹ️ Aucun IDE sélectionné.\n");
                }
            }
            catch (Exception ex)
            {
                AddNotification($"❌ Error: {ex.Message}\n");
            }
        }

        private void AddNotification(string message)
        {
            // Ajouter le message au panneau de notification
            if (!string.IsNullOrWhiteSpace(NotificationTextBlock.Text))
            {
                NotificationTextBlock.Text += "\n";
            }
            NotificationTextBlock.Text += $"[{DateTime.Now:HH:mm:ss}] {message}";

            // Incrémenter le compteur si le panneau est caché
            if (NotificationPanel.Visibility == Visibility.Collapsed)
            {
                _notificationCount++;
                UpdateNotificationBadge();
            }

            // Auto-scroll vers le bas si le panneau est visible
            if (NotificationPanel.Visibility == Visibility.Visible)
            {
                var scrollViewer = FindVisualChild<ScrollViewer>(NotificationPanel);
                scrollViewer?.ScrollToEnd();
            }
        }

        private void UpdateNotificationBadge()
        {
            if (_notificationCount > 0)
            {
                NotificationBadge.Visibility = Visibility.Visible;
                NotificationCount.Text = _notificationCount > 99 ? "99+" : _notificationCount.ToString();
            }
            else
            {
                NotificationBadge.Visibility = Visibility.Collapsed;
            }
        }

        private void ToggleNotification_Click(object sender, RoutedEventArgs e)
        {
            if (NotificationPanel.Visibility == Visibility.Collapsed)
            {
                // Ouvrir le panneau
                NotificationPanel.Visibility = Visibility.Visible;
                _notificationCount = 0;
                UpdateNotificationBadge();

                // Animer l'apparition
                var storyboard = (Storyboard)FindResource("NotificationSlideIn");
                storyboard.Begin(NotificationPanel);

                // Auto-scroll vers le bas
                var scrollViewer = FindVisualChild<ScrollViewer>(NotificationPanel);
                scrollViewer?.ScrollToEnd();
            }
            else
            {
                // Fermer le panneau
                NotificationPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void CloseNotification_Click(object sender, RoutedEventArgs e)
        {
            NotificationPanel.Visibility = Visibility.Collapsed;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Effacer les notifications précédentes
            NotificationTextBlock.Text = string.Empty;
            _notificationCount = 0;
            UpdateNotificationBadge();
            LoadProjects();
        }

        // Helper pour trouver un élément enfant dans l'arbre visuel
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is T)
                {
                    return (T)child;
                }

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }
    }
}
