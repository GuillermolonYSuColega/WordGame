using UnityEngine;
using UnityEditor;
using System.IO;
using Newtonsoft.Json.Linq;

public class TranslateDataset
{
    [MenuItem("Tools/Translate Palabrario Dataset")]
    public static void TranslateJson()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "palabrario_dataset.json");
        
        if (!File.Exists(path))
        {
            Debug.LogError($"No se encontró el archivo en: {path}");
            return;
        }

        Debug.Log("Leyendo dataset...");
        string json = File.ReadAllText(path);
        JArray oldArray = JArray.Parse(json);
        JArray newArray = new JArray();

        foreach (JObject item in oldArray)
        {
            JObject newItem = new JObject();
            
            // Mapeo de claves básicas
            newItem["id"] = item["id"];
            newItem["word"] = item["palabra"];
            newItem["normalizedWord"] = item["lema"];

            // Mapeo de valores de Categoría (Para que coincida con el enum CATEGORY)
            string cat = item["categoria"]?.ToString();
            newItem["category"] = cat switch {
                "SUSTANTIVO" => "Noun",
                "VERBO" => "Verb",
                "ADJETIVO" => "Adjective",
                "ADVERBIO" => "Adverb",
                "LOCUCION" => "Locution",
                "DESCONOCIDA" => "Unknown",
                _ => cat
            };

            newItem["categorySource"] = item["categoria_fuente"];

            // Mapeo de valores de Rareza (Para que coincida con el enum RARITY)
            string rar = item["rareza"]?.ToString();
            newItem["rarity"] = rar switch {
                "COMUN" => "Common",
                "POCO_COMUN" => "Uncommon",
                "POCO COMUN" => "Uncommon",
                "POCOCOMUN" => "Uncommon",
                "RARA" => "Rare",
                "EPICA" => "Epic",
                "LEGENDARIA" => "Legendary",
                _ => rar
            };

            newItem["zipfFrequency"] = item["frecuencia_zipf"];
            newItem["includedInPool"] = item["incluida_en_pool"];
            newItem["isLocution"] = item["es_locucion"];
            newItem["regimen"] = item["regimen"]; 
            newItem["regimenSource"] = item["regimen_fuente"];
            newItem["region"] = item["region"];
            newItem["isPoetic"] = item["es_poetica"];
            newItem["shortDefinition"] = item["acepcion_corta"];
            newItem["fullDefinition"] = item["acepcion_completa"];

            newArray.Add(newItem);
        }

        Debug.Log("Guardando dataset traducido...");
        File.WriteAllText(path, newArray.ToString(Newtonsoft.Json.Formatting.Indented));
        AssetDatabase.Refresh();
        
        Debug.Log("¡Dataset actualizado con éxito!");
    }
}