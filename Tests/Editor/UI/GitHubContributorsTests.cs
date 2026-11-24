using System.Linq;
using dev.limitex.avatar.compressor.editor.ui;
using NUnit.Framework;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    internal class GitHubContributorsTests
    {
        // Trimmed from a real /contributors response; extra fields must be ignored.
        private const string Payload =
            @"[
  {""login"":""Limitex"",""id"":76650151,""avatar_url"":""https://avatars.githubusercontent.com/u/76650151?v=4"",""html_url"":""https://github.com/Limitex"",""type"":""User"",""contributions"":95},
  {""login"":""dependabot[bot]"",""id"":49699333,""type"":""Bot"",""contributions"":12},
  {""login"":""KOZ39"",""id"":38822500,""type"":""User"",""contributions"":1}
]";

        [Test]
        public void Parse_Payload_KeepsUsersInApiOrder()
        {
            var contributors = GitHubContributors.Parse(Payload);

            CollectionAssert.AreEqual(
                new[] { "Limitex", "KOZ39" },
                contributors.Select(c => c.login)
            );
            CollectionAssert.AreEqual(
                new long[] { 76650151, 38822500 },
                contributors.Select(c => c.id)
            );
        }

        // The login becomes the profile URL path, so only GitHub's login grammar survives.
        [TestCase(@"[{""login"":""../evil"",""id"":1,""type"":""User""}]")]
        [TestCase(@"[{""login"":""valid"",""id"":0,""type"":""User""}]")]
        public void Parse_UnusableEntry_IsDropped(string json)
        {
            Assert.IsEmpty(GitHubContributors.Parse(json));
        }

        [TestCase(null)]
        [TestCase("not json")]
        public void Parse_NoContributors_ReturnsEmpty(string json)
        {
            Assert.IsEmpty(GitHubContributors.Parse(json));
        }
    }
}
