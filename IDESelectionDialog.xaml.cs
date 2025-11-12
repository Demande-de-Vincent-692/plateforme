using System.Windows;

namespace Plateforme
{
    /// <summary>
    /// Choix de l'IDE pour ouvrir le projet
    /// </summary>
    public enum IDEChoice
    {
        None,
        VSCode,
        VisualStudio
    }

    /// <summary>
    /// Fenêtre de dialogue pour choisir l'IDE
    /// </summary>
    public partial class IDESelectionDialog : Window
    {
        public IDEChoice SelectedIDE { get; private set; }
        public bool InstallDependencies { get; private set; }

        public IDESelectionDialog()
        {
            InitializeComponent();
            SelectedIDE = IDEChoice.None;
            InstallDependencies = false;
        }

        private void VSCodeButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedIDE = IDEChoice.VSCode;
            InstallDependencies = InstallDependenciesCheckBox.IsChecked ?? false;
            DialogResult = true;
            Close();
        }

        private void VisualStudioButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedIDE = IDEChoice.VisualStudio;
            InstallDependencies = InstallDependenciesCheckBox.IsChecked ?? false;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedIDE = IDEChoice.None;
            DialogResult = false;
            Close();
        }
    }
}
