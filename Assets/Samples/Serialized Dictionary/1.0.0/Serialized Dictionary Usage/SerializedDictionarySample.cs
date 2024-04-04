using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AYellowpaper.SerializedCollections
{
    public class SerializedDictionarySample : MonoBehaviour
    {
        [SerializedDictionary("Element Type", "Description")]
        public SerializedDictionary<ElementType, string> ElementDescriptions;
        
        [SerializedDictionary("Element Type", "Description")]
        public SerializedDictionary<string, string> ElementDescriptions2;
        
        public enum ElementType
        {
            Fire,
            Air,
            Earth,
            Water
        }
    }
}