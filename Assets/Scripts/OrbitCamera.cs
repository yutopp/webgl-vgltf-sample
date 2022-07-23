using UnityEngine;
using UnityEngine.EventSystems;

namespace WebglVGltfSample
{
    // base: https://gist.github.com/gamebox777/5c6882e1424477a19861fa3c674bce9f

    /// <summary>
    /// マウスで操作できるカメラ。対象オブジェクトを中心に見る
    /// 　unityのSceneビューのカメラ操作とほぼ同様
    /// 【使い方】
    /// ・ソースを適当なオブジェクトにAddして使います。
    /// ※CameraはMainCameraを自動取得しています
    /// ・mTargetに中心となるオブジェクトをD&Dしてください
    /// 【操作方法】
    /// ・マウス左：カメラ回転
    /// ・左ALTキー＋マウス左：カメラの平行移動
    /// ・マウスホイール：ズーム
    /// </summary>
    public sealed class OrbitCamera : MonoBehaviour
    {
        ///<summary> メインカメラ </summary>
        Camera mCamera;

        ///<summary> ターゲットキャラ </summary>
        public GameObject mTarget;

        ///<summary>　注視点(座標)のオフセット </summary>
        [Header("座標オフセット初期値")]
        public Vector3 offset = new Vector3(0, 1, 0);

        //初期値
        [System.NonSerialized] public readonly float mInitialDistance = 2f;                 //初期値：距離（ズーム）
        [System.NonSerialized] public readonly float mInitialAzimuthalAngle = 90f;          //初期値：方位角
        [System.NonSerialized] public readonly float mInitialPolarAngle = 90f;              //初期値：仰角
        [System.NonSerialized] public readonly float mInitialFov = 60f;                     //初期値：FOV
        [System.NonSerialized] public readonly float mInitiaMouseRotSensitivity = 5f;       //初期値：カメラ回転感度
        [System.NonSerialized] public readonly float mInitiaMouseZoomSensitivity = 10f;     //初期値：ズーム感度
        [System.NonSerialized] public readonly float mInitiaMouseOffsetSensitivity = 0.1f;  //初期値：カメラ移動感度

        ///<summary> 初期のカメラからの距離 </summary>
        [Header("カメラからの距離(ズーム)")]
        [System.NonSerialized] public float mDistance = 2.0f;

        ///<summary> 方位角 angle with x-axis </summary>
        [Header("方位角")]
        [System.NonSerialized] public float mAzimuthalAngle = 90f;

        ///<summary> 仰角 angle with y-axis </summary>
        [Header("仰角")]
        [System.NonSerialized] public float mPolarAngle = 90f;

        ///<summary> カメラ回転感度_マウス </summary>
        [Header("カメラ回転感度")]
        [SerializeField] public float mMouseCameraRotSensitivity = default;

        ///<summary> カメラ移動感度_マウス </summary>
        [Header("カメラ移動感度")]
        [SerializeField] public float mMouseOffsetSensitivity = default;

        ///<summary> ズーム感度_マウス </summary>
        [Header("ズーム感度")]
        [SerializeField] public float mMouseZoomSensitivity = default;

        ///<summary> カメラの距離_下限 </summary>
        private readonly float mMinDistance = 0.1f;

        ///<summary> カメラの距離_最大 </summary>
        private readonly float mMaxDistance = 30f;

        ///<summary> 仰角_最低値 </summary>
        private readonly float mMinPolarAngle = 1f;

        ///<summary> 仰角_最大値 </summary>
        private readonly float mMaxPolarAngle = 179f;

        private void Start()
        {
            mCamera = GetComponent<Camera>();

            //値初期化
            mDistance = mInitialDistance;                               //距離（ズーム）
            mMouseCameraRotSensitivity = mInitiaMouseRotSensitivity;    //カメラ回転感度：マウス
            mMouseZoomSensitivity = mInitiaMouseZoomSensitivity;        //カメラズーム感度：マウス
            mMouseOffsetSensitivity = mInitiaMouseOffsetSensitivity;    //カメラ移動感度：マウス
            mAzimuthalAngle = mInitialAzimuthalAngle;                   //方位角
            mPolarAngle = mInitialPolarAngle;                           //仰角
        }

        public void LateUpdate()
        {
            if (mTarget == null)
            {
                return;
            }

            float tx = 0, ty = 0;

            ////////////////////////////
            //マウス操作
            ////////////////////////////
            //UI上にマウスカーソルが乗っていない時のみマウスでカメラ操作可能
            bool isCameraControl = true;
            if (EventSystem.current != null)
            {
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    isCameraControl = false;
                }
            }

            if (isCameraControl)
            {
                //カメラ回転
                if (Input.GetMouseButton(0))
                {
                    tx = Input.GetAxis("Mouse X") * mMouseCameraRotSensitivity;
                    ty = Input.GetAxis("Mouse Y") * mMouseCameraRotSensitivity;
                    updateAngle(tx, ty);
                }

                //カメラ移動
                if (Input.GetMouseButton(1))
                {
                    float inputHorizontal = Input.GetAxis("Mouse X") * mMouseOffsetSensitivity;

                    // カメラの方向から、X-Z平面の単位ベクトルを取得
                    Vector3 cameraForward = Vector3.Scale(Camera.main.transform.forward, new Vector3(1, 0, 1)).normalized;

                    // 方向キーの入力値とカメラの向きから、移動方向を決定
                    Vector3 moveForward = Camera.main.transform.right * inputHorizontal;
                    offset -= moveForward;

                    offset.y -= Input.GetAxis("Mouse Y") * mMouseOffsetSensitivity;
                }

                //カメラズーム
                updateDistance(Input.GetAxis("Mouse ScrollWheel") * mMouseZoomSensitivity);
            }

            //
            //計算
            //
            var lookAtPos = mTarget.transform.position + offset;
            updatePosition(lookAtPos);

            mCamera.transform.position = transform.position;
            mCamera.transform.LookAt(lookAtPos);
        }

        /// <summary>
        /// 角度更新
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void updateAngle(float x, float y)
        {
            x = mAzimuthalAngle - x;
            mAzimuthalAngle = Mathf.Repeat(x, 360);

            y = mPolarAngle + y;
            mPolarAngle = Mathf.Clamp(y, mMinPolarAngle, mMaxPolarAngle);
        }

        /// <summary>
        /// 距離（ズーム）更新
        /// </summary>
        /// <param name="scroll"></param>
        void updateDistance(float scroll)
        {
            scroll = mDistance - scroll;
            mDistance = Mathf.Clamp(scroll, mMinDistance, mMaxDistance);
        }

        /// <summary>
        /// 座標更新
        /// </summary>
        /// <param name="lookAtPos"></param>
        void updatePosition(Vector3 lookAtPos)
        {
            var da = mAzimuthalAngle * Mathf.Deg2Rad;
            var dp = mPolarAngle * Mathf.Deg2Rad;
            transform.position = new Vector3(
                lookAtPos.x + mDistance * Mathf.Sin(dp) * Mathf.Cos(da),
                lookAtPos.y + mDistance * Mathf.Cos(dp),
                lookAtPos.z + mDistance * Mathf.Sin(dp) * Mathf.Sin(da));
        }

        /// <summary>
        /// カメラ初期化
        /// </summary>
        public void CameraReset()
        {
            mDistance = mInitialDistance;               //距離（ズーム）
            mAzimuthalAngle = mInitialAzimuthalAngle;   //方位角
            mPolarAngle = mInitialPolarAngle;           //仰角
            mCamera.fieldOfView = mInitialFov;          //FOV
            offset = new Vector3(0, 1, 0);              //オフセット
        }
    }
}
