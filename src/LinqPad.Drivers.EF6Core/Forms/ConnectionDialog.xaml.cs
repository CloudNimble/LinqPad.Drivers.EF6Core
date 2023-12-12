using System;
using System.Data.Entity;
using System.IO;
using System.Windows;
using LINQPad.Extensibility.DataContext;
using LINQPad.Extensibility.DataContext.UI;
using Microsoft.Win32;
using ModernWpf;

namespace CloudNimble.LinqPad.Drivers.EF6Core
{

    /// <summary>
    /// Interaction logic for ConnectionDialog.xaml
    /// </summary>
    public partial class ConnectionDialog : Window
    {

        #region Private Members

        readonly IConnectionInfo _cxInfo;

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cxInfo"></param>
        public ConnectionDialog(IConnectionInfo cxInfo)
        {
            _cxInfo = cxInfo;
            DataContext = cxInfo;
            InitializeComponent();
            ThemeManager.Current.AccentColor = System.Windows.Media.Colors.OrangeRed;
        }

        #endregion

        #region Pickers

        void BrowseAppConfig(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Choose application config file",
                Filter = "AppSettings files (*.json)|*.json|.NET Config files (*.config)|*.config|All files (*.*)|*.*",
            };

            if (dialog.ShowDialog() == true)
                _cxInfo.AppConfigPath = dialog.FileName;
        }

        void BrowseAssembly(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select the Assembly with your custom DbContext",
                Filter = ".NET Assemblies (*.dll, *.exe)|*.dll;*.exe",
            };

            if (dialog.ShowDialog() == true)
                _cxInfo.CustomTypeInfo.CustomAssemblyPath = dialog.FileName;
        }

        void ChooseType(object sender, RoutedEventArgs e)
        {
            var assemPath = _cxInfo.CustomTypeInfo.CustomAssemblyPath;
            if (assemPath.Length == 0)
            {
                MessageBox.Show("First enter a path to an assembly.");
                return;
            }

            if (!File.Exists(assemPath))
            {
                MessageBox.Show("File '" + assemPath + "' does not exist.");
                return;
            }

            string[] customTypes;
            try
            {
                customTypes = _cxInfo.CustomTypeInfo.GetCustomTypesInAssembly("System.Data.Entity.DbContext");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error obtaining custom types: " + ex.Message);
                return;
            }

            if (customTypes.Length == 0)
            {
                MessageBox.Show("There are no public types based on \"System.Data.Entity.DbContext\" in that assembly.");
                return;
            }

            var result = (string)Dialogs.PickFromList("Choose Custom Type", customTypes);
            if (result != null) _cxInfo.CustomTypeInfo.CustomTypeName = result;
        }

        #endregion

        #region Dialog Buttons

        void CreateConnection(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        void TestConnection(object sender, RoutedEventArgs e)
        {
            var assembly = DataContextDriver.LoadAssemblySafely(_cxInfo.CustomTypeInfo.CustomAssemblyPath);
            if (assembly is null)
            {
                MessageBox.Show($"Could not load assembly '{_cxInfo.CustomTypeInfo.CustomAssemblyPath}'.");
                return;
            }

            var dbContextType = assembly.GetType(_cxInfo.CustomTypeInfo.CustomTypeName);
            if (dbContextType is null)
            {
                MessageBox.Show($"Could not find type '{_cxInfo.CustomTypeInfo.CustomTypeName}' in assembly '{_cxInfo.CustomTypeInfo.CustomAssemblyPath}'.");
                return;
            }

            DbContext instance;
            if (string.IsNullOrEmpty(_cxInfo.DatabaseInfo.CustomCxString))
            {
                instance = (DbContext)Activator.CreateInstance(dbContextType);
            }
            else
            {
                instance = (DbContext)Activator.CreateInstance(dbContextType, _cxInfo.DatabaseInfo.CustomCxString);
            }

            try
            {
                instance.Database.Connection.Open();
                instance.Database.Connection.Close();
                MessageBox.Show("Congratulations! Your connection appears to be configured properly.", "Connection Test Result", MessageBoxButton.OK);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dang it! Something didn't work. Attempting to open the connection returned the following error: {Environment.NewLine}{Environment.NewLine}{ex.Message}",
                    "Connection Test Result", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

    }

}
