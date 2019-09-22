using Xunit;

namespace NitroSharp.NsScript.Tests
{
    public class EntityNameTests
    {
        [Theory]
        [InlineData("name", "", null, "name")]
        [InlineData("parent/name", "parent", null, "name")]
        [InlineData("parent/MouseUsual/name", "parent", NsMouseState.Normal, "name")]
        [InlineData("parent/MouseOver/name", "parent", NsMouseState.Over, "name")]
        [InlineData("parent/MouseClicked/name", "parent", NsMouseState.Pressed, "name")]
        [InlineData("parent/MouseLeave/name", "parent", NsMouseState.Leave, "name")]
        [InlineData("root/parent/name", "root/parent", null, "name")]
        [InlineData("root/parent/MouseLeave/name", "root/parent", NsMouseState.Leave, "name")]
        [InlineData("root/grandparent/parent/name", "root/grandparent/parent", null, "name")]
        [InlineData("root/grandparent/parent/MouseLeave/name", "root/grandparent/parent", NsMouseState.Leave, "name")]
        internal void ParseEntityName(string entityName, string parent, NsMouseState? mouseState, string ownName)
        {
            var en = new EntityName(entityName);
            Assert.Equal(parent, en.Parent.ToString());
            Assert.Equal(mouseState, en.MouseState);
            Assert.Equal(ownName, en.OwnName.ToString());
        }
    }
}
