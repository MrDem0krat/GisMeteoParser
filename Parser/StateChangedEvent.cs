using System;

namespace ParserLib
{
    public class StateChangedEventArgs : EventArgs
    {
        public Parser.ParserState State { get; private set; }
        public bool HasError { get; private set; }
        public Exception Exception { get; private set; }

        public StateChangedEventArgs() { }
        public StateChangedEventArgs(Parser.ParserState state, Exception ex = null)
        {
            State = state;
            HasError = ex != null;
            Exception = ex;
        }
    }
}
