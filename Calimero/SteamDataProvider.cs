using System;
using CRPTools;
using System.IO;
using PloppableRICO;

namespace Calimero
{
    class SteamDataProvider : ISteamDataProvider
    {
        public SteamData getSteamData( string packageID )
        {
            Steam.Workshop.FileInfo i;
            SteamData d = new SteamData();
            try
            {
                i = new Steam.Workshop.ItemParser().ItemOf( packageID );
                d.AuthorName = i.Author;
                d.Description = i.Description;
                d.Rating = i.Rating;
                return d;
            }
            catch
            {
                return null;
            }
        }
    }
}
