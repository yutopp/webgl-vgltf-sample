using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace WebglVGltfSample
{
    public sealed class DataContainerFromJs : MonoBehaviour
    {
        readonly StringBuilder _sb = new StringBuilder();
        
        // called from index.html
        public void Add(string s)
        {
            _sb.Append(s);
        }

        // called from index.html
        public void Clear()
        {
            _sb.Clear();
        }

        public string Build()
        {
            return _sb.ToString();
        }
    }
}
