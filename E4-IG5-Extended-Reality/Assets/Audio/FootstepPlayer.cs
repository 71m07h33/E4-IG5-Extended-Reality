using UnityEngine;

public class FootstepPlayer : MonoBehaviour
{
    public AudioClip footstepGrass;
    public AudioClip footstepWood;
    public AudioSource audioSource;
    public LayerMask grassLayer;
    public LayerMask woodLayer;
    public float stepInterval = 0.5f;
    public CharacterController controller;

    private float stepTimer = 0f;

    //void Start()
    //{
    //    controller = transform.root.GetComponentInParent<CharacterController>();
    //} ch

    void Update()
    {
        if (controller.isGrounded && controller.velocity.magnitude > 0.1f)
        {
            stepTimer += Time.deltaTime;
            if (stepTimer > stepInterval)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, Vector3.down, out hit, 2f))
                {
                    if (((1 << hit.collider.gameObject.layer) & grassLayer) != 0)
                        audioSource.PlayOneShot(footstepGrass);
                    else if (((1 << hit.collider.gameObject.layer) & woodLayer) != 0)
                        audioSource.PlayOneShot(footstepWood);
                }
                stepTimer = 0f;
            }
        }
    }
}
