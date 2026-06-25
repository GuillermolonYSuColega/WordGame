using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Palabrario.Data;

public class PackController : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Card cardPrefab;
    [SerializeField] private Transform cardParent;
    [SerializeField] private Transform[] cardSlots;
    [SerializeField] private int cardsPerPack = 5;
    [SerializeField] private float slotSpacing = 2.2f;
 
    [Header("Pack visual")]
    [SerializeField] private Transform packVisual;
    [SerializeField] private float tearDuration = 0.5f;
 
    [Header("Timing")]
    [SerializeField] private float dealStagger = 0.12f;    
    [SerializeField] private bool ignoreTimeScale = false;
 
    [Header("Rarity flash")]
    [SerializeField] private RARITY flashFromRarity = RARITY.Epic;
    [SerializeField] private GameObject flashEffectPrefab;
 
    private readonly List<Card> _spawned = new();
    private readonly Dictionary<Card, WordData> _drawn = new();
    private Sequence _sequence;
    private int _revealedCount;
 
    public void OpenPack(LexicalCatalog catalog)
    {
        if (catalog == null || cardPrefab == null) return;
 
        _sequence?.Kill();
        ClearCards();
        _revealedCount = 0;
 
        for (int i = 0; i < cardsPerPack; i++)
        {
            WordData w = catalog.DrawCard();
            if (w == null) continue;
 
            Card c = Instantiate(cardPrefab, SlotPosition(i), Quaternion.identity, cardParent);
            c.Paint(w);                   
            c.gameObject.SetActive(false);  
            c.Revealed += HandleRevealed;   
            _spawned.Add(c);
            _drawn[c] = w;
        }
 
        _sequence = DOTween.Sequence().SetUpdate(ignoreTimeScale);
 
        if (packVisual != null)
        {
            _sequence.Append(packVisual.DOShakePosition(tearDuration * 0.6f, 0.15f, 14, 90));
            _sequence.Append(packVisual.DOScale(0f, tearDuration * 0.4f).SetEase(Ease.InBack));
            _sequence.AppendCallback(() => packVisual.gameObject.SetActive(false));
        }
 
        foreach (Card c in _spawned)
        {
            _sequence.AppendCallback(() =>
            {
                c.gameObject.SetActive(true);
                c.PlayReveal();           
            });
            _sequence.AppendInterval(dealStagger);
        }
    }
 
    private void HandleRevealed(Card card)
    {
        if (_drawn.TryGetValue(card, out WordData w) && IsFlashy(w.rarity))
            PlayFlash(card.transform);
 
        _revealedCount++;
        if (_revealedCount >= _spawned.Count)
            foreach (Card c in _spawned)
                c.EnableFreeFlip();  
    }
 
    private bool IsFlashy(RARITY? rarity)
        => rarity.HasValue && (int)rarity.Value >= (int)flashFromRarity;
 
    private void PlayFlash(Transform at)
    {
        at.DOPunchScale(Vector3.one * 0.15f, 0.35f, 8, 0.6f).SetUpdate(ignoreTimeScale);
        if (flashEffectPrefab != null)
            Instantiate(flashEffectPrefab, at.position, Quaternion.identity, at);
    }
 
    private Vector3 SlotPosition(int i)
    {
        if (cardSlots != null && i < cardSlots.Length && cardSlots[i] != null)
            return cardSlots[i].position;
 
        float offset = (i - (cardsPerPack - 1) / 2f) * slotSpacing;
        Vector3 basePos = cardParent != null ? cardParent.position : transform.position;
        return basePos + new Vector3(offset, 0f, 0f);
    }
 
    private void ClearCards()
    {
        foreach (Card c in _spawned)
        {
            if (c == null) continue;
            c.Revealed -= HandleRevealed;     
            Destroy(c.gameObject);          
        }
        _spawned.Clear();
        _drawn.Clear();
    }
 
    private void OnDestroy()
    {
        _sequence?.Kill();
        transform.DOKill();
    }
}