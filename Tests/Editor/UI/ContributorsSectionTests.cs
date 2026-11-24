using dev.limitex.avatar.compressor.editor.ui;
using NUnit.Framework;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    internal class ContributorsSectionTests
    {
        private const float Cell = ContributorsSection.AvatarSize + ContributorsSection.Spacing;

        [Test]
        public void ColumnCount_WidthNotMeasuredYet_UsesSingleRow()
        {
            Assert.AreEqual(9, ContributorsSection.ColumnCount(0f, 9));
        }

        [Test]
        public void ColumnCount_ExactFit_FillsRowWithoutTrailingSpacing()
        {
            float width = 4 * Cell - ContributorsSection.Spacing;

            Assert.AreEqual(4, ContributorsSection.ColumnCount(width, 9));
        }

        [Test]
        public void ColumnCount_PartialCell_RoundsDown()
        {
            float width = 4 * Cell - ContributorsSection.Spacing - 1f;

            Assert.AreEqual(3, ContributorsSection.ColumnCount(width, 9));
        }

        [Test]
        public void ColumnCount_NarrowerThanOneAvatar_ReturnsOne()
        {
            Assert.AreEqual(1, ContributorsSection.ColumnCount(10f, 9));
        }
    }
}
