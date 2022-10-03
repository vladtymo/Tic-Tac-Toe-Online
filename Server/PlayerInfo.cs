using CommandClasses;
using System;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace Server
{
    // клас, який містить інформацію та методи для взаємодії з гравцем
    public class PlayerInfo
    {
        public TcpClient TcpClient { get; set; }
        public string Nickname { get; set; }
        public bool IsX { get; set; }

        // метод для відправки об'єкту команди
        private void SendCommand(ServerCommand command)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(TcpClient.GetStream(), command);
        }
        // методи відправки конкретних команд
        public void SendCloseCommand()
        {
            SendCommand(new ServerCommand(CommandType.CLOSE));
        }
        public void SendWaitCommand()
        {
            SendCommand(new ServerCommand(CommandType.WAIT));
        }
        public void SendStartCommand(string opponentName)
        {
            ServerCommand command = new ServerCommand(CommandType.START)
            {
                IsX = this.IsX,
                OpponentName = opponentName
            };

            SendCommand(command);
        }
        public void SendMoveCommand(CellCoord moveCoord)
        {
            ServerCommand command = new ServerCommand(CommandType.MOVE)
            {
                MoveCoord = moveCoord
            };

            SendCommand(command);
        }

        // метод отримання команди від сервера
        public ClientCommand ReceiveCommand()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            return (ClientCommand)formatter.Deserialize(TcpClient.GetStream());
        }

        // метод обробки команд від клієнта та взаємодії з опонентом
        public void StartSession(PlayerInfo opponent)
        {
            bool isExit = false;
            while (!isExit)
            {
                // отримання команди від клієнта
                ClientCommand command = ReceiveCommand();

                // обробка команди
                switch (command.Type)
                {
                    // команда ходу
                    case CommandType.MOVE:
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"Move on {command.MoveCoord} from {command.Nickname}");
                        // повідомлення опонента про виконаний хід
                        opponent.SendMoveCommand(command.MoveCoord);
                        break;
                    // команда закриття сесії на сервері
                    case CommandType.CLOSE:
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"Close command from {command.Nickname}");
                        // встановлення змінної для закриття цикла обробки подій поточного гравця
                        isExit = true;
                        break;
                    // команда завершення гри
                    case CommandType.EXIT:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Exit command from {command.Nickname}");
                        // повідомлення опонента про закриття сесії
                        opponent.SendCloseCommand();
                        // встановлення змінної для закриття цикла обробки подій поточного гравця
                        isExit = true;
                        break;
                }
            }
        }
    }
}
