// CatalogLoader.cs
// Palabrario · Loads the catalog from StreamingAssets (I/O ONLY).
// -----------------------------------------------------------------------------
// Single responsibility: read the file (gzip or plain) and deserialize it.
// Does NOT query, index, or persist player data.
//
// Reads both .json and .json.gz: if the name ends in .gz, it inflates with
// GZipStream before parsing. (Automatic Content-Encoding gzip only happens on
// HTTP downloads; for a local/cached file you must inflate it yourself.)
//
// Requires: com.unity.nuget.newtonsoft-json (Package Manager).
// -----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public static class CatalogLoader
{
    public const string FileName = "palabrario_dataset.json.gz";

    public static IEnumerator Load(
        Action<List<WordData>> onReady,
        Action<string> onError = null)
    {
        string path = Path.Combine(Application.streamingAssetsPath, FileName);
        byte[] bytes = null;

        bool isUrl = path.Contains("://");
        if (isUrl)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(path))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke($"UnityWebRequest: {req.error}");
                    yield break;
                }
                bytes = req.downloadHandler.data;
            }
        }
        else
        {
            if (!File.Exists(path))
            {
                onError?.Invoke($"File not found: {path}");
                yield break;
            }
            bytes = File.ReadAllBytes(path);
        }

        bool isGzip = FileName.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);
        Task<List<WordData>> task = Task.Run(() =>
        {
            string json = isGzip ? Inflate(bytes) : Encoding.UTF8.GetString(bytes);
            return JsonConvert.DeserializeObject<List<WordData>>(json);
        });

        while (!task.IsCompleted) yield return null;

        if (task.Exception != null)
        {
            onError?.Invoke($"Load/parse: {task.Exception.GetBaseException().Message}");
            yield break;
        }

        onReady?.Invoke(task.Result);
    }

    // Decompresses an in-memory gzip blob and returns the UTF-8 text.
    private static string Inflate(byte[] gzipBytes)
    {
        using var input = new MemoryStream(gzipBytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return Encoding.UTF8.GetString(output.ToArray());
    }
}

