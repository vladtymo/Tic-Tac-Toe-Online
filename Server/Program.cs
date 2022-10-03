using CommandClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        static Random rnd = new Random();
        static void Main(string[] args)
        {
            const int port = 8080;
            IPAddress address = IPAddress.Parse("127.0.0.1");

            TcpListener server = new TcpListener(address, port);

            // змінна, яка визначає чи є гравець, який очікує початок гри
            bool hasPlayer = false;
            // гравець, який очікує початок гри
            PlayerInfo player1 = null;

            // запускаємо сервер
            server.Start();

            while (true)
            {
                try
                {
                    TcpClient client = server.AcceptTcpClient();

                    // отримуємо об'єкт команди від клієнта
                    BinaryFormatter formatter = new BinaryFormatter();
                    var command = (ClientCommand)formatter.Deserialize(client.GetStream());

                    // обробляємо команду
                    switch (command.Type)
                    {
                        // запит на запуск гри
                        case CommandType.START:
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"Request to start a new game from {command.Nickname}");
                            // якщо немає гравця для гри
                            if (!hasPlayer)
                            {
                                // зберігаємо інформацію про поточного гравця
                                player1 = new PlayerInfo()
                                {
                                    Nickname = command.Nickname,
                                    TcpClient = client,
                                    IsX = Convert.ToBoolean(rnd.Next(2)) // випадковим чином визначаємо символ гравця
                                };
                                // відкравляємо команду на очікування опонента
                                player1.SendWaitCommand();
                                // встановлюємо наявність гравця, який очікує гру
                                hasPlayer = true;
                            }
                            // якщо гравець чекає
                            else
                            {
                                // зберігаємо інформацію про поточного гравця
                                PlayerInfo player2 = new PlayerInfo()
                                {
                                    Nickname = command.Nickname,
                                    TcpClient = client,
                                    IsX = !player1.IsX // символ гравця буде протилежний до іншого
                                };

                                // відправка команд на запуск гри
                                player1.SendStartCommand(player2.Nickname);
                                player2.SendStartCommand(player1.Nickname);

                                // асинхронний старт сесій гравцій
                                Task.Run(() => player1.StartSession(player2));
                                Task.Run(() => player2.StartSession(player1));

                                // встановлюємо відсутність гравця, який очікує гру
                                hasPlayer = false;
                            }
                            break;
                        default:
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine($"Unknown command from {command.Nickname}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            // зупиняємо сервер
            server.Stop();
        }
    }
}
