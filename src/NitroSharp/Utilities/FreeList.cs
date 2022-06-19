using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NitroSharp.Utilities
{
    internal readonly ref struct RefOption<T> where T : struct
    {
        private readonly Span<T> _span;
        public readonly bool HasValue;

        public RefOption(ref T reference)
        {
            _span = MemoryMarshal.CreateSpan(ref reference, 1);
            HasValue = true;
        }

        public ref T Unwrap()
        {
            static void panic()
            {
                throw new InvalidOperationException("Unwrap() has been called on a None value.");
            }

            if (!HasValue) { panic(); }
            return ref _span[0];
        }

        public static RefOption<T> None => default;
    }

    internal readonly record struct FreeListHandle(uint Index, uint Version)
    {
        public static FreeListHandle Invalid => new(0, 0);

        public WeakFreeListHandle GetWeakHandle() => new(Index, Version);
    }

    internal readonly record struct WeakFreeListHandle(uint Index, uint Version)
    {
        public static WeakFreeListHandle Invalid => new(0, 0);
    }

    public sealed class InvalidHandleException : Exception
    {
        public InvalidHandleException(string message) : base(message)
        {
        }
    }

    internal sealed class FreeList<T> where T : struct
    {
        private const uint None = uint.MaxValue;
        private const uint InitialVersion = 1;

        [StructLayout(LayoutKind.Auto)]
        private struct Slot
        {
            public T Value;
            public uint Next;
            public uint Version;
        }

        private ArrayBuilder<Slot> _slots;
        private uint _head;

        public FreeList()
        {
            _slots = new ArrayBuilder<Slot>(initialCapacity: 1);
            ref Slot slot = ref _slots.Add();
            slot.Next = None;
            slot.Version = InitialVersion;
            _head = 0;
        }

        public FreeListHandle Insert(in T value)
        {
            uint head = _head;
            FreeListHandle handle;
            if (head != None)
            {
                ref Slot slot = ref _slots[head];
                _head = slot.Next;
                slot.Next = None;
                slot.Value = value;
                handle = new FreeListHandle(head, slot.Version);
            }
            else
            {
                uint index = _slots.Count;
                ref Slot slot = ref _slots.Add();
                slot.Next = None;
                slot.Version = InitialVersion;
                slot.Value = value;
                handle = new FreeListHandle(index, InitialVersion);
            }

            return handle;
        }

        public (FreeListHandle? newHandle, T? oldValue) Upsert(WeakFreeListHandle handle, in T value)
        {
            ref Slot slot = ref _slots[handle.Index];
            if (slot.Version == handle.Version)
            {
                T oldValue = slot.Value;
                slot.Value = value;
                return (null, oldValue);
            }

            return (Insert(value), null);
        }

        public ref T Get(FreeListHandle handle)
        {
            Debug.Assert(handle.Version > 0);
            return ref _slots[handle.Index].Value;
        }

        public RefOption<T> GetOpt(WeakFreeListHandle handle)
        {
            if (handle.Index >= _slots.Count)
            {
                goto none;
            }
            ref Slot slot = ref _slots[handle.Index];
            if (handle.Version != slot.Version)
            {
                goto none;
            }

            return new RefOption<T>(ref slot.Value);

        none:
            return RefOption<T>.None;
        }

        public T? TryFree(WeakFreeListHandle handle)
        {
            return GetOpt(handle).HasValue
                ? Free(new FreeListHandle(handle.Index, handle.Version))
                : null;
        }

        public T Free(FreeListHandle handle)
        {
            Debug.Assert(handle.Version > 0);
            ref Slot slot = ref _slots[handle.Index];
            slot.Next = _head;
            slot.Version++;
            _head = handle.Index;
            T value = slot.Value;
            slot.Value = default;
            return value;
        }
    }

    internal sealed class FreeList
    {
        private const uint None = uint.MaxValue;
        private const uint InitialVersion = 1;

        [StructLayout(LayoutKind.Auto)]
        private struct Slot
        {
            public uint Next;
            public uint Version;
        }

        private ArrayBuilder<Slot> _slots;
        private uint _head;

        public FreeList()
        {
            _slots = new ArrayBuilder<Slot>(initialCapacity: 1);
            ref Slot slot = ref _slots.Add();
            slot.Next = None;
            slot.Version = InitialVersion;
            _head = 0;
        }

        public FreeListHandle Next()
        {
            uint head = _head;
            FreeListHandle handle;
            if (head != None)
            {
                ref Slot slot = ref _slots[head];
                _head = slot.Next;
                slot.Next = None;
                handle = new FreeListHandle(head, slot.Version);
            }
            else
            {
                uint index = _slots.Count;
                ref Slot slot = ref _slots.Add();
                slot.Next = None;
                slot.Version = InitialVersion;
                handle = new FreeListHandle(index, InitialVersion);
            }

            return handle;
        }

        public void ThrowIfInvalid(FreeListHandle handle)
        {
            static void invalid() => throw new InvalidHandleException(
                "Attempt to use an invalid free list handle.");

            if (handle.Index >= _slots.Count ||
                handle.Version != _slots[handle.Index].Version)
            {
                invalid();
            }
        }

        public bool ValidateHandle(FreeListHandle handle)
        {
            return handle.Index < _slots.Count
                && _slots[handle.Index].Version == handle.Version;
        }

        public void Free(ref FreeListHandle handle)
        {
            ThrowIfInvalid(handle);
            ref Slot slot = ref _slots[handle.Index];
            slot.Next = _head;
            slot.Version++;
            _head = handle.Index;
            handle = FreeListHandle.Invalid;
        }

        public void Clear()
        {
            _slots.Clear();
            _head = None;
        }
    }
}
