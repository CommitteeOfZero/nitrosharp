﻿using System;

namespace NitroSharp.NsScript
{
    /// <summary>
    /// A path to an entity. May not contain wildcard patterns, and, therefore,
    /// always resolves to either 1 or 0 entities.
    /// The root of a path can be an alias, which makes it possible to refer to the same
    /// entity using different paths. Thus, an <see cref="EntityPath"/> should not
    /// be used as a unique identifier.
    /// </summary>
    public readonly struct EntityPath : IEquatable<EntityPath>
    {
        private readonly string? _parent;
        public readonly string Value;
        public readonly int NameStartIndex;

        private enum ParseResult
        {
            Ok,
            ContainsWildcard,
            UnexpectedAtSymbol
        }

        public static bool IsValidPath(EntityQuery query, out EntityPath path)
        {
            path = new EntityPath(query, out ParseResult parseResult);
            return parseResult == ParseResult.Ok;
        }

        public EntityPath(string path)
            : this(new EntityQuery(path), out ParseResult result)
        {
            static void wildcard()
            {
                throw new ArgumentException($"An {nameof(EntityPath)}, unlike a regular"
                    + $"{nameof(EntityQuery)}, cannot contain wildcard characters."
                );
            }

            static void unexpectedAtSymbol()
            {
                throw new ArgumentException($"Only the root of an {nameof(EntityPath)}"
                    + "may refer to an alias."
                );
            }

            if (result == ParseResult.ContainsWildcard)
            {
                wildcard();
            }
            else if (result == ParseResult.UnexpectedAtSymbol)
            {
                unexpectedAtSymbol();
            }
        }

        private EntityPath(EntityQuery query, out ParseResult result)
        {
            string path = query.Value;
            int pos = 0;
            _parent = null;
            NameStartIndex = 0;
            result = ParseResult.Ok;
            Value = path;
            foreach (EntityQueryPart part in query.EnumerateParts())
            {
                if (part.IsLast && pos > 0)
                {
                    _parent ??= path[..(pos - 1)];
                }
                else
                {
                    if (part.SearchInAliases && pos > 0)
                    {
                        result = ParseResult.UnexpectedAtSymbol;
                        return;
                    }
                    if (part.IsWildcardPattern)
                    {
                        result = ParseResult.ContainsWildcard;
                        return;
                    }
                }
                if (part.IsLast)
                {
                    NameStartIndex = pos;
                }
                pos = part.CharsConsumed;
            }
        }

        public ReadOnlySpan<char> Name => Value.AsSpan(NameStartIndex);
        public bool IsEmpty => string.IsNullOrEmpty(Value);
        public bool HasParent => _parent is not null;

        private static EntityPath Empty => new(string.Empty);

        private ReadOnlySpan<char> CharsToHash
        {
            get
            {
                if (string.IsNullOrEmpty(Value)) { return default; }
                return Value[0] != '@' ? Value : Value.AsSpan(1);
            }
        }

        public bool GetParent(out EntityPath parent)
        {
            if (_parent is null)
            {
                parent = Empty;
                return false;
            }

            parent = new EntityPath(_parent);
            return true;
        }

        private EntityPath Join(in EntityPath other) => new($"{Value}/{other.Value}");
        public EntityPath Join(string other) => Join(new EntityPath(other));

        public override int GetHashCode() => string.GetHashCode(CharsToHash);
        public override string ToString() => Value;

        public bool Equals(EntityPath other)
            => CharsToHash.Equals(other.CharsToHash, StringComparison.Ordinal);
    }
}
