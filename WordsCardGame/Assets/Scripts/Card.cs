using UnityEngine;
using System;
using DG.Tweening;
using TMPro;

public class Card : MonoBehaviour
{
    private enum Face { Cover, Front, Reverse }
 
    [Header("Flip")]
    [SerializeField] private Transform flipRoot;
    [SerializeField] private float flipDuration = 0.45f;
    [SerializeField] private bool flipOnClick = true;
 
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
 
    [Header("Config (ScriptableObjects)")]
    [SerializeField] private RarityConfig rarityConfig;
    [SerializeField] private CategoryConfig categoryConfig;
 
    public event Action<Card> Revealed;
 
    private Vector3 _baseScale = Vector3.one;
    private Face _face = Face.Cover;
    private bool _animating;
    private bool _freeFlip;
 
    private Transform Root => flipRoot != null ? flipRoot : transform;
 
    private void Awake() => _baseScale = Root.localScale;
 
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
 
        _animating = false;
        _freeFlip = false;
        Root.localScale = _baseScale;
        ShowFace(Face.Cover);
    }
 
    public void PlayReveal()
    {
        Root.localScale = Vector3.zero;
        Root.DOScale(_baseScale, 0.35f).SetEase(Ease.OutBack);
    }
 
    public void RevealFront()
    {
        if (_animating || _face != Face.Cover) return;
        FlipTo(Face.Front, () => Revealed?.Invoke(this));
    }
    public void EnableFreeFlip() => _freeFlip = true;
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
 
    private void OnMouseDown()
    {
        if (!flipOnClick || _animating) return;
 
        if (_face == Face.Cover)
            RevealFront();                                
        else if (_freeFlip)
            FlipTo(_face == Face.Front ? Face.Reverse : Face.Front);  
    }
}