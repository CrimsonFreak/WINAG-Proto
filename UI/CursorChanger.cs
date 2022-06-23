using UnityEngine;
using UnityEngine.EventSystems;

public class CursorChanger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Texture2D ModifiedPointer;
    public Texture2D DefaultPointer;
    
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            Cursor.SetCursor(DefaultPointer, Vector2.zero, CursorMode.ForceSoftware);
        }
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        Cursor.SetCursor(ModifiedPointer, new Vector2(16, 12), CursorMode.ForceSoftware);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (Input.anyKey) return;
        Cursor.SetCursor(DefaultPointer, Vector2.zero, CursorMode.ForceSoftware);
    }
}
