using UnityEngine;
using Cinemachine;
using static PlayerState;

public class CameraManager : MonoBehaviour
{

    [Header("Refrences")]
    [SerializeField] private PlayerState state;
    [SerializeField] private CinemachineVirtualCamera cam;
    private CinemachineFramingTransposer transposer;

    [Header("X Offset Settings")]
    [SerializeField] private float flipSpeed = 5f;
    [SerializeField] private float howFar = 1f;

    [Header("Y Settings")]
    [SerializeField] private float jumpYDamping = 2f;
    [SerializeField] private float fallYDamping = 0.3f;
    [SerializeField] private float dampingLerpSpeed = 5f;


    private void Awake()
    {
        transposer = cam.GetCinemachineComponent<CinemachineFramingTransposer>();
    }

    void Update()
    {
        xOffset();

        yDamping();
    }

    private void xOffset()
    {
        float targetX = state.IsFacingRight ? howFar : -howFar;

        float tempFlipSpeed = flipSpeed;
        tempFlipSpeed = state.IsGrounded ? flipSpeed : flipSpeed / 2;

        Vector3 offset = transposer.m_TrackedObjectOffset;

        offset.x = Mathf.Lerp(
            offset.x,
            targetX,
            tempFlipSpeed * Time.deltaTime
        );

        transposer.m_TrackedObjectOffset = offset;
    }

    private void yDamping()
    {
        float targetDamping;

        if (state.CurrentState == PlayerStateType.Jump)
        {
            // going up
            targetDamping = jumpYDamping;
        }
        else
        {
            // falling
            targetDamping = fallYDamping;
        }

        transposer.m_YDamping = Mathf.Lerp(
            transposer.m_YDamping,
            targetDamping,
            dampingLerpSpeed * Time.deltaTime
        );

    }
}
