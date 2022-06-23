using System.Collections;
using UnityEngine;

public class CommandCameraController : MonoBehaviour
{
    public Camera _camera;
    void Start()
    {
        _camera = GetComponent<Camera>();
    }
    void Update()
    {
        if(Input.GetKey(KeyCode.LeftArrow))
        {
            transform.Translate(Vector3.left);
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            transform.Translate(Vector3.right);
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            transform.Translate(Vector3.up);
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            transform.Translate(Vector3.down);
        }

        MouseScroll(Input.GetAxis("Mouse ScrollWheel"));

    }

    private void MouseScroll(float Scroll)
    {

        if (Scroll > 0)
        {
            var target = _camera.transform.position + (_camera.transform.forward) * 10;
            StartCoroutine(Transition(target));
        }
        if (Scroll < 0)
        {
            var target = _camera.transform.position + (-_camera.transform.forward) * 10;
            StartCoroutine(Transition(target));
        }
    }

    IEnumerator Transition(Vector3 target)
    {
        float t = 0.0f;
        Vector3 startingPos = _camera.transform.position;
        while (t < 1.0f)
        {
            t += Time.deltaTime * (Time.timeScale / (1 / (2 * 3)));

            _camera.transform.position = Vector3.Lerp(startingPos, target, t);
            yield return 0;
        }

    }
}
