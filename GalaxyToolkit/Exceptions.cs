using System;

namespace GalaxyToolkit {
    [Serializable]
    public class ToolkitException : Exception {
        public ToolkitException() { }
        public ToolkitException(string message) : base(message) { }
        public ToolkitException(string message, Exception inner) : base(message, inner) { }
    }
}
