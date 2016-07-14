using System;
using System.Collections.Generic;
using System.IO;
using PloppableRICO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
namespace Calimero
{
    public class DocumentList : List<PloppableRICODefinition>
    {
        public void AddDocument( PloppableRICODefinition document )
        {
            Add( document );
        }
    }
  
    public class DocumentManager
    {
        private List<PloppableRICODefinition> _documents;
        public List<String> LastErrors;
        public PloppableRICODefinition CurrentDocument { get; set; }
        public RICOBuilding CurrentBuilding { get; set; }

        public DocumentManager()
        {
            _documents = new List<PloppableRICODefinition>();
            RICOReader.crpDataProvider = new CrpDataProvider();
        }

        public string CurrentDocumentTitle { get { return DocumentTitle(); } }

        private String DocumentTitle()
        {
            if ( CurrentDocument == null || CurrentDocument.sourceFile == null )
                return "Calimero - (unsaved)";
            
            string[] d =  CurrentDocument.sourceFile.FullName.Split( System.IO.Path.DirectorySeparatorChar );
            return String.Format( "Calimero - ({0})", Path.Combine( "...", d[d.Length - 2].ToString(), d[d.Length - 1].ToString() ) );
        }

        public bool SelectBuilding( int index )
        {
            var b = index > -1 && index < CurrentDocument.Buildings.Count;
            if ( b )
                CurrentBuilding = CurrentDocument.Buildings[index];
            return b;
        }
        
        public bool LoadEmptyDocument()
        {
            CurrentDocument = EmptyDocument();
            CurrentBuilding = CurrentDocument.Buildings[0];
            CurrentBuilding.crpData = new CrpData();
            CurrentBuilding.crpData.Tags = "n/a";
            CurrentBuilding.crpData.Type = "n/a";
            CurrentBuilding.crpData.PreviewImage = CrpDataProvider.LoadImage( @"pack://application:,,/Resources/SorrySmall.png" );
            CurrentBuilding.parent = CurrentDocument;
            CurrentDocument.clean();
            AddDocumentEvents( CurrentDocument );
            return true;
        }

        public bool LoadDocument( string file )
        {
            return LoadDocument( new FileInfo( file ) );
        }

        public bool LoadDocument( FileInfo file )
        {
            PloppableRICODefinition r;
            var ricoFile = Path.Combine( file.Directory.FullName, "PloppableRICODefinition.xml" );
            bool usedDocument = false;

            if ( file.Name.ToLower().EndsWith( ".crp" ) )
            {
                r = UseDocument( ricoFile );

                if ( r != null )
                    usedDocument = true;
                else
                    r = LoadCrpDocument( file );
            }
            else
            {
                r = UseDocument( file );
                if ( r != null )
                    usedDocument = true;
                else
                    r = LoadRicoDocument( file );
            }

            if ( r != null )
            {
                if ( !usedDocument )
                {
                    AddDocumentEvents( r );
                    _documents.Add( r );
                }

                CurrentDocument = r;
                CurrentBuilding = r.Buildings[ r.Buildings.Count - 1 ];
                return true;
            }

            return false;
        }

        private void AddDocumentEvents( PloppableRICODefinition ricoDef)
        {
            ricoDef.BuildingDirtynessChanged += ( object s, BuildingChangedEventArgs e ) => {
                RaiseCurrentBuildingDirtynessChanged( e.building );
            };
            ricoDef.BuildingPropertyChanged += ( object s, BuildingChangedEventArgs e ) => {
                RaiseCurrentBuildingPropertyChanged( e.building );
            };
        }

        private List<FileInfo> searchFiles;
        private int lastFoundIndex;
        private string searchTerm;

        public bool FindDocument( string searchTerm )
        {
            var dir = CurrentDocument.sourceFile != null ?
                      CurrentDocument.sourceFile.Directory.Parent :
                      SteamUtil.findWorkshopDir();
            this.searchTerm = searchTerm.ToLower();
            searchFiles = AllDocumentFilesIn( dir, searchTerm );
            lastFoundIndex = -1;
            RaiseInitProgress( 0, searchFiles.Count );

            return FindNextDocument();
        }

