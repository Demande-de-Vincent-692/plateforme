using System;
using System.Collections.Generic;
using System.Windows;
using Plateforme.Services;

namespace Plateforme
{
    public partial class CommitDialog : Window
    {
        public string CommitTitle { get; private set; }
        public string CommitDescription { get; private set; }
        public bool WasCommitted { get; private set; }

        public CommitDialog(List<GitModifiedFile> modifiedFiles)
        {
            InitializeComponent();

            // Initialiser la liste des fichiers
            FilesList.ItemsSource = modifiedFiles;
            FileCountText.Text = $"{modifiedFiles.Count} file(s) changed";

            WasCommitted = false;

            // Ajouter un handler pour le compteur de caractères
            CommitTitleTextBox.TextChanged += (s, e) =>
            {
                int charCount = CommitTitleTextBox.Text.Length;
                CharCountText.Text = $"{charCount}/72 characters";

                // Changer la couleur si on dépasse 50 caractères (avertissement GitHub)
                if (charCount > 50)
                {
                    CharCountText.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(239, 68, 68)); // Rouge
                }
                else
                {
                    CharCountText.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(107, 114, 128)); // Gris
                }
            };

            // Focus sur le champ de titre
            CommitTitleTextBox.Focus();
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {
            // Valider que le titre n'est pas vide
            if (string.IsNullOrWhiteSpace(CommitTitleTextBox.Text))
            {
                MessageBox.Show(
                    "Please enter a commit title.",
                    "Missing Title",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            // Récupérer les valeurs
            CommitTitle = CommitTitleTextBox.Text.Trim();
            CommitDescription = CommitDescriptionTextBox.Text.Trim();
            WasCommitted = true;

            // Fermer le dialogue avec succès
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            WasCommitted = false;
            DialogResult = false;
            Close();
        }
    }
}
