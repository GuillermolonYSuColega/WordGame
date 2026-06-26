using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using Palabrario.Data;

public class PackController : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Card cardPrefab;
    [SerializeField] private Transform cardParent;
    [SerializeField] private int cardsPerPack = 5;
    [SerializeField] private Vector3 behindOffset = new Vector3(0.06f, -0.08f, 0.12f); 

    [Header("Pack visual")]
    [SerializeField] private Transform packVisual;
    [SerializeField] private float tearDuration = 0.5f;

    [Header("Timing")]
    [SerializeField] private float dealStagger = 0.10f;
    [SerializeField] private float advanceDuration = 0.35f;
    [SerializeField] private Vector3 discardMove = new Vector3(0f, 2.5f, 0f); 
    [SerializeField] private bool ignoreTimeScale = false;

    [Header("Rarity flash")]
    [SerializeField] private RARITY flashFromRarity = RARITY.Epic;
    [SerializeField] private GameObject flashEffectPrefab;

    private readonly List<Card> _stack = new();
    private readonly Dictionary<Card, WordData> _drawn = new();
    private Sequence _sequence;
    private bool _busy;      

    private Vector3 BasePos => cardParent != null ? cardParent.position : transform.position;
    private Vector3 StackSlot(int i) => BasePos + behindOffset * i;

    public void OpenPack(LexicalCatalog catalog)
    {
        if (catalog == null || cardPrefab == null) return;

        _sequence?.Kill();
        ClearCards();

        for (int i = 0; i < cardsPerPack; i++)
        {
            WordData w = catalog.DrawCard();
            if (w == null) continue;

            Card c = Instantiate(cardPrefab, StackSlot(i), Quaternion.identity, cardParent);
            c.Paint(w);
            c.SetInteractable(false);
            c.gameObject.SetActive(false);
            c.Revealed += HandleRevealed;
            _stack.Add(c);
            _drawn[c] = w;
        }

        _busy = true;
        _sequence = DOTween.Sequence().SetUpdate(ignoreTimeScale);

        if (packVisual != null)
        {
            _sequence.Append(packVisual.DOShakePosition(tearDuration * 0.6f, 0.15f, 14, 90));
            _sequence.Append(packVisual.DOScale(0f, tearDuration * 0.4f).SetEase(Ease.InBack));
            _sequence.AppendCallback(() => packVisual.gameObject.SetActive(false));
        }

        foreach (Card c in _stack)
        {
            Card card = c;
            _sequence.AppendCallback(() =>
            {
                card.gameObject.SetActive(true);
                card.PlayReveal();
            });
            _sequence.AppendInterval(dealStagger);
        }

        _sequence.OnComplete(() => { _busy = false; RefreshTop(); });
    }

    private void Update()
    {
        if (_busy || _stack.Count == 0) return;

        Pointer p = Pointer.current;
        if (p == null || !p.press.wasPressedThisFrame) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 world = cam.ScreenToWorldPoint(p.position.ReadValue());
        Collider2D hit = Physics2D.OverlapPoint(world);
        Card hitCard = hit != null ? hit.GetComponentInParent<Card>() : null;

        if (hitCard != null && hitCard == _stack[0])
            _stack[0].Tap();      
        else
            Advance();              
    }

    private void Advance()
    {
        if (_stack.Count == 0) return;

        _busy = true;
        Card leaving = _stack[0];
        _stack.RemoveAt(0);
        leaving.Revealed -= HandleRevealed;
        leaving.SetInteractable(false);

        _sequence = DOTween.Sequence().SetUpdate(ignoreTimeScale);
        _sequence.Append(leaving.transform.DOMove(leaving.transform.position + discardMove, advanceDuration).SetEase(Ease.InBack));
        _sequence.Join(leaving.transform.DOScale(0f, advanceDuration).SetEase(Ease.InBack));
        _sequence.AppendCallback(() => { if (leaving != null) Destroy(leaving.gameObject); });

        for (int i = 0; i < _stack.Count; i++)
            _sequence.Join(_stack[i].transform.DOMove(StackSlot(i), advanceDuration).SetEase(Ease.OutQuad));

        _sequence.OnComplete(() =>
        {
            _busy = false;
            RefreshTop();
        });
    }

    private void RefreshTop()
    {
        for (int i = 0; i < _stack.Count; i++)
            _stack[i].SetInteractable(i == 0);
    }

    private void HandleRevealed(Card card)
    {
        if (_drawn.TryGetValue(card, out WordData w) && IsFlashy(w.rarity))
            PlayFlash(card.transform);
    }

    private bool IsFlashy(RARITY? rarity)
        => rarity.HasValue && (int)rarity.Value >= (int)flashFromRarity;

    private void PlayFlash(Transform at)
    {
        at.DOPunchScale(Vector3.one * 0.15f, 0.35f, 8, 0.6f).SetUpdate(ignoreTimeScale);
        if (flashEffectPrefab != null)
            Instantiate(flashEffectPrefab, at.position, Quaternion.identity, at);
    }

    private void ClearCards()
    {
        foreach (Card c in _stack)
        {
            if (c == null) continue;
            c.Revealed -= HandleRevealed;
            Destroy(c.gameObject);
        }
        _stack.Clear();
        _drawn.Clear();
    }

    private void OnDestroy()
    {
        _sequence?.Kill();
        transform.DOKill();
    }
}