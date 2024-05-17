using System.Collections.Generic;
using UnityEngine;

namespace Emerge.Home
{
    public class BidirectionalDictionaryUnique<TFirst, TSecond> : BidirectionalDictionary<TFirst, TSecond>
    {
        public override void Add(TFirst first, TSecond second)
        {
            if (ContainsKey(first) || ContainsKey(second))
            {
                Debug.LogWarning($"Entry already exists for unique map : {first} - {second}");
                return;
            }

            base.Add(first, second);
        }

        public new TSecond this[TFirst first]
        {
            get
            {
                var secondList = base[first];
                return !IsEmpty(secondList) ? secondList[0] : default(TSecond);
            }
        }

        public new TFirst this[TSecond second]
        {
            get
            {
                var secondList = base[second];
                return !IsEmpty(secondList) ? secondList[0] : default(TFirst);
            }
        }

        public new TSecond GetByFirst(TFirst first)
        {
            return this[first];
        }

        public new TFirst GetBySecond(TSecond second)
        {
            return this[second];
        }
    }
}
