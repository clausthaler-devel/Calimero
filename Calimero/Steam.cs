using System;
using System.IO;
using IWshRuntimeLibrary;

namespace Calimero
{
    public static class SteamUtil
    {
        static DirectoryInfo steamDir = findSteamDir();

        public static DirectoryInfo findWorkshopDir()
        {
            var sd = findSteamDir();
            return 
                sd != null ? 
                new DirectoryInfo( Path.Combine( sd.FullName, "SteamApps", "workshop", "content", "255710" ) ) : 
                null;
        }

        public static DirectoryInfo findSteamDir()
        {
            if ( findSteamByShortcut() || findSteamDirNaive() )
                return steamDir;
            else
                return null;
        }

        static bool findSteamDirNaive()
        {
            return steamExists( Environment.GetFolderPath( Environment.SpecialFolder.ProgramFilesX86 ) + "\\Steam" );
        }

        static bool findSteamByShortcut()
        {
            return
                findSteamByShortcut( @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs" ) ||
                findSteamByShortcut( Environment.GetFolderPath( Environment.SpecialFolder.StartMenu ) ) ||
                findSteamByShortcut( Environment.GetFolderPath( Environment.SpecialFolder.Desktop ) );
        }

        static bool findSteamByShortcut( string startMenuDirPath )
        {
            return findSteamByShortcut( new DirectoryInfo( startMenuDirPath ) );
        }

        static bool findSteamByShortcut( DirectoryInfo startMenuDir )
        {
            try
            {
                foreach ( var file in startMenuDir.GetFiles( "*steam*.lnk" ) )
                    if ( steamExists( LinkTargetPath( file ) ) )
                        return true;

                foreach ( var dir in startMenuDir.GetDirectories() )
                    if ( findSteamByShortcut( dir ) )
                        return true;
            }
            catch
            {

            }
            return false;
        }

        static DirectoryInfo LinkTargetPath( FileInfo file )
        {
            var shell = new  WshShell(); //Create a new WshShell Interface
            var link = shell.CreateShortcut(file.FullName); //Link the interface
            var path = link.TargetPath;
            return new DirectoryInfo( path ).Parent;
        }

        static bool steamExists( string dir )
        {
            return steamExists( new DirectoryInfo( dir ) );
        }

        static bool steamExists( DirectoryInfo dir )
        {
            steamDir = dir;
            return steamDir.Exists && System.IO.File.Exists( Path.Combine( steamDir.FullName, "Steam.exe" ) );
        }
    }
}
