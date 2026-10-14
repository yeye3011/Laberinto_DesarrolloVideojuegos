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

        transform.rotation =
            Quaternion.LookRotation(
                camara.transform.position - transform.position,
                Vector3.up
            );
    }
}
