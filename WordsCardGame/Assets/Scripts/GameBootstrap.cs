using UnityEngine;
using Palabrario.Data;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private PackController packController;
 
    private LexicalCatalog _catalog;
 
    private void Start()
    {
        StartCoroutine(CatalogLoader.Load(
            onReady: data =>
            {
                _catalog = new LexicalCatalog(data);
                Debug.Log($"Catalog loaded: {_catalog.TotalCount} words " +
                          $"({_catalog.TotalInPool} in the playable pool).");
 
                if (packController != null)
                    packController.OpenPack(_catalog);
                else
                    Debug.LogError("GameBootstrap: 'packController' is not assigned.");
            },
            onError: e => Debug.LogError($"Could not load catalog: {e}")));
    }
    
    public void OpenAnotherPack()
    {
        if (_catalog != null) packController.OpenPack(_catalog);
    }
}