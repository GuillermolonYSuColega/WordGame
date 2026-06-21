using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace Palabrario.Data
{
    public static class CatalogLoader
    {
        public const string FileName = "palabrario_dataset.json";

        public static IEnumerator Load(
            Action<List<WordData>> onReady,
            Action<string> onError = null)
        {
            string path = Path.Combine(Application.streamingAssetsPath, FileName);
            string json = null;

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
                    json = req.downloadHandler.text;
                }
            }
            else
            {
                if (!File.Exists(path))
                {
                    onError?.Invoke($"File not found: {path}");
                    yield break;
                }
                json = File.ReadAllText(path);
            }

            Task<List<WordData>> task = Task.Run(
                () => JsonConvert.DeserializeObject<List<WordData>>(json));

            while (!task.IsCompleted) yield return null;

            if (task.Exception != null)
            {
                onError?.Invoke($"JSON Parse: {task.Exception.GetBaseException().Message}");
                yield break;
            }

            onReady?.Invoke(task.Result);
        }
    }
}