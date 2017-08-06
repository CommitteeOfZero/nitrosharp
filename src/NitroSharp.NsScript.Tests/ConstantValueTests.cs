using Xunit;

namespace NitroSharp.NsScript.Tests
{
    public class ConstantValueTests
    {
        [Fact]
        public void CreateZeroReturnsSameInstance()
        {
            Assert.True(ReferenceEquals(ConstantValue.Create(0), ConstantValue.Zero));
        }

        [Fact]
        public void CreateOneReturnsSameInstance()
        {
            Assert.True(ReferenceEquals(ConstantValue.Create(1), ConstantValue.One));
        }

        [Fact]
        public void CreateDeltaZeroReturnsSameInstance()
        {
            Assert.True(ReferenceEquals(ConstantValue.Create(0, isDeltaIntegerValue: true), ConstantValue.DeltaZero));
        }

        [Fact]
        public void CreateDeltaOneReturnsSameInstance()
        {
            Assert.True(ReferenceEquals(ConstantValue.Create(1, isDeltaIntegerValue: true), ConstantValue.DeltaOne));
        }

        [Fact]
        public void CreateNullReturnsSameInstance()
        {
            Assert.True(ReferenceEquals(ConstantValue.Create(null), ConstantValue.Null));
        }

        [Fact]
        public void CreateEmptyStringReturnsSameInstance()
        {
            Assert.True(ReferenceEquals(ConstantValue.Create(string.Empty), ConstantValue.EmptyString));
        }

        [Fact]
        public void CreateTrueReturnsSameInstance()
        {
            Assert.True(ReferenceEquals(ConstantValue.Create(true), ConstantValue.True));
        }

        [Fact]
        public void CreateFalseReturnsSameInstance()
        {
            Assert.True(ReferenceEquals(ConstantValue.Create(false), ConstantValue.False));
        }

        [Fact]
        public void CompareEqualIntegers()
        {
            var int1 = ConstantValue.Create(42);
            var int2 = ConstantValue.Create(42);

            Assert.Equal(int1, int2);
        }

        [Fact]
        public void CompareEqualStrings()
        {
            var str1 = ConstantValue.Create("foo");
            var str2 = ConstantValue.Create("foo");

            Assert.Equal(str1, str2);
        }

        [Fact]
        public void TestCaseSensivity()
        {
            var str1 = ConstantValue.Create("test");
            var str2 = ConstantValue.Create("TEST");

            Assert.NotEqual(str1, str2);
        }

        [Fact]
        public void CompareIntegerToItsStringRepresentation()
        {
            var integer = ConstantValue.Create(42);
            var stringRepresentation = ConstantValue.Create("42");

            Assert.NotEqual(integer, stringRepresentation);
        }

        [Fact]
        public void ZeroEqualsFalse()
        {
            Assert.Equal(ConstantValue.Zero, ConstantValue.False);
        }

        [Fact]
        public void ZeroNotEqualsTrue()
        {
            Assert.NotEqual(ConstantValue.Zero, ConstantValue.True);
        }

        [Fact]
        public void NonZeroNotEqualsFalse()
        {
            Assert.NotEqual(ConstantValue.Create(42), ConstantValue.False);
        }

        [Fact]
        public void NonZeroNotEqualsTrue()
        {
            Assert.NotEqual(ConstantValue.Create(42), ConstantValue.True);
        }

        [Fact]
        public void TrueNotEqualsTrueLiteral()
        {
            Assert.NotEqual(ConstantValue.True, ConstantValue.Create("true"));
        }

        [Fact]
        public void FalseNotEqualsFalseLiteral()
        {
            Assert.NotEqual(ConstantValue.False, ConstantValue.Create("false"));
        }

        [Fact]
        public void NullEqualsNullLiteral()
        {
            Assert.Equal(ConstantValue.Null, ConstantValue.Create("null"));
        }

        [Fact]
        public void NullEqualsZero()
        {
            Assert.Equal(ConstantValue.Null, ConstantValue.Zero);
        }

        [Fact]
        public void TestAdditionOnIntegers()
        {
            var result = ConstantValue.One + ConstantValue.One;
            Assert.Equal(result, ConstantValue.Create(2));
        }

        [Fact]
        public void TestAdditionOnStrings()
        {
            var result = ConstantValue.Create("foo") + ConstantValue.Create("bar");
            Assert.Equal(result, ConstantValue.Create("foobar"));
        }

        [Fact]
        public void TestAdditionOnBool()
        {
            var result = ConstantValue.True + ConstantValue.True;
            Assert.Equal(result, ConstantValue.Create(2));
        }

        [Fact]
        public void TestAdditionOnIntegerAndEmptyString()
        {
            var integer = ConstantValue.Create(42);
            Assert.Equal(integer + ConstantValue.EmptyString, integer);
        }

        [Fact]
        public void TestAdditionOnIntegerAndStringRepresentation()
        {
            var integer = ConstantValue.Create(42);
            var str = ConstantValue.Create("5");

            Assert.Equal(integer + str, integer);
        }

        [Fact]
        public void TestAdditionOnIntegerAndString()
        {
            var integer = ConstantValue.Create(42);
            var str = ConstantValue.Create("foo");

            Assert.Equal(integer + str, ConstantValue.Create("42foo"));
        }
    }
}
