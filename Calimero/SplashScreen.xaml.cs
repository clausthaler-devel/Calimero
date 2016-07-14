using System;
using System.Windows;
using System.Threading;

namespace Calimero
{
    /// <summary>
    /// Interaktionslogik für SplashScreen.xaml
    /// </summary>
    public partial class MySplashScreen : Window
    {
        public MySplashScreen()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            var me = this;
            this.Activated += ( object s, EventArgs e ) => {
                new Thread( () => {
                    GameDlls.CopyGameDlls();
                    Thread.Sleep( 2000 );
                    me.Dispatcher.Invoke( () => me.Close() );
                } ).Start();
            };
        }
    }
}
