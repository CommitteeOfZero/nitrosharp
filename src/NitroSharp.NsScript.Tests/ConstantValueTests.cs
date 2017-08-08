using Xunit;

namespace NitroSharp.NsScript.Tests
{
    public class ConstantValueTests
    {
        [Fact]
        public void CreateValue_Zero_ReturnsSameInstance()
        {
            Assert.True(ReferenceEquals(ConstantValue.Create(0), ConstantValue.Zero));
        }

        [Fact]
        public void CreateValue_One_ReturnsSameInstance()
        {
            Assert.True(ReferenceEquals(ConstantValue.Create(1), ConstantValue.One));
        }

        [Fact]
        public void CreateValue_DeltaZero_ReturnsSameInstance()
        {
            Assert.True(ReferenceEquals(ConstantValue.Create(0, isDeltaIntegerValue: true), ConstantValue.DeltaZero));
        }

        [Fact]
        public void CreateValue_DeltaOne_ReturnsSameInstance()
        {
            Assert.True(ReferenceEquals(ConstantValue.Create(1, isDeltaIntegerValue: true), ConstantValue.DeltaOne));
        }

        [Fact]
        public void CreateValue_Null_ReturnsSameInstance()
        {
            Assert.True(ReferenceEquals(ConstantValue.Create(null), ConstantValue.Null));
        }

        [Fact]
        public void CreateValue_EmptyString_ReturnsSameInstance()
        {
            Assert.True(ReferenceEquals(ConstantValue.Create(string.Empty), ConstantValue.EmptyString));
        }

        [Fact]
        public void CreateValue_AtSymbol_ReturnsSameInstance()
        {
            Assert.True(ReferenceEquals(ConstantValue.Create("@"), ConstantValue.AtSymbol));
        }

        [Fact]
        public void CreateValue_True_ReturnsSameInstance()
        {
            Assert.True(ReferenceEquals(ConstantValue.Create(true), ConstantValue.True));
        }

        [Fact]
        public void CreateValue_False_ReturnsSameInstance()
        {
            Assert.True(ReferenceEquals(ConstantValue.Create(false), ConstantValue.False));
        }

        [Fact]
        public void TestDefaultValues()
        {
            Assert.Equal(ConstantValue.Null, ConstantValue.Default(NsBuiltInType.Null));
            Assert.Equal(ConstantValue.Zero, ConstantValue.Default(NsBuiltInType.Integer));
            Assert.Equal(ConstantValue.False, ConstantValue.Default(NsBuiltInType.Boolean));
            Assert.Equal(ConstantValue.EmptyString, ConstantValue.Default(NsBuiltInType.String));
        }

        [Fact]
        public void TestConversionsToSameType()
        {
            Assert.Equal(ConstantValue.Null, ConstantValue.Null.ConvertTo(NsBuiltInType.Null));
            Assert.Equal(ConstantValue.Create(42), ConstantValue.Create(42).ConvertTo(NsBuiltInType.Integer));
            Assert.Equal(ConstantValue.Create("foo"), ConstantValue.Create("foo").ConvertTo(NsBuiltInType.String));
            Assert.Equal(ConstantValue.True, ConstantValue.True.ConvertTo(NsBuiltInType.Boolean));
            Assert.Equal(ConstantValue.False, ConstantValue.False.ConvertTo(NsBuiltInType.Boolean));
        }

        [Fact]
        public void TestIntToStringConversion()
        {
            var integer = ConstantValue.Create(42);
            Assert.Equal(ConstantValue.Create("42"), integer.ConvertTo(NsBuiltInType.String));
        }

        [Fact]
        public void TestIntToBoolConversion()
        {
            Assert.Equal(ConstantValue.False, ConstantValue.Zero.ConvertTo(NsBuiltInType.Boolean));
            Assert.Equal(ConstantValue.True, ConstantValue.One.ConvertTo(NsBuiltInType.Boolean));
            Assert.Equal(ConstantValue.True, ConstantValue.Create(42).ConvertTo(NsBuiltInType.Boolean));
        }

        [Fact]
        public void TestStringToIntConversion()
        {
            Assert.Equal(ConstantValue.Zero, ConstantValue.Create("foo").ConvertTo(NsBuiltInType.Integer));
            Assert.Equal(ConstantValue.DeltaZero, ConstantValue.AtSymbol.ConvertTo(NsBuiltInType.Integer));
        }

