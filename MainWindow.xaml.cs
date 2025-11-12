using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Octokit;
using Plateforme.Services;

namespace Plateforme
{
    public partial class MainWindow : Window
    {
        private ServiceGitHub _serviceGitHub;
        private ServiceGit _serviceGit;
        private string _githubToken;
        private int _notificationCount = 0;
        private string _repoDirectory;
        private Repository _selectedRepository;

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
                Text = repo.Private ? "🔒 Private" : "🌍 Public",
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

        private void ShowDetailView(Repository repo)
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
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Retour à la vue principale
            DetailView.Visibility = Visibility.Collapsed;
            MainTabControl.Visibility = Visibility.Visible;

            AddNotification($"↩️ Back to projects list\n");
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
