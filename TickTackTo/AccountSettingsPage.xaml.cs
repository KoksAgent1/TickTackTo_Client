using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WebSocket4Net;

namespace TickTackTo
{
    /// <summary>
    /// Interaktionslogik für AccountSettingsPage.xaml
    /// </summary>
    public partial class AccountSettingsPage : Window
    {
        private int userId;
        private WebSocket webSocket;
        public AccountSettingsPage(int userId, string username, WebSocket webSocket)
        {
            InitializeComponent();
            this.userId = userId;
            this.webSocket = webSocket;
            UsernameTextBox.Text = username;
        }

        private void UpdateUsername_Click(object sender, RoutedEventArgs e)
        {
            var newUsername = UsernameTextBox.Text;
            webSocket.Send($"{{\"type\":\"update_username\", \"userId\":{userId}, \"newUsername\":\"{newUsername}\"}}");
        }

        private void UpdatePassword_Click(object sender, RoutedEventArgs e)
        {
            var newPassword = NewPasswordBox.Password;
            webSocket.Send($"{{\"type\":\"update_password\", \"userId\":{userId}, \"newPassword\":\"{newPassword}\"}}");
        }
    }
}
