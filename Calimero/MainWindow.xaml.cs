using System;
using System.Linq;
using System.Windows;
using System.Threading;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Controls;

using PloppableRICO;
using System.IO;

namespace Calimero
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Settings Settings;
        public DocumentManager RicoManager;

        public MainWindow()
        {
            var scr = new MySplashScreen();
            scr.ShowDialog();

            InitializeComponent();
            Settings = Settings.Load();
            RicoManager = new DocumentManager();

            var args = Environment.GetCommandLineArgs();

            if ( args.Count() > 1 )
                RicoManager.LoadDocument( args[1] );
            else
                RicoManager.LoadEmptyDocument();

            RicoManager.CurrentBuildingPropertyChanged += ( Object s, BuildingChangedEventArgs e ) => {
                RefreshBindings();
            };

            RicoManager.ProgressEnd += ( Object sender, ProgressEventArgs e ) =>
            {
                ProgressBar.Value = 0;
                ProgressBar.Visibility = Visibility.Hidden;
            };

            RicoManager.ProgressInit += ( Object sender, ProgressEventArgs e ) =>
            {
                ProgressBar.Visibility = Visibility.Visible;
                ProgressBar.Value = 0;
                ProgressBar.Maximum = e.Max;
                ProgressBar.Minimum = e.Min;
                DoEvents();
            };

            RicoManager.ProgressHop += ( Object sender, ProgressEventArgs e ) =>
            {
                ProgressBar.Visibility = Visibility.Visible;
               ProgressBar.Value = e.Value;
               DoEvents();
            };

            steamDir = SteamUtil.findSteamDir();

            RefreshBindings();
        }


        private delegate void EmptyDelegate();

        protected void DoEvents()
        {
            Dispatcher.CurrentDispatcher.Invoke( DispatcherPriority.Background, new EmptyDelegate( delegate { } ) );
        }

         private DirectoryInfo steamDir;


        private void SetService_Click( object sender, RoutedEventArgs e )
        {
            var rb = (RadioButton)sender;
            var b = (Button)rb.Parent;
            b.BorderThickness = new System.Windows.Thickness( 3 );
        }



        private void FindCommand_CanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            e.CanExecute = 
                (RicoManager != null && RicoManager.CurrentDocument != null && RicoManager.CurrentDocument.sourceFile != null ) ||
                steamDir != null;
        }

        private void FindCommand( object sender, ExecutedRoutedEventArgs e )
        {

            Find();
            RefreshBindings();
        }

        private void FindNextCommand_CanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            e.CanExecute = RicoManager.CurrentDocument.sourceFile != null || steamDir != null;
        }

        private void FindNextCommand( object sender, ExecutedRoutedEventArgs e )
        {
            if ( !RicoManager.FindNextDocument() )
                System.Media.SystemSounds.Beep.Play();
            else
                RefreshBindings();
        }

        private void OpenCommand_CanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            e.CanExecute = true;
        }

        private void OpenCommand( object sender, ExecutedRoutedEventArgs e )
        {
            if ( AskUnsaved() )
                LoadRicoData();
        }

        private void SaveCommand_CanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            e.CanExecute = RicoReady();
        }

        private void SaveCommand( object sender, ExecutedRoutedEventArgs e )
        {
            SaveRicoData();
            RicoManager.CurrentDocument.clean();
        }

        private void SaveAsCommand_CanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            e.CanExecute = RicoReady();
        }

        private void SaveAsCommand( object sender, RoutedEventArgs e )
        {
            SaveRicoData( true );
            RicoManager.CurrentDocument.clean();
        }

        private void ImportCommand_CanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            e.CanExecute = RicoReady();
        }

        private void ImportCommand( object sender, ExecutedRoutedEventArgs e )
        {
            ImportRicoData();
        }

        private void ServiceResidential_Checked( object sender, RoutedEventArgs e )
        {
            SubServiceVisibility( true, false, false );
            LevelsVisibility1To5();
            ValueVisibility( true );
        }

        private void ServiceCommercial_Checked( object sender, RoutedEventArgs e )
        {
            SubServiceVisibility( true, false, false );
            LevelsVisibility1To3();
            ValueVisibility( false );
        }

        private void ServiceOffice_Checked( object sender, RoutedEventArgs e )
        {
            SubServiceVisibility( false, true, false );
            LevelsVisibility1To3();
            ValueVisibility( false );
        }

        private void ServiceProduction_Checked( object sender, RoutedEventArgs e )
        {
            SubServiceVisibility( false, false, true );
            ValueVisibility( false );

            if ( RicoManager.CurrentBuilding.subService == "generic" )
                LevelsVisibility1To3();
            else
                LevelsVisibility1();

            if ( RicoManager.CurrentBuilding.service == "extractor" )
                buttonGeneric.Visibility = Visibility.Hidden;
            else
                buttonGeneric.Visibility = Visibility.Visible;

        }

        private void WindowResize( object sender, SizeChangedEventArgs e )
        {
            PanelMain.Height = this.Height;
        }

        private void SubserviceGeneric_Checked( object sender, RoutedEventArgs e ) { LevelsVisibility1To3(); }

        private void SubserviceNonGeneric_Checked( object sender, RoutedEventArgs e ) { LevelsVisibility1(); }

        private void ExitCommand_CanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            e.CanExecute = true;
        }

        private void ExitCommand( object sender, ExecutedRoutedEventArgs e )
        {
            if ( AskUnsaved() )
            {
                this.Close();
                Environment.Exit( 0 );
            }
        }

        private bool AskUnsaved()
        {
            if ( RicoManager.CurrentDocument == null )
                return true;

            if (  RicoManager.CurrentDocument.isDirty )
            {
                var res = MessageBox.Show("There are unsaved changes. Would you like to save them now?", "Oh my", MessageBoxButton.YesNoCancel);
                if ( res == MessageBoxResult.No )
                    return true;
                else if ( res == MessageBoxResult.Yes )
                {
                    SaveRicoData();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        private void AddBuildingCommand_CanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            e.CanExecute = RicoReady();
        }

        private void AddBuildingCommand( object sender, ExecutedRoutedEventArgs e )
        {
            RicoManager.CurrentDocument.addBuilding();
            RefreshBindings();
        }

        private void RemoveBuildingCommand_CanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            e.CanExecute = RicoReady();
        }

        private void RemoveBuildingCommand( object sender, ExecutedRoutedEventArgs e )
        {
            var index = listboxBuildings.SelectedIndex;
            RicoManager.CurrentDocument.removeBuilding( index );
            RefreshBindings();
        }

        private void RefreshBindings( bool leaveBuildingListAlone = false )
        {
            PanelData.DataContext = null;
            PanelPreview.DataContext = null;
            DataContext = null;

            if ( !leaveBuildingListAlone )
                listboxBuildings.ItemsSource = null;

            PanelData.DataContext = RicoManager.CurrentBuilding;
            PanelPreview.DataContext = RicoManager.CurrentBuilding;

            if ( !leaveBuildingListAlone )
            {
                listboxBuildings.Items.Clear();
                listboxBuildings.ItemsSource = RicoManager.CurrentDocument.Buildings;
            }

            ToggleWarnings( !RicoManager.CurrentDocument.isValid );

            if ( RicoManager.CurrentBuilding.steamId != null && RicoManager.CurrentBuilding.steamId != "" )
            {
                if ( RicoManager.CurrentBuilding.steamData == null )
                    new Thread( () => {
                        SteamAuthor.Dispatcher.Invoke( () => { SteamAuthor.Text = "by " + RicoManager.CurrentBuilding.authorName; } ); 
                    } ).Start();
                else
                    SteamAuthor.Text = "by " + RicoManager.CurrentBuilding.authorName;

                this.DataContext = RicoManager;
            }
        }

        private void ShowBuilding( object sender, SelectionChangedEventArgs e )
        {
            RicoManager.SelectBuilding( listboxBuildings.SelectedIndex );
            RefreshBindings(true);
        }

        private void GotIntValueFocus( object sender, RoutedEventArgs e )
        {
            var tb = (TextBox)sender;
            tb.Text = new System.Text.RegularExpressions.Regex( @"[^\d]" ).Replace( tb.Text, "" );
            tb.SelectionStart = 0;
            tb.SelectionLength = tb.Text.Length;
            lastIntValue = tb.Text;
        }

        private void LostIntValueFocus( object sender, RoutedEventArgs e )
        {
            var tb = (TextBox)sender;
            tb.Text = String.Format( tb.Tag.ToString(), lastIntValue );
        }

        private void MenuHover( object sender, MouseEventArgs e )
        {
            OpenSideBar();
        }

        private void MenuHoverEnd( object sender, MouseEventArgs e )
        {
            CloseSideBarDelayed();
        }

        private void HelpHover( object sender, MouseEventArgs e )
        {
            OpenHelp();
        }

        private void HelpHoverEnd( object sender, MouseEventArgs e )
        {
            CloseHelpDelayed();
        }

        private void MoveWindow( object sender, MouseButtonEventArgs e )
        {
            this.DragMove();
        }

        private void CloseWindow( object sender, RoutedEventArgs e )
        {
            this.Close();
            Environment.Exit( 0 );
        }

        private void MaximizeWindow( object sender, RoutedEventArgs e )
        {
            if ( WindowState == WindowState.Maximized )
            {
                imageWindowState.Source = LoadImage( "pack://application:,,/Resources/Maximize.png" );
                WindowState = WindowState.Normal;
            }
            else
            {
                imageWindowState.Source = LoadImage( "pack://application:,,/Resources/Window.png" );
                WindowState = WindowState.Maximized;
            }
        }

        private void MinimizeWindow( object sender, RoutedEventArgs e )
        {
            WindowState = WindowState.Minimized;
        }

        private bool RicoReady()
        {
            return
                RicoManager == null ? false :
                RicoManager.CurrentDocument == null ? false :
                true;
        }

        private bool SteamIdReady()
        { 
            return RicoManager.CurrentBuilding.steamId != null && RicoManager.CurrentBuilding.steamId != "";
        }

        private void LoadFromWebCommand_CanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            e.CanExecute = RicoReady() && SteamIdReady();
        }

        private void LoadFromWebCommand( object sender, ExecutedRoutedEventArgs e )
        {
            var building = DownloadDefinitions();
            if ( building != null )
            {
                var i = RicoManager.CurrentDocument.Buildings.FindIndex( n => n == RicoManager.CurrentBuilding );
                var b = RicoManager.CurrentBuilding;
                building.crpData = b.crpData;
                RicoManager.CurrentDocument.Buildings[i] = building;
                RicoManager.CurrentBuilding = building;
                RefreshBindings();
            }
        }

        private void SaveToWebCommand_CanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            e.CanExecute = RicoReady() && SteamIdReady();
        }

        private void SaveToWebCommand( object sender, ExecutedRoutedEventArgs e )
        {
            UploadCurrentDefinition();
        }

        private void PreviousPremadeCommand_CanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            e.CanExecute = RicoReady() && RicoManager.CurrentDocument.sourceFile != null;
        }

        private void PreviousPremadeCommand( object sender, ExecutedRoutedEventArgs e )
        {
            if ( AskUnsaved() )
            {
                if ( !RicoManager.LoadPreviousDocument() )
                    System.Media.SystemSounds.Beep.Play();
                else
                    RefreshBindings();
            }
        }

        private void NextPremadeCommand_CanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            e.CanExecute = RicoReady() && RicoManager.CurrentDocument.sourceFile != null;
        }

        private void NextPremadeCommand( object sender, ExecutedRoutedEventArgs e )
        {
            if ( AskUnsaved() )
            {
                if ( !RicoManager.LoadNextDocument() )
                    System.Media.SystemSounds.Beep.Play();
                else
                    RefreshBindings();
            }
        }

        private void SteamLinkClick( object sender, MouseButtonEventArgs e )
        {
            Process.Start( "https://steamcommunity.com/sharedfiles/filedetails/?id=" + RicoManager.CurrentBuilding.steamId );
        }

        private void DiskLinkClick( object sender, MouseButtonEventArgs e )
        {
            Process.Start( "explorer.exe", RicoManager.CurrentDocument.sourceFile.Directory.FullName );
        }

        private void SearchEnter( object sender, KeyEventArgs e )
        {
            if ( e.Key == Key.Enter )
            {
                if ( SearchBox.Text != "" )
                {
                    panelSearch.Visibility = Visibility.Hidden;

                    if ( RicoManager.FindDocument( SearchBox.Text ) )
                    {
                        RefreshBindings();
                        ProgressBar.Visibility = Visibility.Hidden;
                        return;
                    }
                }
                ProgressBar.Visibility = Visibility.Hidden;
            }

            if ( e.Key == Key.Escape )
            {
                panelSearch.Visibility = Visibility.Hidden;
            }
        }
    }
}
