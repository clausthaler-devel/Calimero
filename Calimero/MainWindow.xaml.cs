using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading;
using PloppableRICO;

namespace Calimero
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var args = Environment.GetCommandLineArgs();

            if ( args.Count() > 1 )
                LoadRicoData( args[1] );
            else
                CreateEmptyRico();

            buildingDef = ricoDef.Buildings[0];
            this.DataContext = buildingDef;
            UpdateBuildingList();
            StartWatchdogThread();
        }

        private void CreateEmptyRico()
        {
            ricoDef = new PloppableRICODefinition();
            var bdef = new PloppableRICODefinition.Building();
            bdef.name = "* unnamed";
            ricoDef.Buildings.Add( bdef );
            listboxBuildings.Items.Add( "* unnamed" );
        }

        private void SetService_Click( object sender, RoutedEventArgs e )
        {
            var rb = (RadioButton)sender;
            var b = (Button)rb.Parent;
            b.BorderThickness = new System.Windows.Thickness( 3 );
        }

        private void OpenCommand( object sender, RoutedEventArgs e )
        {
            LoadRicoData();
        }


        private void SaveCommand( object sender, RoutedEventArgs e )
        {
            SaveRicoData();
        }

        private void SaveAsCommand( object sender, RoutedEventArgs e )
        {
            SaveRicoData( true );
        }

        private void ImportCommand( object sender, RoutedEventArgs e )
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

            if ( buildingDef.subService == "generic" )
                LevelsVisibility1To3();
            else
                LevelsVisibility1();

            if ( buildingDef.service == "extractor" )
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

        private void AddBuilding_Click( object sender, RoutedEventArgs e )
        {
            var building = ricoDef.AddBuilding();
            this.DataContext = building;
            listboxBuildings.Items.Add( building.name );
        }

        private void RemoveBuilding_Click( object sender, RoutedEventArgs e )
        {
            if ( listboxBuildings.SelectedIndex >= 0 )
            {
                ricoDef.RemoveBuilding( listboxBuildings.SelectedIndex );
                listboxBuildings.Items.RemoveAt( listboxBuildings.SelectedIndex );

                // If the current building is not not in the building list anymore,
                if ( !ricoDef.Buildings.Contains( buildingDef ) )
                {
                    if ( ricoDef.Buildings.Count > 0 )
                    {
                        // (because it got deleted) we must show another building.
                        buildingDef = ricoDef.Buildings[0];
                        listboxBuildings.SelectedIndex = 0;
                    }
                    else
                    {
                        // if the building list is empty, add an empty one
                        AddBuilding_Click( sender, e );
                    }
                }
            }
        }

        private void ShowBuilding( object sender, SelectionChangedEventArgs e )
        {
            if ( listboxBuildings.SelectedIndex < 0 )
                return;

            buildingDef = ricoDef.Buildings[ listboxBuildings.SelectedIndex ];
            this.DataContext = buildingDef;
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

        private void SteamHover( object sender, MouseEventArgs e )
        {
            OpenPreview();

            if ( (!downloadActive) && (previewImage == null) )
                new Thread(
                    () => { DownloadPreviewThread(); }
                ).Start();
        }

        private void SteamHoverEnd( object sender, MouseEventArgs e )
        {
            ClosePreviewDelayed();
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
    }
}
