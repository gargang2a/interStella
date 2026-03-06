using InterStella.Game.Netcode.Runtime;
using NUnit.Framework;

namespace InterStella.Game.Netcode.Editor.Tests
{
    public sealed class NetworkSequenceComparerTests
    {
        [Test]
        public void UShortSequence_RecognizesSimpleIncrement()
        {
            Assert.That(NetworkSequenceComparer.IsNewer(11, 10), Is.True);
        }

        [Test]
        public void UShortSequence_RejectsSameOrOlderValue()
        {
            Assert.That(NetworkSequenceComparer.IsNewer(10, 10), Is.False);
            Assert.That(NetworkSequenceComparer.IsNewer(9, 10), Is.False);
        }

        [Test]
        public void UShortSequence_HandlesWrapAround()
        {
            Assert.That(NetworkSequenceComparer.IsNewer(0, 65535), Is.True);
            Assert.That(NetworkSequenceComparer.IsNewer(65535, 0), Is.False);
        }

        [Test]
        public void UIntSequence_RecognizesSimpleIncrement()
        {
            Assert.That(NetworkSequenceComparer.IsNewer(101u, 100u), Is.True);
        }

        [Test]
        public void UIntSequence_RejectsSameOrOlderValue()
        {
            Assert.That(NetworkSequenceComparer.IsNewer(100u, 100u), Is.False);
            Assert.That(NetworkSequenceComparer.IsNewer(99u, 100u), Is.False);
        }

        [Test]
        public void UIntSequence_HandlesWrapAround()
        {
            Assert.That(NetworkSequenceComparer.IsNewer(0u, 4294967295u), Is.True);
            Assert.That(NetworkSequenceComparer.IsNewer(4294967295u, 0u), Is.False);
        }
    }
}
