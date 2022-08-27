using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MessagePack;
using NitroSharp.NsScript;

namespace NitroSharp;

[StructLayout(LayoutKind.Auto)]
internal readonly struct EntityId : IEquatable<EntityId>
{
    private readonly int _hashCode;
    private readonly int _nameStart;

    public readonly string Path;
    public readonly uint Context;

    public EntityId(ref MessagePackReader reader)
    {
        reader.ReadArrayHeader();
        Context = reader.ReadUInt32();
        string? value = reader.ReadString();
        if (value is not null)
        {
            var path = new EntityPath(value);
            _nameStart = 0;
            Path = path.Value;
            _hashCode = HashCode.Combine(Path.GetHashCode(), Context);
        }
        else
        {
            Path = null!;
            _nameStart = 0;
            Context = 0;
            _hashCode = 0;
        }
    }

    public EntityId(
        uint context,
        string path,
        int nameStart)
    {
        Context = context;
        Path = path;
        _hashCode = HashCode.Combine(path.GetHashCode(), context);
        _nameStart = nameStart;
    }

    public static EntityId Invalid => default;

    public ReadOnlySpan<char> Name => Path.AsSpan(_nameStart);
    public bool IsValid => Path is not null;

    public EntityId Child(string name)
    {
        Debug.Assert(!name.Contains('/'));
        return new EntityId(Context, $"{Path}/{name}", Path.Length + 1);
    }

    public override int GetHashCode() => _hashCode;
    public bool Equals(EntityId other) => string.Equals(Path, other.Path);
    public override string ToString() => Path;

    public void Serialize(ref MessagePackWriter writer)
    {
        writer.WriteArrayHeader(2);
        writer.Write(Context);
        writer.Write(Path);
    }
}