        public bool FindNextDocument()
        {
            if ( searchFiles == null ) return false;

            lastFoundIndex++;

            var cd = CurrentDocument;
            var cb = CurrentBuilding;

            while ( lastFoundIndex < searchFiles.Count )
            {
                if ( LoadDocument( searchFiles[lastFoundIndex] ) )
                {
                    foreach ( var building in CurrentDocument.Buildings )
                    {
                        if (
                            building.name.ToLower().Contains( searchTerm ) ||
                            //building.authorName.ToLower().Contains( searchTerm ) ||
                            ( building.crpData != null && building.crpData.sourceFile.Name.Contains( searchTerm ) )
                        )
                        {
                            CurrentBuilding = building;
                            return true;
                        }
                    }

                }
                lastFoundIndex++;
                RaiseProgress( lastFoundIndex );

            }

            RaiseEndProcess();
            CurrentDocument = cd;
            CurrentBuilding = cb;
            
            return false;
        }

        private List<FileInfo> AllDocumentFilesIn( DirectoryInfo d, string searchTerm )
        {
            var result = new List<FileInfo>();

            foreach ( var dir in d.GetDirectories() )
            {
                var f = new FileInfo( Path.Combine( dir.FullName, "PloppableRICODefinition.xml" ) );
                if ( f.Exists )
                {
                    result.Add( f );
                }
                else
                {
                    f = Util.crpFileIn( d );

                    if ( f != null )
                        result.Add( f );
                }
            }

            return result;
        }

        private PloppableRICODefinition EmptyDocument( string file )
        {
            return EmptyDocument( new FileInfo( file ) );
        }

        private PloppableRICODefinition EmptyDocument( FileInfo file )
        {
            var ricoDef = EmptyDocument();
            ricoDef.sourceFile = file;
            ricoDef.clean();
            return ricoDef;
        }

        private PloppableRICODefinition EmptyDocument()
        {
            var ricoDef = new PloppableRICODefinition();
            ricoDef.addBuilding();
            ricoDef.clean();
            return ricoDef;
        }

        private PloppableRICODefinition LoadCrpDocument( FileInfo file )
        {
            var ricoFile = Path.Combine( file.Directory.FullName, "PloppableRICODefinition.xml" );
            var document = EmptyDocument(  ricoFile );
            var building = document.Buildings[0];

            building.crpData = RICOReader.crpDataProvider.getCrpData( file );
            building.steamDataProvider = new SteamDataProvider();
            building.name = document.Buildings[0].crpData.BuildingName;
            building.clean();
            if (
                document.Buildings[0].crpData.Type == "Prop" ||
                document.Buildings[0].crpData.Type == "Tree" ||
                document.Buildings[0].crpData.Tags.Contains("Intersection") 
            )
                return null;

            return document;
        }

        private PloppableRICODefinition LoadRicoDocument( FileInfo file )
        {
            LastErrors = new List<string>();

            var document = RICOReader.ParseRICODefinition( file.Name, file.FullName, true );
 
            if ( RICOReader.LastErrors.Count > 0 )
            {
                LastErrors = RICOReader.LastErrors;
            }

            foreach ( var b in document.Buildings )
                b.steamDataProvider = new SteamDataProvider();

            return document;
        }

        public bool ImportDocument( FileInfo file )
        {
            LastErrors = new List<string>();
            var document = RICOReader.ParseRICODefinition( file.Name, file.FullName, true );

            if ( RICOReader.LastErrors.Count > 0 )
            {
                LastErrors = RICOReader.LastErrors;
            }
            else
            {
                foreach ( var building in document.Buildings )
                {
                    building.parent = CurrentDocument;
                    CurrentDocument.addBuilding( building );
                }
                CurrentBuilding = document.Buildings[document.Buildings.Count - 1];
            }
            
            return document != null;
        }

        public PloppableRICODefinition UseDocument( string file )
        {
            return UseDocument( new FileInfo( file ) );
        }

        public PloppableRICODefinition UseDocument( FileInfo file )
        {
            return _documents.Find( n => n.sourceFile.FullName == file.FullName );
        }

