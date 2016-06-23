using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using Microsoft.Win32;
using System.Net;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Documents;
//using LoadingIndicators.WPF;
using PloppableRICO;

using System.Windows.Media;


namespace Calimero
{
    public partial class MainWindow : Window
    {
        private FileInfo ricoFile;

        private PloppableRICODefinition ricoDef;
        private PloppableRICODefinition.Building buildingDef;
        private Thread WatchdogThread;
        private Thread CloseSideBarThread;
        private Thread ClosePreviewThread;
        private List<String> buildingProblems;
        string lastIntValue;
        private BitmapImage previewImage;
        private bool downloadActive;
        private System.Text.RegularExpressions.Regex reDigit = new System.Text.RegularExpressions.Regex( @"[^\d]" );
        private void StartWatchdogThread()
        {
            WatchdogThread = new Thread( () => Bark() );
            WatchdogThread.IsBackground = true;
            WatchdogThread.Start();
        }

        private void Bark()
        {
            while ( true )
            {
                if ( buildingDef != null )
                    ToggleWarnings( CheckBuildingForProblems() );
                Thread.Sleep( 1500 );
            }
        }

        public void ToggleWarnings( bool toggle )
        {
            lblInfo.Dispatcher.Invoke( () => {
                panelWarning.Visibility = toggle ? Visibility.Visible : Visibility.Hidden;
            } );

            if ( toggle )
                ShowProblems();
        }

        private String ProblemsOverview()
        {
            var problem = "";
            problem = new Regex( @"^.+\(" ).Replace( buildingProblems[0], "" );
            problem = new Regex( @"\).*$" ).Replace( problem, "" );

            if ( buildingProblems.Count() > 1 )
                problem += String.Format( " (+ {0} more.)", buildingProblems.Count() );

            return problem;
        }

        private void ShowProblems()
        {
            lblInfo.Dispatcher.Invoke( () => { lblInfo.Content = ProblemsOverview(); } );
        }

        private void ImportRicoData()
        {
            var dlg = new OpenFileDialog();

            dlg.FileName = "";
            dlg.Filter = "rico files (*.rico;PloppableRICODefinition.xml)|*.rico;PloppableRICODefinition.xml|asset files (*.crp)|*.crp"; // "Rico files (*.rico)|Asset files (*.crp)";

            if ( dlg.ShowDialog() == true )
            {
                List<string> errors = new List<string>();
                var importRicoDef = RICOReader.ParseRICODefinition( "", dlg.FileName, errors );

                if ( errors.Count > 0 )
                {
                    MessageBox.Show(
                        String.Format(
                            "Errors while importing: {0}\r\n( Plus {1} more )",
                            errors[0], errors.Count - 1
                        ),
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error );
                }
                else
                {
                    foreach ( var building in importRicoDef.Buildings )
                        ricoDef.Buildings.Add( building );
                    UpdateBuildingList();
                }
            }
        }

        private void LoadRicoData()
        {
            var errors = new List<String>();
            var dlg = new OpenFileDialog();

            dlg.FileName = "";
            dlg.Filter = "rico files (*.rico;PloppableRICODefinition.xml)|*.rico;PloppableRICODefinition.xml|asset files (*.crp)|*.crp"; // "Rico files (*.rico)|Asset files (*.crp)";

            if ( dlg.ShowDialog() == true )
                ricoFile = new FileInfo( dlg.FileName );
            else
                ricoFile = null;

            if ( ricoFile != null )
                LoadRicoData( ricoFile );

            this.DataContext = buildingDef;
        }

        private void LoadRicoData( FileInfo file )
        {
            LoadRicoData( file.FullName );
        }

