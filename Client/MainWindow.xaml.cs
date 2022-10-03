using CommandClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
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

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TcpClient client;

        // параметри поточної гри
        public bool IsX { get; set; }
        public char Symbol => IsX ? 'X' : 'O';
        public char OpponentSymbol => IsX ? 'O' : 'X';
        public string Nickname { get; set; }
        public string OpponentNickname { get; set; }
        public bool IsPlaying { get; set; }
        private bool isMoving = false;
        // властивість, яка визначає можливість робити хід
        public bool IsMoving
        {
            get { return isMoving; }
            set
            {
                isMoving = value;

                // при зміні властивості змінюємо колір клітинок
                var brush = isMoving ? Brushes.White : Brushes.Gray;
                foreach (Border item in fieldGrid.Children.OfType<Border>())
                {
                    item.Background = brush;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            IsPlaying = false;
            IsMoving = false;
        }

        // метод відправки команди на сервер
        private Task SendCommand(ClientCommand command)
        {
            // викликаємо Task для можливості запускати метод асинхронно (за доп. await)
            return Task.Run(() =>
            {
                // серіалізуємо об'єкт команди в мережевий потік,
                // після чого відразу відбувається відправка
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(client.GetStream(), command);
            });
        }
        // метод отримання команди від сервер
        private Task<ServerCommand> ReceiveCommand()
        {
            return Task.Run(() =>
            {
                // десеріалізуємо об'єкт команди з мережевого потоку
                // та повертаємо його
                BinaryFormatter formatter = new BinaryFormatter();
                return (ServerCommand)formatter.Deserialize(client.GetStream());
            });
        }

        // метд обровки команд сервера
        private async void Listen()
        {
            try
            {
                bool isExit = false;
                while (!isExit)
                {
                    // очікуємо та приймаємо команду від сервера
                    ServerCommand command = await ReceiveCommand();

                    // обробляємо команду
                    switch (command.Type)
                    {
                        // команда очікування опонента
                        case CommandType.WAIT:
                            // візуально повідомляємо про очікування
                            opponentNameTxtBox.Content = "Waiting...";
                            break;
                        // команда початку гри
                        case CommandType.START:
                            // встановлюємо параметри гри
                            IsX = command.IsX;
                            IsMoving = IsX;
                            symbolLabel.Content = Symbol;
                            OpponentNickname = command.OpponentName;
                            opponentNameTxtBox.Content = OpponentNickname;
                            break;
                        // команда ходу опонента
                        case CommandType.MOVE:
                            // знаходимо клітинку, де був виконаний хід
                            foreach (Border item in fieldGrid.Children.OfType<Border>())
                            {
                                // звіряємо координати
                                if (item.Tag.Equals(command.MoveCoord))
                                {
                                    // встановлюємо символ опонента
                                    ((TextBlock)item.Child).Text = OpponentSymbol.ToString();
                                }
                                // дозволяємо хід
                                IsMoving = true;
                            }
                            break;
                        // команда завершення гри 
                        case CommandType.CLOSE:
                            // завершуємо гру
                            CloseSession();
                            // встановлюємл змінну для закриття цикла обробки подій від сервера
                            isExit = true;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // відправка запиту на початок гри
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // перевіряємо чи гра не була запущена
                if (!IsPlaying)
                {
                    IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(ipTxtBox.Text), int.Parse(portTxtBox.Text));
                    client = new TcpClient();
                    // підключаємося до сервера
                    client.Connect(serverEndPoint);
                    // зберігаємо введене ім'я гравця
                    Nickname = nameTxtBox.Text;
                    // відправляємо запит на початок гри
                    await SendCommand(new ClientCommand(CommandType.START, Nickname));
                    // запускамо метод прослуховування команд він сервера
                    Listen();

                    IsPlaying = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // метод завершення гри
        private async void CloseSession()
        {
            // відправка команди для закриття сесії на сервері
            await SendCommand(new ClientCommand(CommandType.CLOSE, Nickname));

            // закриття поточного підключення
            client.Close();
            client = null;

            // встановлення початкових параметрів на клієнті
            IsPlaying = false;
            IsMoving = false;
            symbolLabel.Content = "-";
            opponentNameTxtBox.Content = "-";

            foreach (Border item in fieldGrid.Children.OfType<Border>())
            {
                ((TextBlock)item.Child).Text = string.Empty;
            }
        }

        // метод нажаття на клітинку ігрового поля
        async private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // якщо хід дозволено
            if (IsMoving)
            {
                // встановлюємо символ в клітинку на полі
                ((TextBlock)((Border)sender).Child).Text = Symbol.ToString();

                // відправляємо команду про хід на сервер
                ClientCommand command = new ClientCommand(CommandType.MOVE, Nickname)
                {
                    MoveCoord = (CellCoord)((Border)sender).Tag
                };
                await SendCommand(command);

                // забороняємо хід
                IsMoving = false;
            }
        }

        // обробка події закриття вікна
        private void Window_Closed(object sender, EventArgs e)
        {
            // якщо гра триває
            if (IsPlaying)
                // відправляємо команду на закритті гри
                SendCommand(new ClientCommand(CommandType.EXIT, Nickname));
        }
    }
}