using System;
using UnityEngine;
using TMPro;
using DG.Tweening;
using Random = UnityEngine.Random;

public class Card : MonoBehaviour
{
    private enum Face { Cover, Front, Reverse }
 
    [Header("Flip")]
    [SerializeField] private Transform flipRoot;
    [SerializeField] private float flipDuration = 0.45f;
 
    [Header("Interaction")]
    [SerializeField] private Collider2D clickCollider;  
 
    [Header("Faces")]
    [SerializeField] private GameObject coverFace;   
    [SerializeField] private GameObject frontFace;      
    [SerializeField] private GameObject backFace;      
 
    [Header("Front")]
    [SerializeField] private SpriteRenderer backgroundRarityColor;
    [SerializeField] private TMP_Text wordText;
    [SerializeField] private SpriteRenderer iconCategory;
 
    [Header("Reverse")]
    [SerializeField] private SpriteRenderer reverseBackground;
    [SerializeField] private TMP_Text backWordText;
    [SerializeField] private TMP_Text grammarText;
    [SerializeField] private TMP_Text definitionText;
 
    [Header("Cover")]
    [SerializeField] private SpriteRenderer coverBackground;   
 
    [Header("Config (ScriptableObjects)")]
    [SerializeField] private RarityConfig rarityConfig;
    [SerializeField] private CategoryConfig categoryConfig;
 
    public event Action<Card> Revealed;
 
    private Vector3 _baseScale = Vector3.one;
    private Face _face = Face.Cover;
    private bool _animating;
 
    private Transform Root => flipRoot != null ? flipRoot : transform;
 
    private void Awake()
    {
        _baseScale = Root.localScale;
        if (clickCollider == null) clickCollider = GetComponent<Collider2D>();
    }
 
    private void OnDestroy()
    {
        Root.DOKill();
        transform.DOKill();
    }
 
    public void Paint(WordData w)
    {
        wordText.text = w.word;
        backgroundRarityColor.color = rarityConfig.GetColor(w.rarity);
        iconCategory.sprite = categoryConfig.GetIcon(w.category);
 
        if (reverseBackground) reverseBackground.color = rarityConfig.GetColor(w.rarity);
        if (backWordText) backWordText.text = w.word;
        if (grammarText) grammarText.text = w.GrammaticalDescription();
        if (definitionText)
            definitionText.text = string.IsNullOrEmpty(w.shortDefinition) ? "—" : w.shortDefinition;

        if (coverBackground)
            coverBackground.color = Random.ColorHSV(0f, 1f, 0.5f, 0.9f, 0.7f, 1f);
 
        _animating = false;
        Root.localScale = _baseScale;
        ShowFace(Face.Cover);
    }

    public void SetInteractable(bool on)
    {
        if (clickCollider) clickCollider.enabled = on;
    }

    public void PlayReveal()
    {
        Root.localScale = Vector3.zero;
        Root.DOScale(_baseScale, 0.35f).SetEase(Ease.OutBack);
    }
 
    public void Tap()
    {
        if (_animating) return;
        if (_face == Face.Cover)
            FlipTo(Face.Front, () => Revealed?.Invoke(this));
        else
            FlipTo(_face == Face.Front ? Face.Reverse : Face.Front);
    }
 
    private void FlipTo(Face target, Action onComplete = null)
    {
        _animating = true;
        float half = flipDuration * 0.5f;
        Sequence seq = DOTween.Sequence();
        seq.Append(Root.DOScaleX(0f, half).SetEase(Ease.InQuad));
        seq.AppendCallback(() => ShowFace(target));
        seq.Append(Root.DOScaleX(_baseScale.x, half).SetEase(Ease.OutQuad));
        seq.OnComplete(() => { _face = target; _animating = false; onComplete?.Invoke(); });
    }
 
    private void ShowFace(Face f)
    {
        if (coverFace) coverFace.SetActive(f == Face.Cover);
        if (frontFace) frontFace.SetActive(f == Face.Front);
        if (backFace)  backFace.SetActive(f == Face.Reverse);
    }
}