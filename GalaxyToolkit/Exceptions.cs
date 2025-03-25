using System;

namespace GalaxyToolkit {
    [Serializable]
    public class ToolkitException : Exception {
        public ToolkitException() { }
        public ToolkitException(string message) : base(message) { }
        public ToolkitException(string message, Exception inner) : base(message, inner) { }
    }

    [Serializable]
    public class MemoryStructureException : Exception {
        public MemoryStructureException() { }
        public MemoryStructureException(string message) : base(message) { }
        public MemoryStructureException(string message, Exception inner) : base(message, inner) { }
    }
}
