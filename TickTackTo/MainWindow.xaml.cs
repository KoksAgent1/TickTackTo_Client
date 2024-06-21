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
using System.Windows.Navigation;
using System.Windows.Shapes;
using WebSocket4Net;

namespace TickTackTo
{
    public enum DifficultyLevel
    {
        Einfach,
        Anspruchsvoll,
        Schwer
    }

    public partial class MainWindow : Window
    {
        private bool isXNext = true;
        private string[] board = new string[9];
        private bool isSingleplayer = false;
        private bool isOnlineMultiplayer = false;
        private DifficultyLevel difficultyLevel = DifficultyLevel.Einfach;
        private string playerSymbol;
        private string roomId;
        private WebSocket webSocket;

        public MainWindow()
        {
            InitializeComponent();
            ResetBoard();
        }

        private void ResetBoard()
        {
            for (int i = 0; i < board.Length; i++)
            {
                board[i] = string.Empty;
                var button = FindName("Button" + i) as Button;
                button.Content = string.Empty;
                button.IsEnabled = true;
            }
            isXNext = true;
            //UpdateCurrentPlayerText();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            int index = int.Parse(button.Name.Replace("Button", string.Empty));

            if (board[index] == string.Empty)
            {
                if (isOnlineMultiplayer)
                {
                    if (playerSymbol == (isXNext ? "X" : "O"))
                    {
                        webSocket.Send($"{{\"type\":\"move\", \"roomId\":\"{roomId}\", \"symbol\":\"{playerSymbol}\", \"index\":{index}}}");
                    }
                }
                else
                {
                    MakeMove(index);
                }
            }
        }

        private void MakeMove(int index)
        {
            board[index] = isXNext ? "X" : "O";
            var button = FindName("Button" + index) as Button;
            button.Content = board[index];
            button.IsEnabled = false;

            if (CheckForWinner())
            {
                MessageBox.Show($"{board[index]} hat gewonnen!");
                ResetBoard();
            }
            else if (CheckForDraw())
            {
                MessageBox.Show("Unentschieden");
                ResetBoard();
            }
            else
            {
                isXNext = !isXNext;
                UpdateCurrentPlayerText();

                if (isSingleplayer && !isXNext)
                {
                    ComputerMove();
                }
            }
        }

        private void ComputerMove()
        {
            int bestMove;
            switch (difficultyLevel)
            {
                case DifficultyLevel.Einfach:
                    bestMove = GetEasyMove();
                    break;
                case DifficultyLevel.Anspruchsvoll:
                    bestMove = GetChallengingMove();
                    break;
                case DifficultyLevel.Schwer:
                    bestMove = GetHardMove();
                    break;
                default:
                    bestMove = GetEasyMove();
                    break;
            }

            MakeMove(bestMove);
        }

        private int GetEasyMove()
        {
            var emptyIndices = board.Select((value, index) => new { value, index })
                                    .Where(x => x.value == string.Empty)
                                    .Select(x => x.index)
                                    .ToList();
            if (emptyIndices.Count > 0)
            {
                Random random = new Random();
                return emptyIndices[random.Next(emptyIndices.Count)];
            }
            return -1;
        }

        private int GetChallengingMove()
        {
            // Mix of random and strategic moves
            Random random = new Random();
            if (random.Next(2) == 0)
            {
                return GetEasyMove();
            }
            return GetHardMove();
        }

        private int GetHardMove()
        {
            // Check for winning move
            for (int i = 0; i < board.Length; i++)
            {
                if (board[i] == string.Empty)
                {
                    board[i] = "O";
                    if (CheckForWinner())
                    {
                        board[i] = string.Empty;
                        return i;
                    }
                    board[i] = string.Empty;
                }
            }

            // Block opponent's winning move
            for (int i = 0; i < board.Length; i++)
            {
                if (board[i] == string.Empty)
                {
                    board[i] = "X";
                    if (CheckForWinner())
                    {
                        board[i] = string.Empty;
                        return i;
                    }
                    board[i] = string.Empty;
                }
            }

            // Choose center if available
            if (board[4] == string.Empty)
            {
                return 4;
            }

            // Choose a corner if available
            int[] corners = { 0, 2, 6, 8 };
            foreach (int corner in corners)
            {
                if (board[corner] == string.Empty)
                {
                    return corner;
                }
            }

            // Choose a random empty space
            return GetEasyMove();
        }

        private bool CheckForWinner()
        {
            string[,] winPatterns = new string[,]
            {
                { board[0], board[1], board[2] },
                { board[3], board[4], board[5] },
                { board[6], board[7], board[8] },
                { board[0], board[3], board[6] },
                { board[1], board[4], board[7] },
                { board[2], board[5], board[8] },
                { board[0], board[4], board[8] },
                { board[2], board[4], board[6] }
            };

            for (int i = 0; i < 8; i++)
            {
                if (winPatterns[i, 0] != string.Empty &&
                    winPatterns[i, 0] == winPatterns[i, 1] &&
                    winPatterns[i, 1] == winPatterns[i, 2])
                {
                    return true;
                }
            }

            return false;
        }

