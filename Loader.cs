using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ROR2_Pusky
{
    public static class Loader
    {
        private static GameObject loadObj;
        private static void unloadObj()
        {
            GameObject.Destroy(Loader.loadObj);
        }

        public static void Init()
        {
            Loader.loadObj = new GameObject();
            Loader.loadObj.AddComponent<Hack>();
            UnityEngine.Object.DontDestroyOnLoad(Loader.loadObj);


            GameObject.DontDestroyOnLoad(Loader.loadObj);
        }

        public static void Unload()
        {
            Loader.unloadObj();
        }

    }
}
