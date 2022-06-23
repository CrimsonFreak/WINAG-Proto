using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Camera cam;
    [Range(0, 5)]
    public float Speed;
    public int Radius;
    readonly int ScreenX = Screen.width / 2;
    readonly int ScreenY = Screen.height / 2;
    Vector3 target;
    Vector3 prev;

    void Update()
    {
        if (!cam.enabled) return;

        Vector2 mousePosition = Input.mousePosition;
        float border = mousePosition.y;
        var t = (transform.position.y / 2) * (Speed/100);

        if (Input.GetKey(KeyCode.LeftControl))
        {
            if(Input.GetKeyDown(KeyCode.UpArrow)) ShiftCamUp();
            if (Input.GetKeyDown(KeyCode.DownArrow)) ShiftCamDown();
        }

        MouseScroll(Input.GetAxis("Mouse ScrollWheel"));
        if (Input.GetMouseButton(2))
        {
            Ray ray = cam.ScreenPointToRay(new Vector2(ScreenX, ScreenY));
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                target = new Vector3(hit.point.x, hit.point.y, hit.point.z);
                RotateCameraY(Input.mousePosition);
            }
        }

        else if (mousePosition.x <= Radius)
        {
            transform.position = Vector3.Lerp(transform.position, transform.position + (Quaternion.Euler(0, transform.eulerAngles.y, 0) * Vector3.left), t);
        }

        else if (mousePosition.x >= cam.pixelWidth - Radius)
        {
            transform.position = Vector3.Lerp(transform.position, transform.position + (Quaternion.Euler(0, transform.eulerAngles.y, 0) * Vector3.right), t);
        }


        else if (border <= Radius)
        {
            transform.position = Vector3.Lerp(transform.position, transform.position + (Quaternion.Euler(0, transform.eulerAngles.y, 0) * Vector3.back), t);
        }
        else if (border > cam.pixelHeight - Radius)
        {
            transform.position = Vector3.Lerp(transform.position, transform.position + (Quaternion.Euler(0, transform.eulerAngles.y, 0) * Vector3.forward), t);
        }
    }
    private void RotateCameraY(Vector3 val)
    {
        if (val.x - prev.x > 0)
        {
            transform.RotateAround(target, Vector3.up, Speed * 100 * Time.deltaTime);
            cam.transform.LookAt(target);
            prev = val;
        }
        else if (val.x - prev.x < 0)
        {
            transform.RotateAround(target, Vector3.up, Speed * -100 * Time.deltaTime);
            cam.transform.LookAt(target);
            prev = val;
        }

    }

    private void MouseScroll(float Scroll)
    {

        if (Scroll > 0)
        {
            var target = cam.transform.position + (cam.transform.forward) * 10;
            StartCoroutine(Transition(target));
        }
        if (Scroll < 0)
        {
            var target = cam.transform.position + (-cam.transform.forward) * 10;
            StartCoroutine(Transition(target));
        }
    }

    public void ShiftCamUp()
    {
        transform.position = Vector3.Slerp(transform.position, transform.position + new Vector3(0,5,5), Speed);
        transform.Rotate(new Vector3(45, transform.eulerAngles.y, 0));
    }

    public void ShiftCamDown()
    {
        transform.position = Vector3.Slerp(transform.position, transform.position + new Vector3(0, -5, -5), Speed);
        transform.Rotate(new Vector3(-45, transform.eulerAngles.y, 0));
    }

    IEnumerator Transition(Vector3 target)
    {
        float t = 0.0f;
        Vector3 startingPos = cam.transform.position;
        while (t < 1.0f)
        {
            t += Time.deltaTime * (Time.timeScale / (1 / (2 * Speed)));

            cam.transform.position = Vector3.Lerp(startingPos, target, t);
            yield return 0;
        }

    }

}
