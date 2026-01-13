using UnityEngine;
//using UnityEngine.UI;
using NaughtyAttributes;

public enum CustomerEmotion { Talking, Thinking, Angry, Happy }

[CreateAssetMenu(fileName = "Customer", menuName = "CustomerSO")]
public class CustomerSO : ScriptableObject
{
    [SerializeField] string customerName;
    [Foldout("Customer Images")] [SerializeField] Sprite customerPhoto;
    [Foldout("Customer Images")] [SerializeField] Sprite happyPhoto;
    [Foldout("Customer Images")] [SerializeField] Sprite angryPhoto;
    [Foldout("Customer Images")] [SerializeField] Sprite talkingPhoto;
    [Foldout("Customer Images")] [SerializeField] Sprite thinkingPhoto;

    [SerializeField] string[] introductionLine;
    [SerializeField] string[] dislikeLine;
    [SerializeField] string[] loveLine;

    public string CustomerName => customerName;

    public string[] GetIntroductionLines() => introductionLine;
    public string[] GetDislikeLines() => dislikeLine;
    public string[] GetLoveLines() => loveLine;

    public Sprite GetSpriteForEmotion(CustomerEmotion emotion)
    {
        switch (emotion)
        {
            case CustomerEmotion.Happy: return happyPhoto;
            case CustomerEmotion.Angry: return angryPhoto;
            case CustomerEmotion.Thinking: return thinkingPhoto;
            default: return talkingPhoto;
        }
    }
}
