using System.Reflection;
using InterStella.Game.Netcode.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace InterStella.Game.Netcode.Editor.Tests
{
    public sealed class SteamSessionServiceTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void StartSession_HostAutoCreateLobby_Succeeds()
        {
            GameObject root = new GameObject("SteamSessionServiceTests_Host");
            try
            {
                FakeNetworkSessionService fakeSession = root.AddComponent<FakeNetworkSessionService>();
                fakeSession.Configure(isHost: true, startResult: true);

                SteamSessionService steamSession = root.AddComponent<SteamSessionService>();
                SetPrivateField(steamSession, "_networkSessionBehaviour", fakeSession);
                SetPrivateField(steamSession, "_allowRuntimeOverride", false);

                bool started = steamSession.StartSession();

                Assert.That(started, Is.True);
                Assert.That(fakeSession.StartCallCount, Is.EqualTo(1));
                Assert.That(string.IsNullOrWhiteSpace(steamSession.ActiveLobbyId), Is.False);
                Assert.That(steamSession.StateName, Is.EqualTo("SessionActive"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void StartSession_ClientWithoutInvite_Fails()
        {
            GameObject root = new GameObject("SteamSessionServiceTests_ClientFail");
            try
            {
                FakeNetworkSessionService fakeSession = root.AddComponent<FakeNetworkSessionService>();
                fakeSession.Configure(isHost: false, startResult: true);

                SteamSessionService steamSession = root.AddComponent<SteamSessionService>();
                SetPrivateField(steamSession, "_networkSessionBehaviour", fakeSession);
                SetPrivateField(steamSession, "_allowRuntimeOverride", false);

                bool started = steamSession.StartSession();

                Assert.That(started, Is.False);
                Assert.That(fakeSession.StartCallCount, Is.EqualTo(0));
                Assert.That(steamSession.StateName, Is.EqualTo("Failed"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void StartSession_ClientWithQueuedInvite_Succeeds()
        {
            GameObject root = new GameObject("SteamSessionServiceTests_ClientInvite");
            try
            {
                FakeNetworkSessionService fakeSession = root.AddComponent<FakeNetworkSessionService>();
                fakeSession.Configure(isHost: false, startResult: true);

                SteamSessionService steamSession = root.AddComponent<SteamSessionService>();
                SetPrivateField(steamSession, "_networkSessionBehaviour", fakeSession);
                SetPrivateField(steamSession, "_allowRuntimeOverride", false);
                steamSession.QueueInvite("lobby-001", "host-001");

                bool started = steamSession.StartSession();

                Assert.That(started, Is.True);
                Assert.That(fakeSession.StartCallCount, Is.EqualTo(1));
                Assert.That(steamSession.ActiveLobbyId, Is.EqualTo("lobby-001"));
                Assert.That(steamSession.ActiveHostSteamId, Is.EqualTo("host-001"));
                Assert.That(steamSession.StateName, Is.EqualTo("SessionActive"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PrivateInstance);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private sealed class FakeNetworkSessionService : MonoBehaviour, ISessionService
        {
            private bool _isHost;
            private bool _startResult = true;
            private bool _isSessionActive;

            public int StartCallCount { get; private set; }

            public bool IsSessionActive => _isSessionActive;
            public bool IsHost => _isHost;

            public void Configure(bool isHost, bool startResult)
            {
                _isHost = isHost;
                _startResult = startResult;
                _isSessionActive = false;
                StartCallCount = 0;
            }

            public bool StartSession()
            {
                StartCallCount++;
                if (_startResult)
                {
                    _isSessionActive = true;
                    return true;
                }

                _isSessionActive = false;
                return false;
            }

            public void StopSession()
            {
                _isSessionActive = false;
            }
        }
    }
}
