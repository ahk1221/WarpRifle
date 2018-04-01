using System.Collections;
using UnityEngine;

namespace WarpRifle
{
    public class WarpedObject : MonoBehaviour
    {
        Rigidbody rb;

        void OnEnable()
        {
            rb = GetComponent<Rigidbody>();

            rb.isKinematic = true;
            StartCoroutine(DestroyObj());
        }

        IEnumerator DestroyObj()
        {
            yield return new WaitForSeconds(2f);

            rb.isKinematic = false;

            DestroyImmediate(this);
        }
    }
}