        [Fact]
        public void TestStringToBoolConversion()
        {
            Assert.Equal(ConstantValue.False, ConstantValue.Create("foo").ConvertTo(NsBuiltInType.Boolean));
        }

        [Fact]
        public void TestBoolToIntConversion()
        {
            Assert.Equal(ConstantValue.Zero, ConstantValue.False.ConvertTo(NsBuiltInType.Integer));
            Assert.Equal(ConstantValue.One, ConstantValue.True.ConvertTo(NsBuiltInType.Integer));
        }

        [Fact]
        public void TestBoolToStringConversion()
        {
            Assert.Equal(ConstantValue.Create("0"), ConstantValue.False.ConvertTo(NsBuiltInType.String));
            Assert.Equal(ConstantValue.Create("1"), ConstantValue.True.ConvertTo(NsBuiltInType.String));
        }

        [Fact]
        public void TestNullConversions()
        {
            Assert.Equal(ConstantValue.Default(NsBuiltInType.Null), ConstantValue.Null.ConvertTo(NsBuiltInType.Null));
            Assert.Equal(ConstantValue.Default(NsBuiltInType.Integer), ConstantValue.Null.ConvertTo(NsBuiltInType.Integer));
            Assert.Equal(ConstantValue.Default(NsBuiltInType.Boolean), ConstantValue.Null.ConvertTo(NsBuiltInType.Boolean));
            Assert.Equal(ConstantValue.Default(NsBuiltInType.String), ConstantValue.Null.ConvertTo(NsBuiltInType.String));
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

            Assert.NotEqual(str2, str1);
        }

        //[Fact]
        //public void NullEqualsNullLiteral()
        //{
        //    Assert.Equal(ConstantValue.Null, ConstantValue.Create("null"));
        //}

        //[Fact]
        //public void EmptyStringEqualsNullLiteral()
        //{
        //    Assert.Equal(ConstantValue.Create("null"), ConstantValue.EmptyString);
        //}

        [Fact]
        public void CompareIntegerToItsStringRepresentation()
        {
            var integer = ConstantValue.Create(42);
            var stringRepresentation = ConstantValue.Create("42");

            Assert.NotEqual(stringRepresentation, integer);
        }

        [Fact]
        public void TestAdditionOnIntegers()
        {
            var result = ConstantValue.One + ConstantValue.One;
            Assert.Equal(ConstantValue.Create(2), result);
        }

        [Fact]
        public void TestStringConcatenation()
        {
            var result = ConstantValue.Create("foo") + ConstantValue.Create("bar");
            Assert.Equal(ConstantValue.Create("foobar"), result);
        }

        [Fact]
        public void TestAdditionOnBool()
        {
            var result = ConstantValue.True + ConstantValue.True;
            Assert.Equal(ConstantValue.Create(2), result);
        }

        [Fact]
        public void TestAdditionOnIntegerAndEmptyString()
        {
            var integer = ConstantValue.Create(42);
            var result = integer + ConstantValue.EmptyString;
            Assert.Equal(integer, result);
        }

        [Fact]
        public void TestAdditionOnIntegerAndStringRepresentationOfNumber()
        {
            var integer = ConstantValue.Create(42);
            var result = integer + ConstantValue.Create("5");
            Assert.Equal(integer, result);
        }

        [Fact]
        public void TestAdditionOnStringRepresentationOfNumberAndInteger()
        {
            var integer = ConstantValue.Create(42);
            var result = ConstantValue.Create("5") + integer;
            Assert.Equal(integer, result);
        }

        [Fact]
        public void TestAdditionOnIntegerAndArbitraryString()
        {
            var result = ConstantValue.Create(42) + ConstantValue.Create("foo");
            Assert.Equal(ConstantValue.Create("42foo"), result);
        }

        [Fact]
        public void TestAdditionOnAtSymbolAndString()
        {
            var result = ConstantValue.Create("@") + ConstantValue.Create("foo");
            Assert.Equal(ConstantValue.Create("@foo"), result);
        }

        [Fact]
        public void TestAdditionOnAtSymbolAndInteger()
        {
            var result = ConstantValue.Create("@") + ConstantValue.Create(42);
            Assert.Equal(ConstantValue.Create(42, isDeltaIntegerValue: true), result);
        }

        [Fact]
        public void TestAdditionOnBoolAndString()
        {
            var result = ConstantValue.True + ConstantValue.Create("foo");
            Assert.Equal(ConstantValue.Create("1foo"), result);
        }
    }
}
