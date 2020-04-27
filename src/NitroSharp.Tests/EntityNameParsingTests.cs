//using NitroSharp.Interactivity;
//using Xunit;

//namespace NitroSharp.Tests
//{
//    public class EntityNameTests
//    {
//        [Theory]
//        [InlineData("name", null, null, "name")]
//        [InlineData("parent/name", "parent", null, "name")]
//        [InlineData("parent/MouseUsual/name", "parent", MouseState.Normal, "name")]
//        [InlineData("parent/MouseOver/name", "parent", MouseState.Over, "name")]
//        [InlineData("parent/MouseClick/name", "parent", MouseState.Pressed, "name")]
//        [InlineData("parent/MouseLeave/name", "parent", MouseState.Leave, "name")]
//        [InlineData("root/parent/name", "root/parent", null, "name")]
//        [InlineData("root/parent/MouseLeave/name", "root/parent", MouseState.Leave, "name")]
//        [InlineData("root/grandparent/parent/name", "root/grandparent/parent", null, "name")]
//        [InlineData("root/grandparent/parent/MouseLeave/name", "root/grandparent/parent", MouseState.Leave, "name")]
//        internal void ParseEntityName(string entityName, string? parent, MouseState? mouseState, string ownName)
//        {
//            var en = new EntityName(entityName);
//            Assert.Equal(parent, en.Parent);
//            Assert.Equal(mouseState, en.MouseState);
//            Assert.Equal(ownName, en.OwnName.ToString());
//        }
//    }
//}
