using System;
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
        public void StartSession_DirectProvider_Host_DelegatesToUnderlyingSession()
        {
            string previousProvider = Environment.GetEnvironmentVariable("INTERSTELLA_PROVIDER");
            GameObject root = new GameObject("SteamSessionServiceTests_Host");
            try
            {
                Environment.SetEnvironmentVariable("INTERSTELLA_PROVIDER", null);

                FakeNetworkSessionService fakeSession = root.AddComponent<FakeNetworkSessionService>();
                fakeSession.Configure(isHost: true, startResult: true);

                SteamSessionService steamSession = root.AddComponent<SteamSessionService>();
                SetPrivateField(steamSession, "_networkSessionBehaviour", fakeSession);
                SetPrivateField(steamSession, "_allowRuntimeOverride", false);

                bool started = steamSession.StartSession();

                Assert.That(started, Is.True);
                Assert.That(fakeSession.StartCallCount, Is.EqualTo(1));
                Assert.That(steamSession.ActiveLobbyId, Is.EqualTo(string.Empty));
                Assert.That(steamSession.ActiveHostSteamId, Is.EqualTo(string.Empty));
                Assert.That(steamSession.StateName, Is.EqualTo("SessionActive"));
            }
            finally
            {
                Environment.SetEnvironmentVariable("INTERSTELLA_PROVIDER", previousProvider);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void StartSession_ClientWithoutInvite_Fails()
        {
            string previousProvider = Environment.GetEnvironmentVariable("INTERSTELLA_PROVIDER");
            GameObject root = new GameObject("SteamSessionServiceTests_ClientFail");
            try
            {
                Environment.SetEnvironmentVariable("INTERSTELLA_PROVIDER", "steam");

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
                Environment.SetEnvironmentVariable("INTERSTELLA_PROVIDER", previousProvider);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void StartSession_ClientWithQueuedInvite_Succeeds()
        {
            string previousProvider = Environment.GetEnvironmentVariable("INTERSTELLA_PROVIDER");
            GameObject root = new GameObject("SteamSessionServiceTests_ClientInvite");
            try
            {
                Environment.SetEnvironmentVariable("INTERSTELLA_PROVIDER", "steam");

                FakeNetworkSessionService fakeSession = root.AddComponent<FakeNetworkSessionService>();
                fakeSession.Configure(isHost: false, startResult: true);

                FakeSteamLobbyService fakeLobbyService = root.AddComponent<FakeSteamLobbyService>();
                fakeLobbyService.ConfigureJoin(result: true, "host-001");

                SteamSessionService steamSession = root.AddComponent<SteamSessionService>();
                SetPrivateField(steamSession, "_networkSessionBehaviour", fakeSession);
                SetPrivateField(steamSession, "_steamLobbyServiceBehaviour", fakeLobbyService);
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
                Environment.SetEnvironmentVariable("INTERSTELLA_PROVIDER", previousProvider);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void StartSession_DirectProvider_ClientWithoutInvite_DelegatesToUnderlyingSession()
        {
            string previousProvider = Environment.GetEnvironmentVariable("INTERSTELLA_PROVIDER");
            GameObject root = new GameObject("SteamSessionServiceTests_DirectClient");
            try
            {
                Environment.SetEnvironmentVariable("INTERSTELLA_PROVIDER", null);

                FakeNetworkSessionService fakeSession = root.AddComponent<FakeNetworkSessionService>();
                fakeSession.Configure(isHost: false, startResult: true);

                SteamSessionService steamSession = root.AddComponent<SteamSessionService>();
                SetPrivateField(steamSession, "_networkSessionBehaviour", fakeSession);
                SetPrivateField(steamSession, "_allowRuntimeOverride", false);

                bool started = steamSession.StartSession();

                Assert.That(started, Is.True);
                Assert.That(fakeSession.StartCallCount, Is.EqualTo(1));
                Assert.That(steamSession.ActiveLobbyId, Is.EqualTo(string.Empty));
                Assert.That(steamSession.ActiveHostSteamId, Is.EqualTo(string.Empty));
                Assert.That(steamSession.StateName, Is.EqualTo("SessionActive"));
            }
            finally
            {
                Environment.SetEnvironmentVariable("INTERSTELLA_PROVIDER", previousProvider);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void StartSession_HostSteamProvider_UsesLobbyServiceCreate()
        {
            const string lobbyId = "76561198000012345";
            const string hostSteamId = "76561198000000000";
            string previousProvider = Environment.GetEnvironmentVariable("INTERSTELLA_PROVIDER");
            GameObject root = new GameObject("SteamSessionServiceTests_HostSteamLobby");
            try
            {
                Environment.SetEnvironmentVariable("INTERSTELLA_PROVIDER", "steam");

                FakeNetworkSessionService fakeSession = root.AddComponent<FakeNetworkSessionService>();
                fakeSession.Configure(isHost: true, startResult: true);

                FakeSteamLobbyService fakeLobbyService = root.AddComponent<FakeSteamLobbyService>();
                fakeLobbyService.ConfigureCreate(result: true, lobbyId, hostSteamId);

                SteamSessionService steamSession = root.AddComponent<SteamSessionService>();
                SetPrivateField(steamSession, "_networkSessionBehaviour", fakeSession);
                SetPrivateField(steamSession, "_steamLobbyServiceBehaviour", fakeLobbyService);
                SetPrivateField(steamSession, "_allowRuntimeOverride", false);

                bool started = steamSession.StartSession();

                Assert.That(started, Is.True);
                Assert.That(fakeLobbyService.CreateCallCount, Is.EqualTo(1));
                Assert.That(fakeSession.StartCallCount, Is.EqualTo(1));
                Assert.That(steamSession.ActiveLobbyId, Is.EqualTo(lobbyId));
                Assert.That(steamSession.ActiveHostSteamId, Is.EqualTo(hostSteamId));
            }
            finally
            {
                Environment.SetEnvironmentVariable("INTERSTELLA_PROVIDER", previousProvider);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void StartSession_ClientSteamProvider_WithQueuedInvite_JoinsThroughLobbyService()
        {
            const string lobbyId = "76561198000054321";
            const string hostSteamId = "76561198000099999";
            string previousProvider = Environment.GetEnvironmentVariable("INTERSTELLA_PROVIDER");
            GameObject root = new GameObject("SteamSessionServiceTests_ClientSteamLobby");
            try
            {
                Environment.SetEnvironmentVariable("INTERSTELLA_PROVIDER", "steam");

                FakeNetworkSessionService fakeSession = root.AddComponent<FakeNetworkSessionService>();
                fakeSession.Configure(isHost: false, startResult: true);

                FakeSteamLobbyService fakeLobbyService = root.AddComponent<FakeSteamLobbyService>();
                fakeLobbyService.ConfigureJoin(result: true, hostSteamId);

                SteamSessionService steamSession = root.AddComponent<SteamSessionService>();
                SetPrivateField(steamSession, "_networkSessionBehaviour", fakeSession);
                SetPrivateField(steamSession, "_steamLobbyServiceBehaviour", fakeLobbyService);
                SetPrivateField(steamSession, "_allowRuntimeOverride", false);
                steamSession.QueueInvite(lobbyId, string.Empty);

                bool started = steamSession.StartSession();

                Assert.That(started, Is.True);
                Assert.That(fakeLobbyService.JoinCallCount, Is.EqualTo(1));
                Assert.That(fakeLobbyService.LastJoinLobbyId, Is.EqualTo(lobbyId));
                Assert.That(fakeSession.StartCallCount, Is.EqualTo(1));
                Assert.That(steamSession.ActiveLobbyId, Is.EqualTo(lobbyId));
                Assert.That(steamSession.ActiveHostSteamId, Is.EqualTo(hostSteamId));
            }
            finally
            {
                Environment.SetEnvironmentVariable("INTERSTELLA_PROVIDER", previousProvider);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void StartSession_ClientSteamProvider_ConsumesPendingSteamInvite()
        {
            const string lobbyId = "76561198000123456";
            const string hostSteamId = "76561198000999999";
            string previousProvider = Environment.GetEnvironmentVariable("INTERSTELLA_PROVIDER");
            GameObject root = new GameObject("SteamSessionServiceTests_PendingSteamInvite");
            try
            {
                Environment.SetEnvironmentVariable("INTERSTELLA_PROVIDER", "steam");

                FakeNetworkSessionService fakeSession = root.AddComponent<FakeNetworkSessionService>();
                fakeSession.Configure(isHost: false, startResult: true);

                FakeSteamLobbyService fakeLobbyService = root.AddComponent<FakeSteamLobbyService>();
                fakeLobbyService.ConfigureJoin(result: true, hostSteamId);
                fakeLobbyService.QueuePendingInvite(lobbyId);

                SteamSessionService steamSession = root.AddComponent<SteamSessionService>();
                SetPrivateField(steamSession, "_networkSessionBehaviour", fakeSession);
                SetPrivateField(steamSession, "_steamLobbyServiceBehaviour", fakeLobbyService);

                bool started = steamSession.StartSession();

                Assert.That(started, Is.True);
                Assert.That(fakeLobbyService.JoinCallCount, Is.EqualTo(1));
                Assert.That(fakeLobbyService.LastJoinLobbyId, Is.EqualTo(lobbyId));
                Assert.That(steamSession.ActiveLobbyId, Is.EqualTo(lobbyId));
                Assert.That(steamSession.ActiveHostSteamId, Is.EqualTo(hostSteamId));
            }
            finally
            {
                Environment.SetEnvironmentVariable("INTERSTELLA_PROVIDER", previousProvider);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void StartSession_HostSteamProvider_WithAutoInvite_SendsInvite()
        {
            const string lobbyId = "76561198000111111";
            const string hostSteamId = "76561198000222222";
            const string inviteeSteamId = "76561198000333333";
            string previousProvider = Environment.GetEnvironmentVariable("INTERSTELLA_PROVIDER");
            GameObject root = new GameObject("SteamSessionServiceTests_HostAutoInvite");
            try
            {
                Environment.SetEnvironmentVariable("INTERSTELLA_PROVIDER", "steam");

                FakeNetworkSessionService fakeSession = root.AddComponent<FakeNetworkSessionService>();
                fakeSession.Configure(isHost: true, startResult: true);

                FakeSteamLobbyService fakeLobbyService = root.AddComponent<FakeSteamLobbyService>();
                fakeLobbyService.ConfigureCreate(result: true, lobbyId, hostSteamId);
                fakeLobbyService.ConfigureInvite(result: true);

                SteamSessionService steamSession = root.AddComponent<SteamSessionService>();
                SetPrivateField(steamSession, "_networkSessionBehaviour", fakeSession);
                SetPrivateField(steamSession, "_steamLobbyServiceBehaviour", fakeLobbyService);
                SetPrivateField(steamSession, "_allowRuntimeOverride", false);
                SetPrivateField(steamSession, "_autoInviteFriendSteamId", inviteeSteamId);

                bool started = steamSession.StartSession();

                Assert.That(started, Is.True);
                Assert.That(fakeLobbyService.CreateCallCount, Is.EqualTo(1));
                Assert.That(fakeLobbyService.InviteCallCount, Is.EqualTo(1));
                Assert.That(fakeLobbyService.LastInviteLobbyId, Is.EqualTo(lobbyId));
                Assert.That(fakeLobbyService.LastInviteTargetSteamId, Is.EqualTo(inviteeSteamId));
            }
            finally
            {
                Environment.SetEnvironmentVariable("INTERSTELLA_PROVIDER", previousProvider);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TryInviteUserToActiveLobby_HostSteamProvider_DelegatesToLobbyService()
        {
            const string lobbyId = "76561198000444444";
            const string inviteeSteamId = "76561198000555555";
            string previousProvider = Environment.GetEnvironmentVariable("INTERSTELLA_PROVIDER");
            GameObject root = new GameObject("SteamSessionServiceTests_DirectInvite");
            try
            {
                Environment.SetEnvironmentVariable("INTERSTELLA_PROVIDER", "steam");

                FakeNetworkSessionService fakeSession = root.AddComponent<FakeNetworkSessionService>();
                fakeSession.Configure(isHost: true, startResult: true);

                FakeSteamLobbyService fakeLobbyService = root.AddComponent<FakeSteamLobbyService>();
                fakeLobbyService.ConfigureInvite(result: true);

                SteamSessionService steamSession = root.AddComponent<SteamSessionService>();
                SetPrivateField(steamSession, "_networkSessionBehaviour", fakeSession);
                SetPrivateField(steamSession, "_steamLobbyServiceBehaviour", fakeLobbyService);
                SetPrivateField(steamSession, "_allowRuntimeOverride", false);
                SetPrivateField(steamSession, "_activeLobbyId", lobbyId);

                bool invited = steamSession.TryInviteUserToActiveLobby(inviteeSteamId, out string details);

                Assert.That(invited, Is.True);
                Assert.That(details, Does.Contain("invite"));
                Assert.That(fakeLobbyService.InviteCallCount, Is.EqualTo(1));
                Assert.That(fakeLobbyService.LastInviteLobbyId, Is.EqualTo(lobbyId));
                Assert.That(fakeLobbyService.LastInviteTargetSteamId, Is.EqualTo(inviteeSteamId));
            }
            finally
            {
                Environment.SetEnvironmentVariable("INTERSTELLA_PROVIDER", previousProvider);
                UnityEngine.Object.DestroyImmediate(root);
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

        private sealed class FakeSteamLobbyService : MonoBehaviour, ISteamLobbyService
        {
            private bool _createResult = true;
            private string _createdLobbyId = "76561198000000001";
            private string _createdHostSteamId = "76561198000000002";
            private bool _joinResult = true;
            private string _joinedHostSteamId = "76561198000000003";
            private bool _inviteResult = true;
            private string _pendingInviteLobbyId = string.Empty;

            public int CreateCallCount { get; private set; }
            public int JoinCallCount { get; private set; }
            public int InviteCallCount { get; private set; }
            public int LeaveCallCount { get; private set; }
            public string LastJoinLobbyId { get; private set; }
            public string LastInviteLobbyId { get; private set; }
            public string LastInviteTargetSteamId { get; private set; }
            public string LastLeaveLobbyId { get; private set; }

            public void ConfigureCreate(bool result, string lobbyId, string hostSteamId)
            {
                _createResult = result;
                _createdLobbyId = lobbyId;
                _createdHostSteamId = hostSteamId;
            }

            public void ConfigureJoin(bool result, string hostSteamId)
            {
                _joinResult = result;
                _joinedHostSteamId = hostSteamId;
            }

            public void ConfigureInvite(bool result)
            {
                _inviteResult = result;
            }

            public void QueuePendingInvite(string lobbyId)
            {
                _pendingInviteLobbyId = lobbyId;
            }

            public bool TryCreateLobby(out string lobbyId, out string hostSteamId, out string details)
            {
                CreateCallCount++;
                lobbyId = _createdLobbyId;
                hostSteamId = _createdHostSteamId;
                details = _createResult ? "fake create ok" : "fake create failed";
                return _createResult;
            }

            public bool TryJoinLobby(string lobbyId, out string hostSteamId, out string details)
            {
                JoinCallCount++;
                LastJoinLobbyId = lobbyId;
                hostSteamId = _joinedHostSteamId;
                details = _joinResult ? "fake join ok" : "fake join failed";
                return _joinResult;
            }

            public bool TryConsumePendingInvite(out string lobbyId)
            {
                lobbyId = _pendingInviteLobbyId;
                if (string.IsNullOrWhiteSpace(lobbyId))
                {
                    return false;
                }

                _pendingInviteLobbyId = string.Empty;
                return true;
            }

            public bool TryInviteUser(string lobbyId, string targetSteamId, out string details)
            {
                InviteCallCount++;
                LastInviteLobbyId = lobbyId;
                LastInviteTargetSteamId = targetSteamId;
                details = _inviteResult ? "fake invite ok" : "fake invite failed";
                return _inviteResult;
            }

            public void LeaveLobby(string lobbyId)
            {
                LeaveCallCount++;
                LastLeaveLobbyId = lobbyId;
            }
        }
    }
}