        private void LoadRicoData( string file )
        {

            var errors = new List<String>();

            if ( !new FileInfo( file ).Exists ) return;

            if ( file.ToLower().EndsWith( ".crp" ) )
            {
                var crp = new CrpFile();
                var buildingName = "";
                var buildingPackageId = "";

                if ( crp.parse( file ) )
                {
                    buildingName = crp.mainAsset;
                    buildingPackageId = crp.packageName;
                }

                ricoDef = new PloppableRICODefinition();
                buildingDef = new PloppableRICODefinition.Building();
                buildingDef.name = buildingName;
                buildingDef.steamId = buildingPackageId;
                ricoDef.Buildings.Add( buildingDef );

                ricoFile = new FileInfo( System.IO.Path.Combine( new FileInfo( file ).Directory.FullName, "PloppableRICODefinition.xml" ) );
                UpdateCaption( ricoFile );
                UpdateBuildingList();
                previewImage = null;
            }
            else
            {

                ricoDef = RICOReader.ParseRICODefinition( "", file, errors );

                if ( errors.Count > 0 )
                {
                    MessageBox.Show(
                        String.Format(
                            "Errors while loading: {0}\r\n( Plus {1} more )",
                            errors[0], errors.Count - 1
                        ),
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error );

                    UpdateCaption( ricoFile );
                }
                else
                {
                    ricoFile = new FileInfo( file );
                    buildingDef = ricoDef.Buildings[0];
                    UpdateCaption( ricoFile );
                    previewImage = null;
                }

                // see if we can deduce a steam id from the files path
                if ( buildingDef.steamId == "" )
                {
                    var d = ricoFile.FullName.Split( System.IO.Path.DirectorySeparatorChar );
                    var p = d[ d.Count() - 2 ];
                    if ( new Regex( @"^\d+$" ).IsMatch( p ) )
                        buildingDef.steamId = p;
                }

                
                foreach ( var building in ricoDef.Buildings )
                {
                    if ( building.service != "residential" )
                    {
                        var wc = building.workplaces.Count();
                        if ( building.workplaces[wc - 1] < 0 )
                        {
                            var service = Util.ucFirst( buildingDef.service );
                            var subservice = Util.ucFirst( buildingDef.subService );
                            var level = "Level" + buildingDef.level.ToString();

                            if ( service == "Industrial" || service == "Commercial" )
                                subservice = service + subservice;
                            else if ( service == "Extractor" )
                                subservice = "Industrial" + subservice;
                            else if ( service == "Office" )
                                subservice = "None";

                            buildingDef.workplaces = WorkplaceAIHelper.distributeWorkplaceLevels(
                                wc,
                                Util.WorkplaceDistributionOf( service, subservice, level ),
                                new int[] { 0, 0, 0, 0 }
                            );
                        }
                    }
                }
            }

        }

        private void SaveRicoData( bool forceAsking = false )
        {
            if ( ricoFile == null || forceAsking )
            {
                var dlg = new SaveFileDialog();
                dlg.FileName = "PloppableRICODefinition.xml";
                dlg.Filter = "rico files (*.rico;PloppableRICODefinition.xml)|*.rico;PloppableRICODefinition.xml|asset files (*.crp)|*.crp"; // "Rico files (*.rico)|Asset files (*.crp)";
                if ( dlg.ShowDialog() == true )
                    ricoFile = new FileInfo( dlg.FileName );
                else
                    return;
            }
            if ( ricoFile.Exists )
                ricoFile.Delete();

            RicoWriter.saveRicoData( ricoFile.FullName, ricoDef );
            UpdateCaption( ricoFile );
        }

        void UpdateCaption( FileInfo file )
        {
            if ( file == null )
            {
                labelCaption.Content = Title = String.Format( "Calimero - ( {0} )", "no file" );
                return;
            }

            var d = file.FullName.Split( System.IO.Path.DirectorySeparatorChar );
            labelCaption.Content = Title = String.Format( "Calimero - ({0})", Path.Combine( "...", d[d.Count() - 2].ToString(), d[d.Count() - 1].ToString() ) );
        }

        private void LevelsVisibility1()
        {
            buttonLevel1.Visibility = Visibility.Visible;
            buttonLevel2.Visibility = Visibility.Hidden;
            buttonLevel3.Visibility = Visibility.Hidden;
            buttonLevel4.Visibility = Visibility.Hidden;
            buttonLevel5.Visibility = Visibility.Hidden;
        }

        private void LevelsVisibility1To3()
        {
            buttonLevel1.Visibility = Visibility.Visible;
            buttonLevel2.Visibility = Visibility.Visible;
            buttonLevel3.Visibility = Visibility.Visible;
            buttonLevel4.Visibility = Visibility.Hidden;
            buttonLevel5.Visibility = Visibility.Hidden;
        }

        private void LevelsVisibility1To5()
        {
            buttonLevel1.Visibility = Visibility.Visible;
            buttonLevel2.Visibility = Visibility.Visible;
            buttonLevel3.Visibility = Visibility.Visible;
            buttonLevel4.Visibility = Visibility.Visible;
            buttonLevel5.Visibility = Visibility.Visible;
        }

