//using System;
//using System.Buffers;
//using System.Linq;
//using MessagePack;
//using NitroSharp.Saving;
//using Xunit;

//namespace NitroSharp.Tests
//{
//    [SavegameData]
//    public class BaseClass
//    {
//        public string BaseClassField;
//    }

//    [SavegameData]
//    public class DerivedClass : BaseClass, IEquatable<DerivedClass>
//    {
//        public int SomeNumber;
//        public string SomeText;
//        public int[] Data;

//        public bool Equals(DerivedClass? other) =>
//            other is not null && SomeText == other.SomeText
//                && SomeNumber == other.SomeNumber
//                && BaseClassField == other.BaseClassField
//                && Enumerable.SequenceEqual(Data, other.Data);
//    }

//    public class NewSerializationTests
//    {
//        [Fact]
//        public void SerializeTest()
//        {
//            var obj = new DerivedClass
//            {
//                BaseClassField = "LuLu",
//                SomeNumber = 69,
//                SomeText = "Meow",
//                Data = new int[] { 1, 2, 3, 4, 5 }
//            };

//            var buffer = new ArrayBufferWriter<byte>();
//            var writer = new MessagePackWriter(buffer);
//            obj.Serialize(ref writer);

//            ReadOnlyMemory<byte> bytes = buffer.GetMemory();
//            var reader = new MessagePackReader(bytes);
//            var obj2 = new DerivedClass();
//            obj2.Deserialize(ref reader);

//            Assert.Equal(obj, obj2);
//        }
//    }
//}
