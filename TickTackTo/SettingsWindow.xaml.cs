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
    /// Interaktionslogik für SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public bool IsMultiplayer { get; set; }
        public bool IsOnlineMultiplayer { get; set; }
        public DifficultyLevel Difficulty { get; set; }
        public string RoomId { get; set; }
        public WebSocket WebSocket { get; private set; }
        public string PlayerSymbol { get; private set; }

        public SettingsWindow(bool isMultiplayer, bool isOnlineMultiplayer, DifficultyLevel difficulty)
        {
            InitializeComponent();
            IsMultiplayer = isMultiplayer;
            IsOnlineMultiplayer = isOnlineMultiplayer;
            Difficulty = difficulty;

            MultiplayerOption.IsChecked = isMultiplayer && !isOnlineMultiplayer;
            OnlineMultiplayerOption.IsChecked = isOnlineMultiplayer;

            foreach (ComboBoxItem item in DifficultyLevelComboBox.Items)
            {
                if ((DifficultyLevel)item.Tag == difficulty)
                {
                    item.IsSelected = true;
                    break;
                }
            }
        }

        private void Mode_Checked(object sender, RoutedEventArgs e)
        {
            IsMultiplayer = MultiplayerOption.IsChecked == true || OnlineMultiplayerOption.IsChecked == true;
            IsOnlineMultiplayer = OnlineMultiplayerOption.IsChecked == true;
        }

        private void DifficultyLevel_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var comboBox = sender as System.Windows.Controls.ComboBox;
            var selectedItem = comboBox.SelectedItem as System.Windows.Controls.ComboBoxItem;
            Difficulty = (DifficultyLevel)selectedItem.Tag;
        }

        private void CreateOnlineGame_Click(object sender, RoutedEventArgs e)
        {
            WebSocket = new WebSocket("ws://localhost:8080");
            WebSocket.Opened += (s, ev) => {
                WebSocket.Send("{\"type\":\"create\"}");
            };
            WebSocket.MessageReceived += WebSocket_MessageReceived;
            WebSocket.Open();
        }

        private void JoinOnlineGame_Click(object sender, RoutedEventArgs e)
        {
            RoomId = RoomIdTextBox.Text;
            WebSocket = new WebSocket("ws://localhost:8080");
            WebSocket.Opened += (s, ev) => {
                WebSocket.Send($"{{\"type\":\"join\", \"roomId\":\"{RoomId}\"}}");
            };
            WebSocket.MessageReceived += WebSocket_MessageReceived;
            WebSocket.Open();
        }

        private void WebSocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(e.Message);

            switch ((string)data.type)
            {
                case "created":
                    RoomId = (string)data.roomId;
                    Dispatcher.Invoke(() => {
                        RoomIdTextBox.Text = RoomId;
                        MessageBox.Show($"Spiel erstellt. Raum-ID: {RoomId}");
                    });
                    break;
                case "joined":
                    Dispatcher.Invoke(() => MessageBox.Show("Spiel beigetreten."));
                    break;
                case "start":
                    PlayerSymbol = (string)data.symbol;
                    break;
                case "error":
                    Dispatcher.Invoke(() => MessageBox.Show((string)data.message));
                    break;
            }
        }

        private void ApplySettings_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