        public bool LoadPreviousDocument()
        {
            return PreviousPremade( CurrentDocument.sourceFile );
        }

        public bool LoadNextDocument()
        {
            return NextPremade( CurrentDocument.sourceFile ); ;
        }

        private bool PreviousPremade( FileInfo rf )
        {
            return PreviousPremade( rf.Directory );
        }

        private bool NextPremade( FileInfo rf )
        {
            return NextPremade( rf.Directory );
        }

        private bool PreviousPremade( DirectoryInfo d )
        {
            var npd = previousPremadeDir( d );
            if ( npd == null ) return false;
            if ( LoadDocumentIn( npd ) ) return true;
            return PreviousPremade( npd );
        }

        private bool NextPremade( DirectoryInfo d )
        {
            var npd = nextPremadeDir( d );
            if ( npd == null ) return false;
            if ( LoadDocumentIn( npd ) ) return true;
            return NextPremade( npd );
        }

        private bool LoadDocumentIn( DirectoryInfo npd )
        {
            var crpFile = Util.crpFileIn( npd );

            if ( crpFile == null ) return false;

            var ricoFile = Util.ricoFileIn( npd, crpFile );

            return ( ricoFile != null && LoadDocument( ricoFile ) ) || LoadDocument( crpFile );
        }

        private DirectoryInfo previousPremadeDir()
        {
            return previousPremadeDir( CurrentDocument.sourceFile.Directory );
        }

        private DirectoryInfo nextPremadeDir()
        {
            return nextPremadeDir( CurrentDocument.sourceFile.Directory );
        }

        private DirectoryInfo previousPremadeDir( DirectoryInfo assetDirectory )
        {

            DirectoryInfo pd = null;
            DirectoryInfo rd = assetDirectory;
            DirectoryInfo p = rd.Parent;

            foreach ( var d in p.GetDirectories() )
            {
                if ( d.FullName == rd.FullName )
                    return pd;
                pd = d;
            }
            return null;
        }

        private DirectoryInfo nextPremadeDir( DirectoryInfo assetDirectory )
        {
            DirectoryInfo pd = null;
            DirectoryInfo rd = assetDirectory;
            DirectoryInfo p = rd.Parent;

            foreach ( var d in p.GetDirectories() )
            {
                if ( pd != null && pd.FullName == rd.FullName )
                    return d;
                pd = d;
            }
            return null;
        }

        public delegate void ProgressHandler( object sender, ProgressEventArgs e );
        public delegate void BuildingChangedEventHandler( object sender, BuildingChangedEventArgs e );
        public event BuildingChangedEventHandler CurrentBuildingDirtynessChanged;
        public event BuildingChangedEventHandler CurrentBuildingPropertyChanged;
        public event ProgressHandler ProgressInit;
        public event ProgressHandler ProgressHop;
        public event ProgressHandler ProgressEnd;

        public void RaiseCurrentBuildingDirtynessChanged( RICOBuilding building )
        {
            if ( CurrentBuildingDirtynessChanged != null )
            {
                var e = new BuildingChangedEventArgs();
                e.building = building;
                CurrentBuildingDirtynessChanged( this, e );
            }
        }

        public void RaiseCurrentBuildingPropertyChanged( RICOBuilding building )
        {
            if ( CurrentBuildingPropertyChanged != null )
            {
                var e = new BuildingChangedEventArgs();
                e.building = building;
                CurrentBuildingPropertyChanged( this, e );
            }
        }

        public void RaiseProgress(int Value)
        {
            if ( ProgressHop != null )
            {
                var e = new ProgressEventArgs();
                e.Value = Value;
                ProgressHop( this, e );
            }
        }

        public void RaiseInitProgress( int Min, int Max)
        {
            if ( ProgressInit != null )
            {
                var e = new ProgressEventArgs();
                e.Min = Min; e.Max = Max;
                ProgressInit( this, e );
            }
        }

        public void RaiseEndProcess()
        {
            if ( ProgressEnd != null )
                ProgressEnd( this, new ProgressEventArgs() );
        }

    }

    public class ProgressEventArgs : EventArgs
    {
        public int Min;
        public int Max;
        public int Value;
        public string Message;
    }
}