        private void SubServiceVisibility( bool hilo, bool office, bool production )
        {
            panelSubServiceHiLo.Visibility = hilo ? Visibility.Visible : Visibility.Collapsed;
            panelSubServiceOffice.Visibility = office ? Visibility.Visible : Visibility.Collapsed;
            panelSubServiceProduction.Visibility = production ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ValueVisibility( bool residential )
        {
            //PanelHomes.Visibility = residential ? Visibility.Visible : Visibility.Collapsed;
            //PanelJobs.Visibility = residential ? Visibility.Collapsed : Visibility.Visible;
            //PanelDeviation.Visibility = residential ? Visibility.Collapsed : Visibility.Visible;
        }

        private bool CheckBuildingForProblems()
        {
            return CheckBuildingForProblems( buildingDef );
        }

        private bool CheckBuildingForProblems( PloppableRICODefinition.Building building, bool noReset = false )
        {
            var insane = false;

            if ( !noReset )
                buildingProblems = new List<string>();

            insane = !RICOReader.SanitizeRICOBuilding( "ricod", 0, building, buildingProblems );

            return insane;
        }

        private void CloseSideBarDelayed() { CloseSideBarDelayed( 2000 ); }
        private void ClosePreviewDelayed() { ClosePreviewDelayed( 2000 ); }

        private void StopSideBarCloserThread()
        {
            if ( CloseSideBarThread != null && CloseSideBarThread.IsAlive )
                CloseSideBarThread.Abort();
        }

        private void StopPreviewCloserThread()
        {
            if ( ClosePreviewThread != null && ClosePreviewThread.IsAlive )
                ClosePreviewThread.Abort();
        }

        private void CloseSideBarDelayed( int delay )
        {
            StopSideBarCloserThread();
            CloseSideBarThread = new Thread( () => { Thread.Sleep( delay ); CloseSideBar(); } );
            CloseSideBarThread.Start();
        }

        private void ClosePreviewDelayed( int delay )
        {
            StopPreviewCloserThread();
            ClosePreviewThread = new Thread( () => { Thread.Sleep( delay ); ClosePreview(); } );
            ClosePreviewThread.Start();
        }

        private void CloseSideBar()
        {
            PanelBuildings.Dispatcher.Invoke( () => PanelBuildings.Visibility = Visibility.Hidden );
            PanelLabels.Dispatcher.Invoke( () => PanelLabels.Visibility = Visibility.Visible );
        }

        private void ClosePreview()
        {
            PanelPreview.Dispatcher.Invoke( () => PanelPreview.Visibility = Visibility.Hidden );
            PanelLabels.Dispatcher.Invoke( () => PanelLabels.Visibility = Visibility.Visible );
        }

        private void OpenSideBar()
        {
            StopPreviewCloserThread();
            StopSideBarCloserThread();
            PanelBuildings.Visibility = Visibility.Visible;
            PanelPreview.Visibility = PanelLabels.Visibility = Visibility.Hidden;
        }

        private void OpenPreview()
        {
            StopPreviewCloserThread();
            StopSideBarCloserThread();
            PanelPreview.Visibility = Visibility.Visible;
            PanelBuildings.Visibility = PanelLabels.Visibility = Visibility.Hidden;
        }

        private void UpdateBuildingList()
        {
            listboxBuildings.Items.Clear();
            foreach ( var b in ricoDef.Buildings )
                listboxBuildings.Items.Add( b.name == "" ? "*unnamed" : b.name );
        }

        private void OnlyIntInput( object sender, KeyEventArgs e )
        {
            var tb = (TextBox)sender;
            var text = tb.Text;
            var keycode = Convert.ToInt32(e.Key);

            if ( e.Key == Key.Enter )
            {
                tb.Text = reDigit.Replace( text, "" );
                lastIntValue = tb.Text;
                tb.Text = String.Format( tb.Tag.ToString(), lastIntValue );
                Keyboard.ClearFocus();
            }
            else if ( e.Key == Key.Tab )
            {
                tb.Text = reDigit.Replace( text, "" );
                lastIntValue = tb.Text;
            }
            else if ( e.Key == Key.Escape )
            {
                listboxBuildings.Focus();
                tb.Text = String.Format( tb.Tag.ToString(), lastIntValue );
            }
            else if ( ( keycode < 34 || keycode > 43 ) )
            {
                System.Media.SystemSounds.Beep.Play();
                e.Handled = true;
            }
        }

        private void DownloadPreviewThread()
        {
            downloadActive = true;
            ImagePreview.Dispatcher.Invoke( () => { ImagePreview.Source = LoadImage( @"pack://application:,,/Resources/Loading.png" ); } );
            Thread.Sleep( 1000 );
            DownloadPreview( buildingDef.steamId );
        }

        private void DownloadPreview( string id )
        {
            if ( previewImage != null )
                return;

            if ( id != null && id != "" )
            {
                Steam.Workshop.FileInfo i;

                try
                {
                    i = new Steam.Workshop.ItemParser().ItemOf( id );
                    i.FileID = id;
                }
                catch
                {
                    downloadActive = false;
                    return;
                }

                var wc = new WebClient();
                wc.DownloadDataCompleted += ( object s, DownloadDataCompletedEventArgs ea ) => {
                    downloadActive = false;
                    previewImage = LoadImage( ea.Result );
                    ImagePreview.Dispatcher.Invoke( () => { ImagePreview.Source = previewImage; } );

                    // God, is that ugly
                    textboxDescription.Dispatcher.Invoke( () => {
                        var tb = new TextBlock();
                        var b = new Bold();
                        var it = new Italic();
                        textboxDescription.Inlines.Clear();
                        b.Inlines.Add( new Run( i.Title ) );
                        textboxDescription.Inlines.Add( b );
                        textboxDescription.Inlines.Add( new Run( "\r\nby\r\n " ) );
                        it.Inlines.Add( new Run( i.Author ) );
                        textboxDescription.Inlines.Add( it );
                        textboxDescription.Inlines.Add( new Run( " " ) );
                        var h = new Hyperlink() { NavigateUri = new Uri("https://steamcommunity.com/sharedfiles/filedetails/?id=" + i.FileID) };
                        h.Inlines.Add( "\r\n(on Steam)" );
                        h.RequestNavigate += ( object o, System.Windows.Navigation.RequestNavigateEventArgs e ) => {
                            System.Diagnostics.Process.Start( e.Uri.ToString() );
                        };
                        textboxDescription.Inlines.Add( h );
                    } );
                    
                    
                };
                wc.DownloadDataAsync( new Uri( i.PreviewImageSrc + "?output-format=png&fit=inside|190:120" ) ); //, @"d:\b.png" );
            }
            else
            {
                textboxDescription.Dispatcher.Invoke( () => {

                    previewImage = LoadImage( @"pack://application:,,/Resources/SorrySmall.png" );
                    ImagePreview.Dispatcher.Invoke( () => { ImagePreview.Source = previewImage; ImagePreview.Stretch = Stretch.None; } );

                    var tb = new TextBlock();
                    var b = new Bold();
                    var it = new Italic();
                    textboxDescription.Inlines.Clear();
                    b.Inlines.Add( new Run( "Unknown Steam-ID" ) );
                    it.Inlines.Add( b );
                    textboxDescription.Inlines.Add( it );
                    downloadActive = false;
                } );
            }
        }

        private BitmapImage LoadImage( string url )
        {
            try
            {
                var i = new BitmapImage();
                i.BeginInit();
                i.UriSource = new Uri( url );
                i.EndInit();
                i.Freeze();
                return i;
            }
            catch
            {
                return null;
            }
        }

        private static BitmapImage LoadImage( byte[] imageData )
        {
            try
            {
                var i = new BitmapImage();
                i.BeginInit();
                i.StreamSource = new System.IO.MemoryStream( imageData );
                i.EndInit();
                i.Freeze();
                return i;
            }
            catch
            {
                return null;
            }
        }

    }

    public class FormatIntConverter : IValueConverter
    {
        public String FieldName;

        public object Convert( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            var s = parameter.ToString();
            return parameter.ToString() == "" ? value : String.Format( s, value );
        }

        public object ConvertBack( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            return System.Convert.ToInt32(
                    new System.Text.RegularExpressions
                    .Regex( @"[^\d]+" )
                    .Replace( value.ToString(), "" ) );
        }
    }

    public class IsTrueConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            return (bool) value;
        }

        public object ConvertBack( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            return (bool) value;
        }
    }

    public class IsFalseConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            return !(bool) value;
        }

        public object ConvertBack( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            return !(bool) value;
        }
    }

    public class IntEqualsConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            return System.Convert.ToInt32( parameter.ToString() ) == System.Convert.ToUInt32( value.ToString() );
        }

        public object ConvertBack( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            return (bool) value ? System.Convert.ToInt32( parameter.ToString() ) : 0;
        }
    }

    public class StringMatchConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            return parameter.ToString().Split( '|' ).Contains( value.ToString() );
        }

        public object ConvertBack( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            return (bool) value ? parameter.ToString() : "";
        }
    }
}
