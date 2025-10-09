using System.Collections;
using UnityEngine;

namespace Mirror.Examples.MultipleMatch
{
    [AddComponentMenu("")]
    public class MatchNetworkManager : NetworkManager
    {
        [Header("Match GUI")]
        public GameObject canvas;
        public CanvasController canvasController;

        /// <summary>
        /// 서버와 클라이언트 모두에서 실행됩니다.
        /// 이 함수가 실행될 때 네트워킹은 초기화되지 않습니다.
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            canvasController.InitializeData();
        }

        #region 서버 시스템 콜백

        /// <summary>
        /// 클라이언트가 준비되면 서버에서 호출됩니다.
        /// <para>이 함수의 기본 구현은 NetworkServer.SetClientReady()를 호출하여 네트워크 설정 프로세스를 계속합니다.</para>
        /// </summary>
        /// <param name="conn">클라이언트로부터의 연결입니다.</param>
        public override void OnServerReady(NetworkConnectionToClient conn)
        {
            base.OnServerReady(conn);
            canvasController.OnServerReady(conn);
        }

        /// <summary>
        /// 클라이언트 연결이 끊어지면 서버에서 호출됩니다.
        /// <para>이것은 클라이언트가 서버에서 연결을 끊을 때 서버에서 호출됩니다. 연결 끊김이 감지되었을 때 수행할 작업을 결정하려면 재정의를 사용하십시오.</para>
        /// </summary>
        /// <param name="conn">클라이언트로부터의 연결입니다.</param>
        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            StartCoroutine(DoServerDisconnect(conn));
        }

        IEnumerator DoServerDisconnect(NetworkConnectionToClient conn)
        {
            yield return canvasController.OnServerDisconnect(conn);
            base.OnServerDisconnect(conn);
        }

        #endregion

        #region 클라이언트 시스템 콜백

        /// <summary>
        /// 서버에서 연결이 끊어지면 클라이언트에서 호출됩니다.
        /// <para>이것은 클라이언트가 서버에서 연결을 끊을 때 클라이언트에서 호출됩니다. 클라이언트 연결이 끊어졌을 때 수행할 작업을 결정하려면 이 함수를 재정의하십시오.</para>
        /// </summary>
        public override void OnClientDisconnect()
        {
            canvasController.OnClientDisconnect();
            base.OnClientDisconnect();
        }

        #endregion

        #region 시작 및 중지 콜백

        /// <summary>
        /// 호스트가 시작될 때를 포함하여 서버가 시작될 때 호출됩니다.
        /// <para>StartServer에는 여러 서명이 있지만 모두 이 후크를 호출합니다.</para>
        /// </summary>
        public override void OnStartServer()
        {
            if (mode == NetworkManagerMode.ServerOnly)
                canvas.SetActive(true);

            canvasController.OnStartServer();
        }

        /// <summary>
        /// 클라이언트가 시작될 때 호출됩니다.
        /// </summary>
        public override void OnStartClient()
        {
            canvas.SetActive(true);
            canvasController.OnStartClient();
        }

        /// <summary>
        /// 호스트가 중지될 때를 포함하여 서버가 중지될 때 호출됩니다.
        /// </summary>
        public override void OnStopServer()
        {
            canvasController.OnStopServer();
            canvas.SetActive(false);
        }

        /// <summary>
        /// 클라이언트가 중지될 때 호출됩니다.
        /// </summary>
        public override void OnStopClient()
        {
            canvasController.OnStopClient();
        }

        #endregion
    }
}