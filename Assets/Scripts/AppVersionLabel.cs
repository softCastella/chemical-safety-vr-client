using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public sealed class AppVersionLabel : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<TMP_Text>().text = "v" + Application.version;
    }
}
