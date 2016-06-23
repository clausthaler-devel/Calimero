using System;
using System.IO;

namespace Calimero
{
    public class CrpFile
    {
        public string id = "";
        UInt16 version  = 0;
        public string packageName = "";
        public string text = "";
        public string packageAuthor = "";
        public UInt32 packageVersion = 0;
        public string mainAsset = "";


        public bool parse( string fileName )
        {
            try
            {
                Stream stream = new FileStream(fileName, FileMode.Open);
                BinaryReader binaryReader = new BinaryReader(stream);

                id = new string( binaryReader.ReadChars( 4 ) );
                version = binaryReader.ReadUInt16();
                packageName = binaryReader.ReadString();
                string text = binaryReader.ReadString();
                if ( text != "" )
                {
                    //1ColossalFramework.Security.SimpleAES simpleAES = new ColossalFramework.Security.SimpleAES();
                    //packageAuthor = simpleAES.Decrypt( text );
                }
                packageVersion = binaryReader.ReadUInt32();
                mainAsset = binaryReader.ReadString();
                int num = binaryReader.ReadInt32();
                long num2 = binaryReader.ReadInt64();
                int type;

                for ( int i = 0 ; i < num ; i++ )
                {
                    string name = binaryReader.ReadString();
                    string checksum = binaryReader.ReadString();
                    type = binaryReader.ReadInt32();

                    long offset = binaryReader.ReadInt64() + num2 + 5; // m_PackageImplementation.dataOffset;
                    int size = (int)binaryReader.ReadInt64();
                    //Asset asset = new Asset(name, offset, size, checksum, type, false);
                    //this.AddAsset(asset);
                    Console.WriteLine( String.Format( "type{0}, name {0}, asset {1}, checksum {2}, package {3}", type, name, mainAsset, checksum ) );
                }

                ////ColossalFramework.PackagingX.PackageReader reader = new ColossalFramework.PackagingX.PackageReader(stream);

                //////if (!ColossalFramework.Packaging.Package.a .A AssetSerializer.DeserializeHeader(out type, reader))
                //////{
                //////    return null;
                //////}
                ////var goo = reader.ReadString();
                ////string htype = reader.ReadString();
                ////var vtype = Type.GetType(htype);
                //////var hname = reader.ReadString();

                ////if (vtype == typeof(GameObject))
                ////{
                ////    //return PackageDeserializer.DeserializeGameObject(package, reader);
                ////}
                ////if (vtype == typeof(Mesh))
                ////{
                ////    //return PackageDeserializer.DeserializeMesh(package, reader);
                ////}
                ////if (vtype == typeof(Material))
                ////{
                ////    //return PackageDeserializer.DeserializeMaterial(package, reader);
                ////}
                ////if (vtype == typeof(Texture2D) || vtype == typeof(ColossalFramework.Importers.Image))
                ////{
                ////      string name = reader.ReadString();
                ////    bool linear = reader.ReadBoolean();
                ////    int count = reader.ReadInt32();
                ////    byte[] fileByte = reader.ReadBytes(count);
                ////    var bw = new StreamWriter("d:\\img.png");
                ////    bw.Write(fileByte.Select(n=>(char)n).ToArray() );
                ////    bw.Close();
                ////   //< ColossalFramework.Importers.Image image = new ColossalFramework.Importers.Image(fileByte);
                ////   // Texture2D texture2D = image.CreateTexture(linear);
                ////   // texture2D.name = name;
                ////}
                ////if (typeof(ScriptableObject).IsAssignableFrom(vtype))
                ////{
                ////    //return PackageDeserializer.DeserializeScriptableObject(package, vtype, reader);
                ////}
                //////return PackageDeserializer.DeserializeObject(package, vtype, reader);

                stream.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

}



