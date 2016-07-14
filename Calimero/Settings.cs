using System;
using System.IO;
using System.Xml.Serialization;
using System.Reflection;

namespace Calimero
{
    public class Settings
    {
        [XmlIgnore]
        public FileInfo settingsFile;

        [XmlElement]
        public string client_id;

        [XmlElement]
        public string remote_host = "http://ricodb.chickenkiller.com";

        public Settings()
        {
            var appData = Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData );
            var ourData = Path.Combine(appData.ToString(), "Calimero");

            if ( !MakePath( ourData ) )
                throw ( new Exception( "Can't write settings (Directory can't be created)" ) );

            var fileName = Path.Combine(ourData, "Calimero.xml");

            settingsFile = new FileInfo( fileName );
        }

        public static Settings Load()
        {
            var settings = new Settings();
            if ( settings.settingsFile.Exists )
                return Load( settings );

            settings.client_id = Guid.NewGuid().ToString();
            return settings.Save();
        }

        public Settings Save()
        {
            try
            {
                var streamWriter = new System.IO.StreamWriter(settingsFile.FullName);
                var xmlSerializer = new XmlSerializer(typeof(Settings));
                xmlSerializer.Serialize( streamWriter, this );
                streamWriter.Close();
                return this;
            }
            catch
            { }

            return null;
        }

        public static Settings Load(Settings settings)
        {
            try
            {
                var streamReader = new System.IO.StreamReader( settings.settingsFile.FullName );
                var xmlSerializer = new XmlSerializer( typeof(Settings) );
                settings = xmlSerializer.Deserialize( streamReader ) as Settings;
                streamReader.Close();
                return settings;
            }
            catch
            {
            }

            return null;
        }

        static bool MakePath( string directory )
        {
            return MakePath( new DirectoryInfo( directory ) );
        }

        static bool MakePath( DirectoryInfo directory )
        {
            if ( directory.Exists )
                return true;

            try
            {
                if ( !directory.Exists && MakePath( directory.Parent ) )
                    directory.Create();
            }
            catch
            {
                return false;
            }

            return true;
        }


    }
}
