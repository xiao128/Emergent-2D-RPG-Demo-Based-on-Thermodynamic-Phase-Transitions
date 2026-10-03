using TMPro;
using UnityEngine;
namespace PhaseArena
{
    public class RuleFeedback : MonoBehaviour
    {
        float age;
        void Update()
        {
            age+=Time.deltaTime; transform.position+=Vector3.up*Time.deltaTime*.7f;
            if(age>.65f) { var label=GetComponent<TMP_Text>(); if(label!=null) label.alpha=Mathf.Clamp01((1.2f-age)/.55f); }
            if(age>=1.2f) Destroy(gameObject);
        }
    }
}
