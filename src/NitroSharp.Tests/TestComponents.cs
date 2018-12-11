using System;
using NitroSharp.EntitySystem;

namespace NitroSharp.Tests
{
    public struct TestComponent : Component<TestComponent>, IEquatable<TestComponent>
    {
        public int Value;

        public void DeepCopy(ref TestComponent destination)
            => destination = this;

        public bool Equals(TestComponent other) => Value == other.Value;
    }

    public struct TestComponent2 : Component<TestComponent2>, IEquatable<TestComponent2>
    {
        public string Value;

        public void DeepCopy(ref TestComponent2 destination)
            => destination = this;

        public bool Equals(TestComponent2 other) => Value == other.Value;
    }

    public struct TestComponent3 : Component<TestComponent3>
    {
        public void DeepCopy(ref TestComponent3 destination) => destination = this;
    }

    public struct TestComponent4 : Component<TestComponent4>
    {
        public void DeepCopy(ref TestComponent4 destination) => destination = this;
    }

    public struct TestComponent5 : Component<TestComponent5>
    {
        public void DeepCopy(ref TestComponent5 destination) => destination = this;
    }

    public struct TestComponent6 : Component<TestComponent6>
    {
        public void DeepCopy(ref TestComponent6 destination) => destination = this;
    }

    public struct TestComponent7 : Component<TestComponent7>
    {
        public void DeepCopy(ref TestComponent7 destination) => destination = this;
    }

    public struct TestComponent8 : Component<TestComponent8>
    {
        public void DeepCopy(ref TestComponent8 destination) => destination = this;
    }

    public struct TestComponent9 : Component<TestComponent9>
    {
        public void DeepCopy(ref TestComponent9 destination) => destination = this;
    }

    public struct TestComponent10 : Component<TestComponent10>
    {
        public void DeepCopy(ref TestComponent10 destination) => destination = this;
    }

    public struct TestComponent11 : Component<TestComponent11>
    {
        public void DeepCopy(ref TestComponent11 destination) => destination = this;
    }

    public struct TestComponent12 : Component<TestComponent12>
    {
        public void DeepCopy(ref TestComponent12 destination) => destination = this;
    }

    public struct TestComponent13 : Component<TestComponent13>
    {
        public void DeepCopy(ref TestComponent13 destination) => destination = this;
    }

    public struct TestComponent14 : Component<TestComponent14>
    {
        public void DeepCopy(ref TestComponent14 destination) => destination = this;
    }

    public struct TestComponent15 : Component<TestComponent15>
    {
        public void DeepCopy(ref TestComponent15 destination) => destination = this;
    }

    public struct TestComponent16 : Component<TestComponent16>
    {
        public void DeepCopy(ref TestComponent16 destination) => destination = this;
    }

    public struct TestComponent17 : Component<TestComponent17>
    {
        public void DeepCopy(ref TestComponent17 destination) => destination = this;
    }

    public struct TestComponent18 : Component<TestComponent18>
    {
        public void DeepCopy(ref TestComponent18 destination) => destination = this;
    }

    public struct TestComponent19 : Component<TestComponent19>
    {
        public void DeepCopy(ref TestComponent19 destination) => destination = this;
    }

    public struct TestComponent20 : Component<TestComponent20>
    {
        public void DeepCopy(ref TestComponent20 destination) => destination = this;
    }

    public struct TestComponent21 : Component<TestComponent21>
    {
        public void DeepCopy(ref TestComponent21 destination) => destination = this;
    }

    public struct TestComponent22 : Component<TestComponent22>
    {
        public void DeepCopy(ref TestComponent22 destination) => destination = this;
    }

    public struct TestComponent23 : Component<TestComponent23>
    {
        public void DeepCopy(ref TestComponent23 destination) => destination = this;
    }

    public struct TestComponent24 : Component<TestComponent24>
    {
        public void DeepCopy(ref TestComponent24 destination) => destination = this;
    }

    public struct TestComponent25 : Component<TestComponent25>
    {
        public void DeepCopy(ref TestComponent25 destination) => destination = this;
    }

    public struct TestComponent26 : Component<TestComponent26>
    {
        public void DeepCopy(ref TestComponent26 destination) => destination = this;
    }

    public struct TestComponent27 : Component<TestComponent27>
    {
        public void DeepCopy(ref TestComponent27 destination) => destination = this;
    }

    public struct TestComponent28 : Component<TestComponent28>
    {
        public void DeepCopy(ref TestComponent28 destination) => destination = this;
    }

    public struct TestComponent29 : Component<TestComponent29>
    {
        public void DeepCopy(ref TestComponent29 destination) => destination = this;
    }

    public struct TestComponent30 : Component<TestComponent30>
    {
        public void DeepCopy(ref TestComponent30 destination) => destination = this;
    }

    public struct TestComponent31 : Component<TestComponent31>
    {
        public void DeepCopy(ref TestComponent31 destination) => destination = this;
    }

    public struct TestComponent32 : Component<TestComponent32>
    {
        public void DeepCopy(ref TestComponent32 destination) => destination = this;
    }

    public struct TestComponent33 : Component<TestComponent33>
    {
        public void DeepCopy(ref TestComponent33 destination) => destination = this;
    }
}
