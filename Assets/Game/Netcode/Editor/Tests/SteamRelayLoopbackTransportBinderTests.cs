using InterStella.Game.Netcode.Runtime;
using NUnit.Framework;

namespace InterStella.Game.Netcode.Editor.Tests
{
    public sealed class SteamRelayLoopbackTransportBinderTests
    {
        [Test]
        public void TryParseEndpoint_ParsesAddressAndPort()
        {
            bool parsed = SteamRelayLoopbackTransportBinder.TryParseEndpoint("127.0.0.1:7770", out string address, out ushort port);

            Assert.That(parsed, Is.True);
            Assert.That(address, Is.EqualTo("127.0.0.1"));
            Assert.That(port, Is.EqualTo((ushort)7770));
        }

        [Test]
        public void TryParseEndpoint_RejectsInvalidInput()
        {
            bool parsed = SteamRelayLoopbackTransportBinder.TryParseEndpoint("invalid-endpoint", out string address, out ushort port);

            Assert.That(parsed, Is.False);
            Assert.That(address, Is.EqualTo(string.Empty));
            Assert.That(port, Is.EqualTo((ushort)0));
        }
    }
}
