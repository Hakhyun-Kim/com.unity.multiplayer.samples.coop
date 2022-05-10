using System;
using System.Collections;
using TMPro;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    public class IPConnectionWindow : MonoBehaviour
    {
        [SerializeField]
        CanvasGroup m_CanvasGroup;

        [SerializeField]
        TextMeshProUGUI m_TitleText;

        IPUIMediator m_IPUIMediator;

        IPublisher<ConnectStatus> m_ConnectStatusPublisher;

        [Inject]
        void InjectDependencies(IPUIMediator ipUIMediator, IPublisher<ConnectStatus> connectStatusPublisher)
        {
            m_IPUIMediator = ipUIMediator;
            m_ConnectStatusPublisher = connectStatusPublisher;
        }

        void Awake()
        {
            Hide();
        }

        void Show()
        {
            m_CanvasGroup.alpha = 1f;
            m_CanvasGroup.blocksRaycasts = true;
        }

        void Hide()
        {
            m_CanvasGroup.alpha = 0f;
            m_CanvasGroup.blocksRaycasts = false;
        }

        public void ShowConnectingWindow()
        {
            var chosenTransport = NetworkManager.Singleton.gameObject.GetComponent<TransportPicker>().IpHostTransport;
            NetworkManager.Singleton.NetworkConfig.NetworkTransport = chosenTransport;

            void OnTimeElapsed()
            {
                m_ConnectStatusPublisher.Publish(ConnectStatus.StartClientFailed);
                Hide();
                m_IPUIMediator.DisableSignInSpinner();
            }

            switch (chosenTransport)
            {
                case UNetTransport unetTransport:
                    StartCoroutine(WaitUntilUNETDisconnected(OnTimeElapsed));
                    break;
                case UnityTransport unityTransport:
                    var maxConnectAttempts = unityTransport.MaxConnectAttempts;
                    var connectTimeoutMS = unityTransport.ConnectTimeoutMS;
                    StartCoroutine(DisplayUTPConnectionDuration(maxConnectAttempts, connectTimeoutMS, OnTimeElapsed));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(chosenTransport));
            }

            Show();
        }

        public void CancelConnectionWindow()
        {
            Hide();
            StopAllCoroutines();
        }

        IEnumerator WaitUntilUNETDisconnected(Action endAction)
        {
            yield return new WaitUntil(() => !NetworkManager.Singleton.IsListening);

            endAction();
        }

        IEnumerator DisplayUTPConnectionDuration(int maxReconnectAttempts, int connectTimeoutMS, Action endAction)
        {
            var connectionDuration = maxReconnectAttempts * connectTimeoutMS / 1000f;

            var seconds = Mathf.CeilToInt(connectionDuration);

            while (seconds > 0)
            {
                m_TitleText.text = $"Connecting...\n{seconds}";
                yield return new WaitForSeconds(1f);
                seconds--;
            }
            m_TitleText.text = "Connecting...";

            endAction();
        }

        // invoked by UI cancel button
        public void OnCancelJoinButtonPressed()
        {
            CancelConnectionWindow();
            m_IPUIMediator.JoiningWindowCancelled();
        }
    }
}