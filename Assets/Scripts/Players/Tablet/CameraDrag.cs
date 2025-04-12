using UnityEngine;

namespace Main
{
	class CameraDrag : MonoBehaviour
	{
		Vector3 targetPos;
		[SerializeField]
		Camera mainCamera;

		[SerializeField]
		float distance;

		[Range(0.1f, 5f)]
		[Tooltip("How sensitive the mouse drag to camera rotation")]
		public float mouseRotateSpeed = 5f;
		[Range(0.01f, 100)]
		[Tooltip("How sensitive the touch drag to camera rotation")]
		public float touchRotateSpeed = 20f;
		[Tooltip("Smaller positive value means smoother rotation, 1 means no smooth apply")]
		public float slerpValue = 0.5f;
		public enum RotateMethod { Mouse, Touch };
		[Tooltip("How do you like to rotate the camera")]
		public RotateMethod rotateMethod = RotateMethod.Mouse;


		private Vector2 swipeDirection; //swipe delta vector2
		private Quaternion cameraRot; // store the quaternion after the slerp operation
		private Touch touch;
		private float distanceBetweenCameraAndTarget;

		private float minXRotAngle = 10; //min angle around x axis
		private float maxXRotAngle = 80; // max angle around x axis

		//Mouse rotation related
		private float rotX = 45; // around x
		private float rotY = 45; // around y
		private void Awake()
		{
			if (mainCamera == null)
			{
				mainCamera = Camera.main;
			}


		}
		// Start is called before the first frame update
		void Start()
		{
			targetPos = BoardGameManager.Instance.AnswerBoardOrigin.transform.position;
			distanceBetweenCameraAndTarget = distance;
		}

		void Update()
		{
			if (rotateMethod == RotateMethod.Mouse)
			{
				if (Input.GetMouseButton(0))
				{
					rotX += -Input.GetAxis("Mouse Y") * mouseRotateSpeed; // around X
					rotY += Input.GetAxis("Mouse X") * mouseRotateSpeed;
				}

				if (rotX < minXRotAngle)
				{
					rotX = minXRotAngle;
				}
				else if (rotX > maxXRotAngle)
				{
					rotX = maxXRotAngle;
				}
			}
			else if (rotateMethod == RotateMethod.Touch)
			{
				if (Input.touchCount > 0)
				{
					touch = Input.GetTouch(0);
					if (touch.phase == TouchPhase.Began)
					{
						//Debug.Log("Touch Began");

					}
					else if (touch.phase == TouchPhase.Moved)
					{
						swipeDirection += touch.deltaPosition * Time.deltaTime * touchRotateSpeed;
					}
					else if (touch.phase == TouchPhase.Ended)
					{
						//Debug.Log("Touch Ended");
					}
				}

				if (swipeDirection.y < minXRotAngle)
				{
					swipeDirection.y = minXRotAngle;
				}
				else if (swipeDirection.y > maxXRotAngle)
				{
					swipeDirection.y = maxXRotAngle;
				}
			}
		}

		private void LateUpdate()
		{
			Vector3 dir = new(0, 0, -distanceBetweenCameraAndTarget); //assign value to the distance between the maincamera and the target
			Quaternion newQ; // value equal to the delta change of our mouse or touch position

			if (rotateMethod == RotateMethod.Mouse)
			{
				newQ = Quaternion.Euler(rotX, rotY, 0); //We are setting the rotation around X, Y, Z axis respectively
			}
			else
			{
				newQ = Quaternion.Euler(swipeDirection.y, swipeDirection.x, 0);
			}

			cameraRot = Quaternion.Slerp(cameraRot, newQ, slerpValue);  //let cameraRot value gradually reach newQ which corresponds to our touch
			mainCamera.transform.position = targetPos + cameraRot * dir;
			mainCamera.transform.LookAt(targetPos);
		}
	}
}