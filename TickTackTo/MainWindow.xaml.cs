using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
    #region Enums
    public enum DifficultyLevel
    {
        Einfach,
        Anspruchsvoll,
        Schwer
    }
    #endregion

    public partial class MainWindow : Window
    {
        #region Variablen

        //Websocket
        private WebSocket webSocket;
        private string prodconnection = "ws://91.107.203.9:2000";
        private string localconnection = "ws://localhost:2000";
        private bool isdev = false;

        //Game States
        private bool isXNext = true;
        private string[] board = new string[9];
        private bool gamerunning = false;
        private bool isMyTurn;
        private string playerSymbol;
        private string roomId;
        private DifficultyLevel difficultyLevel = DifficultyLevel.Einfach;

        //Benutzer Daten
        private int userId;
        private string username;
        private int wins;
        private int losses;
        private int draws;

        //Game Modes

        private bool isSingleplayer = false;
        private bool isOnlineMultiplayer = false;



        #endregion

        #region Construktor | Init

        public MainWindow()
        {
            Init();
        }

        private void Init()
        {
            InitializeComponent();
            UpdateSettingsVisibility();
            InitializeWebSocket();
        }


        #endregion

        #region Websocket Methoden

        private void InitializeWebSocket()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            webSocket = new WebSocket(isdev ? localconnection : prodconnection);
            webSocket.Opened += (s, ev) => {
                Console.WriteLine("WebSocket opened");
            };
            webSocket.Error += (s, ev) => {
                Console.WriteLine($"WebSocket error: {ev.Exception.Message}");
            };
            webSocket.Closed += (s, ev) => {
                Console.WriteLine("WebSocket closed");
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
                    Dispatcher.Invoke(() =>
                    {
                        ResetBoard();
                        RoomIdTextBox.Text = roomId;
                        MessageBox.Show($"Spiel erstellt. Raum-ID: {roomId}");
                    });
                    break;
                case "joined":
                    Dispatcher.Invoke(() =>
                    {
                        ResetBoard();
                        MessageBox.Show("Spiel beigetreten.");
                    });
                    break;
                case "start":
                    playerSymbol = (string)data.symbol;
                    isMyTurn = (bool)data.starts;
                    Dispatcher.Invoke(() => {
                        gamerunning = true;
                        PlayerSymbolText.Text = $"Dein Symbol: {playerSymbol}";
                        PlayerSymbolText.Visibility = Visibility.Visible;
                        UpdateCurrentPlayerText();
                        EndGame(false);
                    });
                    break;
                case "move":
                    int index = (int)data.index;
                    string symbol = (string)data.symbol;
                    Dispatcher.Invoke(() => {
                        board[index] = symbol;
                        var button = FindName("Button" + index) as Button;
                        button.Content = symbol;
                        button.IsEnabled = false;

                        if (CheckForWinner())
                        {
                            gamerunning = false;
                            MessageBox.Show($"{symbol} hat gewonnen!");
                            ResetBoard();
                            EndGame(true);
                        }
                        else if (CheckForDraw())
                        {
                            gamerunning = false;
                            MessageBox.Show("Unentschieden");
                            ResetBoard();
                            EndGame(true);
                        }
                        else
                        {
                            isMyTurn = !isMyTurn;
                            UpdateCurrentPlayerText();
                        }
                    });
                    break;
                case "reset":
                    Dispatcher.Invoke(() => {
                        isMyTurn = (bool)data.starts;
                        gamerunning = true;
                        PlayerSymbolText.Text = $"Dein Symbol: {playerSymbol}";
                        PlayerSymbolText.Visibility = Visibility.Visible;
                        UpdateCurrentPlayerText();
                        ResetBoard();
                        EndGame(true);
                        Settings(false);
                    });
                    break;
                case "player_left":
                    Dispatcher.Invoke(() => {
                        gamerunning = false;
                        MessageBox.Show("Der andere Spieler hat das Spiel verlassen.");
                        ResetBoard();
                    });
                    break;
                case "error":
                    gamerunning = false;
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show((string)data.message);
                        ResetBoard();
                    });
                    break;
                case "register":
                    Dispatcher.Invoke(() => {
                        if ((bool)data.success)
                        {
                            MessageBox.Show("Registrierung erfolgreich!");
                        }
                        else
                        {
                            MessageBox.Show("Registrierung fehlgeschlagen.");
                        }
                    });
                    break;
                case "login":
                    Dispatcher.Invoke(() => {
                        UsernameTextBox.Text = string.Empty;
                        PasswordBox.Password = string.Empty;
                        if ((bool)data.success)
                        {
                            userId = (int)data.userId;
                            username = (string)data.username;
                            LoginState(true);
                            MessageBox.Show("Anmeldung erfolgreich!");
                        }
                        else
                        {
                            MessageBox.Show("Anmeldung fehlgeschlagen.");
                        }
                    });
                    break;
                case "stats":
                    Dispatcher.Invoke(() => {
                        var wins = (int)data.wins;
                        var losses = (int)data.losses;
                        var draws = (int)data.draws;
                        webSocket.Send($"{{\"type\":\"game_history\", \"userId\":{userId}}}");
                        this.wins = wins;
                        this.losses = losses;
                        this.draws = draws;
                    });
                    break;
                case "game_history":
                    Dispatcher.Invoke(() => {
                        var history = (dynamic)data.history;
                        ShowStatsAndHistoryPage(this.wins, this.losses, this.draws, history);
                    });
                    break;
                case "update_username":
                    Dispatcher.Invoke(() => {
                        if ((bool)data.success)
                        {
                            username = (string)data.newUsername;
                            MessageBox.Show("Benutzername erfolgreich aktualisiert!");
                        }
                        else
                        {
                            MessageBox.Show("Fehler beim Aktualisieren des Benutzernamens.");
                        }
                    });
                    break;
                case "update_password":
                    Dispatcher.Invoke(() => {
                        if ((bool)data.success)
                        {
                            MessageBox.Show("Passwort erfolgreich aktualisiert!");
                        }
                        else
                        {
                            MessageBox.Show("Fehler beim Aktualisieren des Passworts.");
                        }
                    });
                    break;
            }
        }

        #endregion Websocket Methoden

        #region Show Pages Methode

        private void ShowStatsAndHistoryPage(int wins, int losses, int draws, dynamic history)
        {
            var statsAndHistoryPage = new StatsAndHistoryPage(wins, losses, draws, history);
            statsAndHistoryPage.Show();
        }

        #endregion Show Pages Methode

        #region UI Event Methodes

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            int index = int.Parse(button.Name.Replace("Button", string.Empty));
            if (board[index] == string.Empty)
            {
                Settings(false);
                if (isOnlineMultiplayer)
                {
                    if (isMyTurn)
                    {
                        if (webSocket.State == WebSocketState.Closed)
                        {
                            webSocket.Open();
                        }
                        webSocket.Send($"{{\"type\":\"move\", \"roomId\":\"{roomId}\", \"symbol\":\"{playerSymbol}\", \"index\":{index}}}");
                    }
                }
                else
                {
                    MakeMove(index);
                }
            }
        }

        private void CreateOnlineGame_Click(object sender, RoutedEventArgs e)
        {
            if (webSocket.State == WebSocketState.Closed)
            {
                webSocket.Open();
            }
            webSocket.Send($"{{\"type\":\"create\", \"userId\":{userId}}}");
            Settings(false);
        }

        private void JoinOnlineGame_Click(object sender, RoutedEventArgs e)
        {
            if (webSocket.State == WebSocketState.Closed)
            {
                webSocket.Open();
            }
            roomId = RoomIdTextBox.Text;
            webSocket.Send($"{{\"type\":\"join\", \"roomId\":\"{roomId}\", \"userId\":{userId}}}");
            Settings(false);
        }

        private void ResetGame_Click(object sender, RoutedEventArgs e)
        {
            if (isOnlineMultiplayer)
            {
                if (webSocket.State == WebSocketState.Closed)
                {
                    webSocket.Open();
                }
                webSocket.Send($"{{\"type\":\"reset\", \"roomId\":\"{roomId}\"}}");
            }
            ResetBoard();
            EndGame(false);
            Settings(false);
        }

        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            if (isOnlineMultiplayer)
            {
                if (webSocket.State == WebSocketState.Closed)
                {
                    webSocket.Open();
                }
                webSocket.Send($"{{\"type\":\"new_room\", \"userId\":{userId}}}");
            }
            ResetBoard();
        }

        private void DifficultyLevel_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var comboBox = sender as System.Windows.Controls.ComboBox;
            var selectedItem = comboBox.SelectedItem as System.Windows.Controls.ComboBoxItem;
            difficultyLevel = (DifficultyLevel)selectedItem.Tag;
            if (isSingleplayer)
            {
                ResetBoard();
                gamerunning = true;
            }

        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameTextBox.Text;
            var password = PasswordBox.Password;
            webSocket.Send($"{{\"type\":\"register\", \"username\":\"{username}\", \"password\":\"{password}\"}}");
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameTextBox.Text;
            var password = PasswordBox.Password;
            webSocket.Send($"{{\"type\":\"login\", \"username\":\"{username}\", \"password\":\"{password}\"}}");
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            userId = 0;
            username = null;
           LoginState(false);
        }

        private void Stats_Click(object sender, RoutedEventArgs e)
        {
            webSocket.Send($"{{\"type\":\"stats\", \"userId\":{userId}}}");
        }

        private void AccountSettings_Click(object sender, RoutedEventArgs e)
        {
            var accountSettingsPage = new AccountSettingsPage(userId, username, webSocket);
            accountSettingsPage.Show();
        }

        private void Mode_Checked(object sender, RoutedEventArgs e)
        {
            isSingleplayer = SingleplayerOption.IsChecked == true;
            isOnlineMultiplayer = OnlineMultiplayerOption.IsChecked == true;

            if (isOnlineMultiplayer && userId == 0)
            {
                MessageBox.Show("Bitte melden Sie sich an, um den Online-Multiplayer-Modus zu nutzen.");
                LocalMultiplayerOption.IsChecked = true;
                isOnlineMultiplayer = false;
            }

            if (!isSingleplayer)
            {
                ResetBoard();
            }
            UpdateSettingsVisibility();
        }

        #endregion UI Event Methodes

        #region Singelplayer Methodes
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

        #endregion Singelplayer Methodes

        #region Game Methodes

        private void ResetBoard()
        {
            if (gamerunning)
            {
                return;
            }
            for (int i = 0; i < board.Length; i++)
            {
                board[i] = string.Empty;
                var button = FindName("Button" + i) as Button;
                button.Content = string.Empty;
                button.IsEnabled = true;
            }
            isXNext = true;
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
                gamerunning = false;
                ResetBoard();
                Settings(true);
            }
            else if (CheckForDraw())
            {
                MessageBox.Show("Unentschieden");
                gamerunning = false;
                ResetBoard();
                Settings(true);
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
                CurrentPlayerText.Text = isMyTurn ? "Dein Zug" : "Gegener Zug";
            }
            else
            {
                CurrentPlayerText.Text = $"Aktuell am Zug: {(isXNext ? "X" : "O")}";
            }
        }


        #endregion

        #region UI Manager
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

        private void EndGame(bool showendgame)
        {
            if (showendgame)
            {
                ResetGameButton.Visibility = Visibility.Visible;
                NewGameButton.Visibility = Visibility.Visible;
                CurrentPlayerText.Text = string.Empty;
                Settings(true);
            }
            else
            {
                ResetGameButton.Visibility = Visibility.Collapsed;
                NewGameButton.Visibility = Visibility.Collapsed;
            }
        }

        private void Settings(bool showsettings)
        {
            if (showsettings)
            {
                LocalMultiplayerOption.IsEnabled = true;
                OnlineMultiplayerOption.IsEnabled = true;
                SingleplayerOption.IsEnabled = true;
                DifficultyLevelComboBox.IsEnabled = true;
            }
            else
            {
                LocalMultiplayerOption.IsEnabled = false;
                OnlineMultiplayerOption.IsEnabled = false;
                SingleplayerOption.IsEnabled = false;
                DifficultyLevelComboBox.IsEnabled = false;
            }
        }

        private void LoginState(bool loggedin)
        {
            if (loggedin)
            {
                UsernameTextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Collapsed;
                LoggedInUserText.Visibility = Visibility.Visible;
                LoggedInUserText.Text = $"Angemeldet als: {username}";
                LogoutButton.Visibility = Visibility.Visible;
                StatsButton.Visibility = Visibility.Visible;
                UsernameText.Visibility = Visibility.Collapsed;
                PasswordText.Visibility = Visibility.Collapsed;
                RegisterButton.Visibility = Visibility.Collapsed;
                LoginButton.Visibility = Visibility.Collapsed;
                AccountSettingsButton.Visibility = Visibility.Visible;
            }
            else
            {
                UsernameTextBox.Visibility = Visibility.Visible;
                PasswordBox.Visibility = Visibility.Visible;
                LoggedInUserText.Visibility = Visibility.Collapsed;
                LogoutButton.Visibility = Visibility.Collapsed;
                StatsButton.Visibility = Visibility.Collapsed;
                UsernameText.Visibility = Visibility.Visible;
                PasswordText.Visibility = Visibility.Visible;
                RegisterButton.Visibility = Visibility.Visible;
                LoginButton.Visibility = Visibility.Visible;
                AccountSettingsButton.Visibility = Visibility.Collapsed;
            }
        }


        #endregion UI Manager

    }
}
