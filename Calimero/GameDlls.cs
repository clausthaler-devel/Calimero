using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Diagnostics;
using System.Threading;

namespace Calimero
{
    class GameDlls
    {
        public static DirectoryInfo steamDir;
        public static DirectoryInfo skylinesDir;
        public static DirectoryInfo skylinesDllDir;
        public static FileInfo calimeroExe;
        public static string error;

        public static string[] GameDllFileNames = new string[] {
            "Assembly-CSharp-firstpass.dll",
            "Assembly-CSharp.dll",
            "ColossalManaged.dll",
            "ICities.dll",
            "ICSharpCode.SharpZipLib.dll",
            "UnityEngine.dll",
        };

        public static void CopyGameDlls()
        {
            new Thread( () => { CopyGameDllsThread(); } ).Start();
        }

        private static void CopyGameDllsThread()
        {
            steamDir = SteamUtil.findSteamDir();
            skylinesDir = new DirectoryInfo( Path.Combine( steamDir.FullName, "SteamApps", "common", "Cities_Skylines" ) );
            skylinesDllDir = new DirectoryInfo( Path.Combine( skylinesDir.FullName, "Cities_Data", "Managed" ) );
            calimeroExe = new FileInfo( System.Reflection.Assembly.GetExecutingAssembly().Location );

            if ( HadToCopyGameDlls() )
            {
                if ( error == "" )
                    MessageBox.Show( error, "Error", MessageBoxButton.OK, MessageBoxImage.Error );
                else
                {
                    MessageBox.Show( "Game-Dlls had to be copied. Calimero must be restarted", "Bab-ba-da-bu-bap", MessageBoxButton.OK, MessageBoxImage.Information );
                    Process.Start( calimeroExe.FullName );
                }
                Environment.Exit( 0 );
            }
        }


        private static bool HadToCopyGameDlls()
        {
            if ( steamDir == null )
            {
                error = "Steam not found.";
                return true;
            }

            if ( !skylinesDir.Exists )
            {
                error = "Cities: Skylines not found.";
                return true;
            }

            if ( !skylinesDllDir.Exists )
            {
                error = "Cities: Skylines DLLs not found.";
                return true;
            }

            var existing = 0;
            var all = 0;

            foreach ( var dllName in GameDllFileNames )
            {
                var dllFile = new FileInfo( Path.Combine( skylinesDllDir.FullName, dllName ) );
                try
                {
                    var targetDllFile = new FileInfo( Path.Combine( calimeroExe.Directory.FullName, dllFile.Name ) );

                    if ( targetDllFile.Exists )
                        existing++;
                    else
                        dllFile.CopyTo( targetDllFile.FullName );
                    all++;
                }
                catch ( Exception e )
                {
                    error = e.Message;
                    return true;
                }
            }

            if ( all != 6 )
            {
                error = "DLL-Files not found";
                return true;
            }

            if ( existing != all )
                return true;

            return false;
        }
    }
}
