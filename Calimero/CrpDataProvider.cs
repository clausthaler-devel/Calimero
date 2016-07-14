using System;
using CRPTools;
using System.IO;
using System.Linq;
using PloppableRICO;
using System.Windows.Media.Imaging;

namespace Calimero
{
    class CrpDataProvider : ICrpDataProvider
    {
        public CrpData getCrpData( string file )
        {
            return getCrpData( new FileInfo( file ) );
        }

        public CrpData getCrpData( FileInfo file )
        {
            var crp = CrpDeserializer.parseFile( file );
            var res = new CrpData();

            if ( crp == null )
                return null;

            res.BuildingName = crp.header.mainAssetName;
            res.AuthorName = "";
            res.AuthorID = crp.header.authorName;
            res.SteamId = crp.header.packageName;
            res.sourceFile = file;

            if ( crp.metadata == null )
            {
                res.Tags = "n/a";
                res.Type = "n/a";
                res.PreviewImage = LoadImage( @"pack://application:,,/Resources/SorrySmall.png" );

            }
            else
            {
                res.Tags = String.Join(", ", crp.metadata["steamTags"]);
                res.Type = crp.metadata["type"].ToString();
                res.PreviewImage = crp.Images == null || crp.Images.Count == 0 ?
                                    LoadImage( @"pack://application:,,/Resources/SorrySmall.png" ) :
                                    LoadImage( crp.Images[crp.Images.Keys.First()] );
            }

            return res;
        }

        public static BitmapImage LoadImage( string url )
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

        public static BitmapImage LoadImage( byte[] imageData )
        {
            return LoadImage( new System.IO.MemoryStream( imageData ) );
        }

        public static BitmapImage LoadImage( Stream stream )
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
    }
}