        private bool CheckForDraw()
        {
            foreach (var cell in board)
            {
                if (cell == string.Empty)
                {
                    return false;
                }
            }
            return true;
        }

        private void UpdateCurrentPlayerText()
        {
            if (isOnlineMultiplayer && !string.IsNullOrEmpty(playerSymbol))
            {
                CurrentPlayerText.Text = $"Aktuell am Zug: {(isXNext ? "X" : "O")}";
            }
            else
            {
                CurrentPlayerText.Text = $"Aktuell am Zug: {(isXNext ? "X": "O")}";
            }
        }

        private void Mode_Checked(object sender, RoutedEventArgs e)
        {
            isSingleplayer = SingleplayerOption.IsChecked == true;
            isOnlineMultiplayer = OnlineMultiplayerOption.IsChecked == true;
            ResetBoard();
            UpdateSettingsVisibility();
        }

        private void DifficultyLevel_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var comboBox = sender as System.Windows.Controls.ComboBox;
            var selectedItem = comboBox.SelectedItem as System.Windows.Controls.ComboBoxItem;
            difficultyLevel = (DifficultyLevel)selectedItem.Tag;
            ResetBoard();
        }

        private void UpdateSettingsVisibility()
        {
            DifficultyLevelComboBox.Visibility = isSingleplayer ? Visibility.Visible : Visibility.Collapsed;
            DificultyText.Visibility = isSingleplayer ? Visibility.Visible : Visibility.Collapsed;
            RoomIdTextBox.Visibility = isOnlineMultiplayer ? Visibility.Visible : Visibility.Collapsed;
            PlayerSymbolText.Visibility = isOnlineMultiplayer ? Visibility.Visible : Visibility.Collapsed;
            OnlineText.Visibility = isOnlineMultiplayer ? Visibility.Visible : Visibility.Collapsed;
            CreateOnlineGameButton.Visibility = isOnlineMultiplayer ? Visibility.Visible : Visibility.Collapsed;
            JoinOnlineGameButton.Visibility = isOnlineMultiplayer ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CreateOnlineGame_Click(object sender, RoutedEventArgs e)
        {
            webSocket = new WebSocket("ws://localhost:8080");
            webSocket.Opened += (s, ev) => {
                webSocket.Send("{\"type\":\"create\"}");
            };
            webSocket.MessageReceived += WebSocket_MessageReceived;
            webSocket.Open();
        }

        private void JoinOnlineGame_Click(object sender, RoutedEventArgs e)
        {
            roomId = RoomIdTextBox.Text;
            webSocket = new WebSocket("ws://localhost:8080");
            webSocket.Opened += (s, ev) => {
                webSocket.Send($"{{\"type\":\"join\", \"roomId\":\"{roomId}\"}}");
            };
            webSocket.MessageReceived += WebSocket_MessageReceived;
            webSocket.Open();
        }

        private void WebSocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(e.Message);

            switch ((string)data.type)
            {
                case "created":
                    roomId = (string)data.roomId;
                    Dispatcher.Invoke(() => {
                        RoomIdTextBox.Text = roomId;
                        MessageBox.Show($"Spiel erstellt. Raum-ID: {roomId}");
                    });
                    break;
                case "joined":
                    Dispatcher.Invoke(() => MessageBox.Show("Spiel beigetreten."));
                    break;
                case "start":
                    Console.WriteLine("Game started");
                    playerSymbol = (string)data.symbol;
                    Dispatcher.Invoke(() => {
                        PlayerSymbolText.Text = $"Dein Symbole: {playerSymbol}";
                        PlayerSymbolText.Visibility = Visibility.Visible;
                        UpdateCurrentPlayerText();
                    });
                    break;
                case "move":
                    int index = (int)data.index;
                    string symbol = (string)data.symbol;
                    Console.WriteLine("Move erhalten von "+symbol+" eigenes Symbol "+playerSymbol);
                    Dispatcher.Invoke(() => {
                        board[index] = symbol;
                        var button = FindName("Button" + index) as Button;
                        button.Content = symbol;
                        button.IsEnabled = false;

                        if (CheckForWinner())
                        {
                            MessageBox.Show($"{symbol} hat gewonnen!");
                            ResetBoard();
                        }
                        else if (CheckForDraw())
                        {
                            MessageBox.Show("Unentschieden");
                            ResetBoard();
                        }
                        else
                        {
                            isXNext = !isXNext;
                            UpdateCurrentPlayerText();
                        }
                    });
                    break;
                case "player_left":
                    Console.WriteLine("Der andere Spieler hat das Spiel verlassen.");
                    Dispatcher.Invoke(() => {
                        MessageBox.Show("Der andere Spieler hat das Spiel verlassen.");
                        ResetBoard();
                    });
                    break;
                case "error":
                    Console.WriteLine("Fehler: "+(string)data.message);
                    Dispatcher.Invoke(() => MessageBox.Show((string)data.message));
                    break;
            }
        }
    }
}
