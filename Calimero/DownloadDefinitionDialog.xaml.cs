using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.Generic;

using PloppableRICO;

namespace Calimero
{
    /// <summary>
    /// Interaktionslogik für DownloadDefinitionDialog.xaml
    /// </summary>
    public partial class DownloadDefinitionDialog : Window
    {
        public DownloadDefinitionDialog()
        {
            InitializeComponent();
        }

        public List<RICOBuilding> buildings;
        public RICOBuilding selectedBuilding;

        public bool? ShowDialog( Window owner, List<RICOBuilding> buildings )
        {
            this.Owner = owner;
            this.buildings = buildings;
            listBoxPremades.Items.Clear();
            foreach ( var building in buildings )
                listBoxPremades.Items.Add( PremadeString( building ) );
            listBoxPremades.Focus();
            return this.ShowDialog();
        }


        private string PremadeString( RICOBuilding building )
        {
            if ( building.service == "residential" )
                return String.Format( "{0}-{1}, Level {2}, {3} homes, {4} cost, used {5} times{6}", 
                    building.service, 
                    building.subService, 
                    building.level, 
                    building.homeCount, 
                    building.constructionCost,
                    building.usages,
                    ( building.author != "" ? ", official" : "" )
                );
            else
                return String.Format( 
                    "{0}-{1}, Level {2}, {3} workplaces ( {4} deviation ), {5} cost, used {6} times{7}", 
                    building.service, 
                    building.subService, 
                    building.level, 
                    building.workplacesString, 
                    building.workplaceDeviationString, 
                    building.constructionCost,
                    building.usages,
                    ( building.author != "" ? ", official" : "" )
                );
        }

        private void listBoxPremades_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            selectedBuilding = buildings[ listBoxPremades.SelectedIndex ];
        }

        private void Cancel( object sender, RoutedEventArgs e )
        {
            this.DialogResult = false;
            this.Close();
        }

        private void OK( object sender, RoutedEventArgs e )
        {
            this.DialogResult = true;
            this.Close();
        }

        private void listBoxPremades_MouseDoubleClick( object sender, MouseButtonEventArgs e )
        {
            selectedBuilding = buildings[listBoxPremades.SelectedIndex];
            this.DialogResult = true;
            this.Close();
        }
    }
}
