using Xunit;

namespace NitroSharp.NsScript.Tests
{
    public class ConstantValueTests
    {
        [Fact]
        public void CreateValue_Zero_ReturnsSameInstance()
        {
            Assert.True(ReferenceEquals(ConstantValue.Create(0.0d), ConstantValue.Zero));
        }

        [Fact]
        public void CreateValue_One_ReturnsSameInstance()
        {
            Assert.True(ReferenceEquals(ConstantValue.Create(1), ConstantValue.One));
        }

        [Fact]
        public void CreateValue_DeltaZero_ReturnsSameInstance()
        {
            Assert.True(ReferenceEquals(ConstantValue.Create(0, isDeltaValue: true), ConstantValue.DeltaZero));
        }

        [Fact]
        public void CreateValue_DeltaOne_ReturnsSameInstance()
        {
            Assert.True(ReferenceEquals(ConstantValue.Create(1, isDeltaValue: true), ConstantValue.DeltaOne));
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
            Assert.Equal(ConstantValue.Null, ConstantValue.Default(BuiltInType.Null));
            Assert.Equal(ConstantValue.Zero, ConstantValue.Default(BuiltInType.Double));
            Assert.Equal(ConstantValue.False, ConstantValue.Default(BuiltInType.Boolean));
            Assert.Equal(ConstantValue.EmptyString, ConstantValue.Default(BuiltInType.String));
        }

        [Fact]
        public void TestConversionsToSameType()
        {
            Assert.Equal(ConstantValue.Null, ConstantValue.Null.ConvertTo(BuiltInType.Null));
            Assert.Equal(ConstantValue.Create(42), ConstantValue.Create(42).ConvertTo(BuiltInType.Double));
            Assert.Equal(ConstantValue.Create("foo"), ConstantValue.Create("foo").ConvertTo(BuiltInType.String));
            Assert.Equal(ConstantValue.True, ConstantValue.True.ConvertTo(BuiltInType.Boolean));
            Assert.Equal(ConstantValue.False, ConstantValue.False.ConvertTo(BuiltInType.Boolean));
        }

        [Fact]
        public void TestIntToStringConversion()
        {
            var integer = ConstantValue.Create(42);
            Assert.Equal(ConstantValue.Create("42"), integer.ConvertTo(BuiltInType.String));
        }

        [Fact]
        public void TestIntToBoolConversion()
        {
            Assert.Equal(ConstantValue.False, ConstantValue.Zero.ConvertTo(BuiltInType.Boolean));
            Assert.Equal(ConstantValue.True, ConstantValue.One.ConvertTo(BuiltInType.Boolean));
            Assert.Equal(ConstantValue.True, ConstantValue.Create(42).ConvertTo(BuiltInType.Boolean));
        }

        [Fact]
        public void TestStringToIntConversion()
        {
            Assert.Equal(ConstantValue.Zero, ConstantValue.Create("foo").ConvertTo(BuiltInType.Double));
            Assert.Equal(ConstantValue.DeltaZero, ConstantValue.AtSymbol.ConvertTo(BuiltInType.Double));
        }

        [Fact]
        public void TestStringToBoolConversion()
        {
            Assert.Equal(ConstantValue.False, ConstantValue.Create("foo").ConvertTo(BuiltInType.Boolean));
        }

        [Fact]
        public void TestBoolToIntConversion()
        {
            Assert.Equal(ConstantValue.Zero, ConstantValue.False.ConvertTo(BuiltInType.Double));
            Assert.Equal(ConstantValue.One, ConstantValue.True.ConvertTo(BuiltInType.Double));
        }

        [Fact]
        public void TestBoolToStringConversion()
        {
            Assert.Equal(ConstantValue.Create("0"), ConstantValue.False.ConvertTo(BuiltInType.String));
            Assert.Equal(ConstantValue.Create("1"), ConstantValue.True.ConvertTo(BuiltInType.String));
        }

        [Fact]
        public void TestNullConversions()
        {
            Assert.Equal(ConstantValue.Default(BuiltInType.Null), ConstantValue.Null.ConvertTo(BuiltInType.Null));
            Assert.Equal(ConstantValue.Default(BuiltInType.Double), ConstantValue.Null.ConvertTo(BuiltInType.Double));
            Assert.Equal(ConstantValue.Default(BuiltInType.Boolean), ConstantValue.Null.ConvertTo(BuiltInType.Boolean));
            Assert.Equal(ConstantValue.Default(BuiltInType.String), ConstantValue.Null.ConvertTo(BuiltInType.String));
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
            Assert.Equal(ConstantValue.Create((object)42, isDeltaValue: true), result);
        }

        [Fact]
        public void TestAdditionOnBoolAndString()
        {
            var result = ConstantValue.True + ConstantValue.Create("foo");
            Assert.Equal(ConstantValue.Create("1foo"), result);
        }

        [Fact]
        public void CreateEnumValueConstant()
        {
            var constant = ConstantValue.Create(BuiltInEnumValue.Axl1);
            Assert.Equal(BuiltInType.EnumValue, constant.Type);
            Assert.Equal(BuiltInEnumValue.Axl1, constant.EnumValue);
        }

        [Fact]
        public void TestEnumValueToStringConversion()
        {
            var enumValue = ConstantValue.Create(BuiltInEnumValue.Axl1);
            var converted = enumValue.ConvertTo(BuiltInType.String);
            Assert.Equal("Axl1", converted.StringValue);
        }
    }
}
