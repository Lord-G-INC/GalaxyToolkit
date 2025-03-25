using GalaxyToolkit.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace GalaxyToolkit.Structures {
    using JsonData = Dictionary<string, JsonElement>;

    public class GalaxyStructure {
        private readonly string _name;
        private readonly string? _baseString;
        private readonly GalaxyStructure? _base;
        private readonly List<StructureMember> _members;

        public string Name {
            get => _name;
        }

        public GalaxyStructure? Inherits {
            get => _base;
        }

        public List<StructureMember> Members {
            get => _members;
        }

        public GalaxyStructure(string path) {
            var json = JsonSerializer.Deserialize<JsonData>(File.ReadAllText(path)) ?? throw new MemoryStructureException($"Memory structure \"{path}\" could not be deserialized.");
            var name = json["Name"].GetString();

            if (string.IsNullOrEmpty(name))
                throw new MemoryStructureException($"Memory structure \"{path}\" has an invalid name.");

            _name = name;
            _members = [];

            if (json.TryGetValue("Base", out var baseElement))
                _baseString = baseElement.GetString();
            
            foreach (var memberJson in json["Members"].EnumerateArray()) {
                var memberInfo = memberJson.Deserialize<JsonData>();
                if (memberInfo is null)
                    continue;

                var memberType = memberInfo["Type"].GetString();
                if (memberType is null)
                    continue;

                var memberName = memberInfo["Name"].GetString();
                if (memberName is null)
                    continue;

                var memberOffset = memberInfo["Offset"].GetUInt32();
                var member = new StructureMember(this, memberType, memberName, memberOffset);

                _members.Add(member);
            }
        }

        public void Initialize() {

        }

        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append("class ");
            sb.Append(_name);

            if (_baseString is not null) {
                sb.Append(" : public ");
                sb.Append(_baseString);
            }

            sb.AppendLine(" {");

            foreach (var member in _members) {
                sb.Append("    ");
                sb.AppendLine(member.ToString());
            }

            sb.AppendLine("};");
            return sb.ToString();
        }
    }

    public class StructureMember {
        private readonly GalaxyStructure _parent;

        private readonly StructureType _type;
        private readonly string _name;
        private readonly uint _offset;

        private BufferAddress _address;
        private bool _isAddressInitialized;

        public StructureMember(GalaxyStructure parent, string type, string name, uint offset) {
            _parent = parent;
            _name = name;
            _type = StructureType.Parse(type);
            _offset = offset;
        }

        public override string ToString() {
            return $"{_type} {_name} // _{_offset:X}";
        }
    }

    public readonly struct StructureType {
        private readonly string _type;
        private readonly int _pointerCount;
        private readonly uint _arraySize;

        public string Type {
            get => _type;
            init => _type = value;
        }

        public int PointerCount {
            get => _pointerCount;
            init => _pointerCount = value;
        }

        public uint ArraySize {
            get => _arraySize;
            init => _arraySize = value;
        }

        public override string ToString() {
            var sb = new StringBuilder(_type.Length + _pointerCount);
            sb.Append(_type);

            for (int i = 0; i < _pointerCount; i++)
                sb.Append('*');

            if (_arraySize > 1) {
                sb.Append('[');
                sb.Append(_arraySize);
                sb.Append(']');
            }

            return sb.ToString();
        }

        public static StructureType Parse(string s) {
            const byte READ_STAGE_NAME = 0;
            const byte READ_POINTER = 1;
            const byte READ_ARRAY = 2;

            var readStage = READ_STAGE_NAME;

            var sb = new StringBuilder();
            var pointerCount = 0;
            var arraySize = 1u;

            int i = 0;
            while (i < s.Length) {
                var c = s[i];

                if (c == '*') {
                    readStage = READ_POINTER;
                }
                else if (c == '[') {
                    readStage = READ_ARRAY;
                }

                switch (readStage) {
                    case READ_STAGE_NAME:
                        if (c == ' ') {
                            readStage = READ_POINTER;
                            break;
                        }

                        if (!char.IsAsciiLetter(c) && !char.IsAsciiDigit(c))
                            return ThrowInvalidCharException(c, i);

                        sb.Append(c);
                        break;
                    case READ_POINTER:
                        if (c == '*')
                            pointerCount++;
                        else if (c != ' ')
                            return ThrowInvalidCharException(c, i);

                        break;
                    case READ_ARRAY:
                        if (i + 2 >= s.Length) {
                            throw new FormatException($"Array in \"{s}\" at index {i} doesn't have a size.");
                        }

                        var lastChar = s[^1];

                        if (lastChar != ']') {
                            return ThrowInvalidCharException(lastChar, s.Length - 1);
                        }

                        arraySize = uint.Parse(s[(i + 1)..^1]);
                        goto End;
                    default:
                        throw new Exception("uhhhh idk");
                }

                i++;
            }

            End:
            return new StructureType() {
                Type = sb.ToString(),
                PointerCount = pointerCount,
                ArraySize = arraySize,
            };

            StructureType ThrowInvalidCharException(char c, int i) {
                throw new FormatException($"Invalid character '{c}' in \"{s}\" at index {i}.");
            }
        }
    }
}
