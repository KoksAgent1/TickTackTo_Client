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

namespace TickTackTo
{
    /// <summary>
    /// Interaktionslogik für StatsAndHistoryPage.xaml
    /// </summary>
    public partial class StatsAndHistoryPage : Window
    {
        public StatsAndHistoryPage(int wins, int losses, int draws, dynamic history)
        {
            InitializeComponent();
            WinsText.Text = $"Gewonnen: {wins}";
            LossesText.Text = $"Verloren: {losses}";
            DrawsText.Text = $"Unentschieden: {draws}";
            HistoryDataGrid.ItemsSource = history;
        }
    }
}
