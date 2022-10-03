using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandClasses
{
    // перелік типів команд
    public enum CommandType
    {
        WAIT,    // очікування (опонента)
        START,   // початок гри
        RESTART, // перезапуск гри
        MOVE,    // хід
        CLOSE,   // закриття сесії на сервері
        EXIT,    // закриття гри
    }

    // координати клітинки
    [Serializable]
    public struct CellCoord
    {
        public int RowIndex { get; set; }
        public int ColumnIndex { get; set; }

        public override string ToString()
        {
            return $"{RowIndex}:{ColumnIndex}";
        }
    }

    // клас, який містить інформацію, яка передається від клієнта на сервер
    [Serializable]
    public class ClientCommand
    {
        public CommandType Type { get; set; }
        public string Nickname { get; set; }
        public CellCoord MoveCoord { get; set; }

        public ClientCommand(CommandType type, string nick)
        {
            this.Type = type;
            this.Nickname = nick;
        }
    }

    // клас, який містить інформацію, яка передається від сервера на клієнт
    [Serializable]
    public class ServerCommand
    {
        public CommandType Type { get; set; }
        public bool IsX { get; set; }
        public string OpponentName { get; set; }
        public CellCoord MoveCoord { get; set; }

        public ServerCommand(CommandType type)
        {
            this.Type = type;
        }
    }
}
