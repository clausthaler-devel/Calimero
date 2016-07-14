using Microsoft.Win32;
using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

using PloppableRICO;
using CRPTools;

namespace Calimero
{

    public static class CalimeroCommands
    {
        public static readonly RoutedUICommand ExitCommand = new RoutedUICommand
        (
            "ExitCommand",
            "ExitCommand",
            typeof(CalimeroCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.F4, ModifierKeys.Alt)
            }
        );

        public static readonly RoutedUICommand OpenCommand = new RoutedUICommand
        (
            "OpenCommand",
            "OpenCommand",
            typeof(CalimeroCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.O, ModifierKeys.Control)
            }
        );

        public static readonly RoutedUICommand ImportCommand = new RoutedUICommand
        (
            "OpenCommand",
            "OpenCommand",
            typeof(CalimeroCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.I, ModifierKeys.Control)
            }
        );

        public static readonly RoutedUICommand AddBuildingCommand = new RoutedUICommand
        (
            "AddBuildingCommand",
            "AddBuildingCommand",
            typeof(CalimeroCommands)
        );

        public static readonly RoutedUICommand RemoveBuildingCommand = new RoutedUICommand
        (
            "RemoveBuildingCommand",
            "RemoveBuildingCommand",
            typeof(CalimeroCommands)
        );

        public static readonly RoutedUICommand SaveCommand = new RoutedUICommand
        (
            "SaveCommand",
            "SaveCommand",
            typeof(CalimeroCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.S, ModifierKeys.Control)
            }
        );

        public static readonly RoutedUICommand SaveAsCommand = new RoutedUICommand
        (
            "SaveAsCommand",
            "SaveAsCommand",
            typeof(CalimeroCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.S, ModifierKeys.Alt ^ ModifierKeys.Control)
            }
        );

        public static readonly RoutedUICommand SaveToWebCommand = new RoutedUICommand
        (
            "SaveToWebCommand",
            "SaveToWebCommand",
            typeof(CalimeroCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.U, ModifierKeys.Control)
            }
        );

        public static readonly RoutedUICommand LoadFromWebCommand = new RoutedUICommand
        (
            "LoadFromWebCommand",
            "LoadFromWebCommand",
            typeof(CalimeroCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.D, ModifierKeys.Control)
            }
        );

        public static readonly RoutedUICommand PreviousPremadeCommand = new RoutedUICommand
        (
            "PreviousPremadeCommand",
            "PreviousPremadeCommand",
            typeof(CalimeroCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.Left, ModifierKeys.Control)
            }
        );

        public static readonly RoutedUICommand NextPremadeCommand = new RoutedUICommand
        (
            "NextPremadeCommand",
            "NextPremadeCommand",
            typeof(CalimeroCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.Right, ModifierKeys.Control)
            }
        );

        public static readonly RoutedUICommand FindCommand = new RoutedUICommand
        (
            "FindCommand",
            "FindCommand",
            typeof(CalimeroCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.F, ModifierKeys.Control)
            }
        );

        public static readonly RoutedUICommand FindNextCommand = new RoutedUICommand
        (
            "FindNextCommand",
            "FindNextCommand",
            typeof(CalimeroCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.F3)
            }
        );

    }

    public partial class MainWindow : Window
    {
        private Thread CloseSideBarThread;
        private Thread ClosePreviewThread;
        string lastIntValue;
     

        //private bool downloadActive = false;
        private System.Text.RegularExpressions.Regex reDigit = new System.Text.RegularExpressions.Regex( @"[^\d]" );


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
            if ( RicoManager.CurrentBuilding.errors.Count > 0 )
            {
                problem = new Regex( @"^.+\(" ).Replace( RicoManager.CurrentBuilding.errors[0], "" );
                problem = new Regex( @"\).*$" ).Replace( problem, "" );
                problem = new Regex( @"\. " ).Replace( problem, ".\r\n", 1 );
                if ( RicoManager.CurrentBuilding.errors.Count() > 1 )
                    problem += String.Format( " (+ {0} more.)", RicoManager.CurrentBuilding.errors.Count() );
            }
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
            dlg.Multiselect = true;
            if ( dlg.ShowDialog() == true )
                foreach ( var fileName in dlg.FileNames )
                    RicoManager.ImportDocument( new FileInfo( fileName ) );

            RefreshBindings();
        }

        private void LoadRicoData(bool noProps = false)
        {
            var errors = new List<String>();
            var dlg = new OpenFileDialog();

            dlg.FileName = "";
            dlg.Filter = "rico files (*.rico;PloppableRICODefinition.xml)|*.rico;PloppableRICODefinition.xml|asset files (*.crp)|*.crp"; // "Rico files (*.rico)|Asset files (*.crp)";

            if ( dlg.ShowDialog() == true )
                RicoManager.LoadDocument( dlg.FileName );
            else
                return;

            RefreshBindings();
        }

        private void Find()
        {
            if ( panelSearch.Visibility == Visibility.Visible )
            {
                panelSearch.Visibility = Visibility.Hidden;
            }
            else
            {
                panelSearch.Visibility = Visibility.Visible;
                SearchBox.Focus();
                SearchBox.SelectionStart = 0;
                SearchBox.SelectionLength = SearchBox.Text.Length;
            }
            //if ( dlg.ShowDialog() == true )
            //    RicoManager.FindDocument( dlg.searchTerm );
        }


        private void SaveRicoData( bool forceAsking = false )
        {
            FileInfo ricoFile = null;

            if ( RicoManager.CurrentDocument.sourceFile == null || forceAsking )
            {
                var dlg = new SaveFileDialog();
                dlg.FileName = "RICODefinition.xml";
                dlg.Filter = "rico files (*.rico;RICODefinition.xml)|*.rico;RICODefinition.xml|asset files (*.crp)|*.crp"; // "Rico files (*.rico)|Asset files (*.crp)";
                if ( dlg.ShowDialog() == true )
                    ricoFile = new FileInfo( dlg.FileName );
                else
                    return;
            }

            foreach ( var building in RicoManager.CurrentDocument.Buildings )
                if ( building.dbKey != 0 )
                {
                    postUse( building );
                    building.dbKey = 0;
                }

            if ( !RicoWriter.saveRicoData( ricoFile != null ? ricoFile.FullName : RicoManager.CurrentDocument.sourceFile.FullName, RicoManager.CurrentDocument ) )
                MessageBox.Show( "Cannot save rico file.", "Oops", MessageBoxButton.OK );
        }

        void postUse( RICOBuilding building )
        {
            var client = new WebClient();
            var url = String.Format("{0}", Settings.remote_host );
            var args = new NameValueCollection() {{ "db-key", building.dbKey.ToString() }};

            try
            {
                byte[] response = client.UploadValues( url, "PUT", args );
                string result = System.Text.Encoding.UTF8.GetString(response);
            }
            catch // ( Exception e )
            {
                // MessageBox.Show( "Upload failed.\r\n" + e.Message, "Calimero - Error", MessageBoxButton.OK, MessageBoxImage.Error );
            }
        } 

        //void UpdateCaption( FileInfo file )
        //{
        //    if ( file == null )
        //    {
        //        labelCaption.Content = Title = String.Format( "Calimero - ( {0} )", "no file" );
        //        return;
        //    }

        //    var d = file.FullName.Split( System.IO.Path.DirectorySeparatorChar );
        //    labelCaption.Content = Title = String.Format( "Calimero - ({0})", Path.Combine( "...", d[d.Count() - 2].ToString(), d[d.Count() - 1].ToString() ) );
        //}

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

        private void CloseSideBarDelayed() { CloseSideBarDelayed( 2000 ); }

        private void CloseHelpDelayed() { ClosePreviewDelayed( 2000 ); }

        private void StopSideBarCloserThread()
        {
            if ( CloseSideBarThread != null && CloseSideBarThread.IsAlive )
                CloseSideBarThread.Abort();
        }

        private void StopHelpCloserThread()
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
            StopHelpCloserThread();
            ClosePreviewThread = new Thread( () => { Thread.Sleep( delay ); CloseHelp(); } );
            ClosePreviewThread.Start();
        }

        private void CloseSideBar()
        {
            PanelBuildings.Dispatcher.Invoke( () => PanelBuildings.Visibility = Visibility.Hidden );
            PanelHelp.Dispatcher.Invoke( () => PanelHelp.Visibility = Visibility.Hidden );
            PanelPreview.Dispatcher.Invoke( () => PanelPreview.Visibility = Visibility.Visible );
        }

        private void CloseHelp()
        {
            PanelPreview.Dispatcher.Invoke( () => PanelPreview.Visibility = Visibility.Visible );
            PanelHelp.Dispatcher.Invoke( () => PanelHelp.Visibility = Visibility.Hidden );
            PanelBuildings.Dispatcher.Invoke( () => PanelBuildings.Visibility = Visibility.Hidden );
        }

        private void OpenSideBar()
        {
            StopHelpCloserThread();
            StopSideBarCloserThread();
            PanelBuildings.Visibility = Visibility.Visible;
            PanelHelp.Visibility = PanelPreview.Visibility = Visibility.Hidden;
        }

        private void OpenHelp()
        {
            StopHelpCloserThread();
            StopSideBarCloserThread();
            PanelHelp.Visibility = Visibility.Visible;
            PanelBuildings.Visibility = PanelPreview.Visibility = Visibility.Hidden;
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
            return LoadImage( new System.IO.MemoryStream( imageData ) );
        }

        private static BitmapImage LoadImage( Stream stream )
        {
            try
            {
                var i = new BitmapImage();
                i.BeginInit();
                i.StreamSource = stream;
                i.EndInit();
                i.Freeze();
                return i;
            }
            catch
            {
                return null;
            }
        }

        private void UploadCurrentDefinition()
        {
            UploadDefinition( RicoManager.CurrentDocument );
        }

        private void UploadDefinition( PloppableRICODefinition ricoDef )
        {
            var client = new WebClient();
            var ricoXml = "";

            try
            {
                var ms = new MemoryStream();
                ricoDef.Buildings[0].story = "666";
                var xmlSerializer = new XmlSerializer(typeof(PloppableRICODefinition));
                xmlSerializer.Serialize( ms, ricoDef );
                ms.Seek( 0, SeekOrigin.Begin );
                ricoXml = new StreamReader( ms ).ReadToEnd();
            }
            catch
            { }

            var url = String.Format("{0}", Settings.remote_host );
            var args = new NameValueCollection() {
                { "rico-definition", ricoXml },
                { "client-id", Settings.client_id }
            };

            try
            {
                byte[] response = client.UploadValues( url, "POST", args );
                string result = System.Text.Encoding.UTF8.GetString(response);

                if ( result == "1" )
                    MessageBox.Show( "Upload successful.", "Calimero", MessageBoxButton.OK, MessageBoxImage.Information );
                else
                    throw ( new Exception( "Database-Error" ) );
            }
            catch ( Exception e )
            {
                MessageBox.Show("Upload failed.\r\n" + e.Message, "Calimero - Error", MessageBoxButton.OK, MessageBoxImage.Error );
            }
        }

        private RICOBuilding DownloadDefinitions()
        {
            
            return DownloadDefinitions( RicoManager.CurrentBuilding.steamId );
        }

        private RICOBuilding DownloadDefinitions( string steamID )
        {
            var wc = new WebClient();
            try
            {
                var response = wc.DownloadData( "http://ricodb.chickenkiller.com/" + steamID );
                var ricoXml = System.Text.Encoding.UTF8.GetString( response );
                var ricoData = RICOReader.ParseRICODefinition(steamID, new MemoryStream(response));
                var dlg = new DownloadDefinitionDialog();
                var res = dlg.ShowDialog(this, ricoData.Buildings);
                if ( res.HasValue && res.Value )
                    return dlg.selectedBuilding;
                else
                    return null;
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

    public class NotNullOrEmptyConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            if ( value == null )
                return false;

            try
            {
                var v = (string)value;
                return v != "";
            }
            catch
            {
                return true;
            }
        }

        public object ConvertBack( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            return "";
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
