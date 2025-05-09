using UnityEngine;

public class DustTrailController : MonoBehaviour
{
    public ParticleSystem dustTrail;
    public CharacterController controller;

    void Update()
    {
        if (controller == null || dustTrail == null)
            return;

        // Si le joueur est en train de marcher et touche le sol
        if (controller.isGrounded && controller.velocity.magnitude > 0.1f)
        {
            if (!dustTrail.isPlaying)
                dustTrail.Play();
        }
        else
        {
            if (dustTrail.isPlaying)
                dustTrail.Stop();
        }
    }
}
