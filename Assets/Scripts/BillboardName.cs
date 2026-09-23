using UnityEngine;

public class BillboardName : MonoBehaviour
{
    private Camera camara;

    private void LateUpdate()
    {
        if (camara == null)
        {
            camara = Camera.main;

            if (camara == null)
                return;
        }

        transform.LookAt(
            transform.position + camara.transform.rotation * Vector3.forward,
            camara.transform.rotation * Vector3.up
        );
    }
}
