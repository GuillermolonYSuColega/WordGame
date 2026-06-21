using UnityEngine;
using Palabrario.Data;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private Card cardUI;

    private LexicalCatalog _catalog;

    private void Start()
    {
        StartCoroutine(CatalogLoader.Load(
            onReady: data =>
            {
                _catalog = new LexicalCatalog(data);
                Debug.Log($"Catalog loaded: {_catalog.TotalCount} words " +
                          $"({_catalog.TotalInPool} in playable pool).");
                OpenPack();   
            },
            onError: e => Debug.LogError($"Could not load catalog: {e}")));
    }

    public void OpenPack()
    {
        if (_catalog == null) return;
        WordData card = _catalog.DrawCard();
        if (card == null) return;
        Debug.Log($"Drawn: {card.word}  [{card.rarity}]  ({card.category})");
        if (cardUI != null) cardUI.Paint(card);
    }
}