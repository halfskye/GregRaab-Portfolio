using System;
using System.Collections.Generic;

namespace Emerge.Home
{
    public class BidirectionalDictionary<TFirst, TSecond>
    {
        private readonly IDictionary<TFirst, IList<TSecond>> _firstToSecond = new Dictionary<TFirst, IList<TSecond>>();
        private readonly IDictionary<TSecond, IList<TFirst>> _secondToFirst = new Dictionary<TSecond, IList<TFirst>>();

        private static readonly IList<TFirst> EmptyFirstList = Array.Empty<TFirst>();
        protected static bool IsEmpty(IList<TFirst> first) => Equals(first, EmptyFirstList);
        private static readonly IList<TSecond> EmptySecondList = Array.Empty<TSecond>();
        protected static bool IsEmpty(IList<TSecond> second) => Equals(second, EmptySecondList);

        public bool ContainsKey(TFirst first) => _firstToSecond.ContainsKey(first);
        public bool ContainsKey(TSecond second) => _secondToFirst.ContainsKey(second);

        public virtual void Add(TFirst first, TSecond second)
        {
            if (!_firstToSecond.TryGetValue(first, out var seconds))
            {
                seconds = new List<TSecond>();
                _firstToSecond[first] = seconds;
            }
            if (!_secondToFirst.TryGetValue(second, out var firsts))
            {
                firsts = new List<TFirst>();
                _secondToFirst[second] = firsts;
            }
            seconds.Add(second);
            firsts.Add(first);
        }

        public void Remove(TFirst first)
        {
            if (_firstToSecond.TryGetValue(first, out var seconds))
            {
                foreach (var second in seconds)
                {
                    _secondToFirst[second].Remove(first);
                    if (_secondToFirst[second].Count == 0)
                    {
                        _secondToFirst.Remove(second);
                    }
                }
                _firstToSecond.Remove(first);
            }
        }

        public void Remove(TSecond second)
        {
            if (_secondToFirst.TryGetValue(second, out var firsts))
            {
                foreach (var first in firsts)
                {
                    _firstToSecond[first].Remove(second);
                    if (_firstToSecond[first].Count == 0)
                    {
                        _firstToSecond.Remove(first);
                    }
                }
                _secondToFirst.Remove(second);
            }
        }

        // Note potential ambiguity using indexers (e.g. mapping from int to int)
        // Hence the methods as well...
        public IList<TSecond> this[TFirst first] => GetByFirst(first);

        public IList<TFirst> this[TSecond second] => GetBySecond(second);

        public IList<TSecond> GetByFirst(TFirst first)
        {
            return !_firstToSecond.TryGetValue(first, out var list) ? EmptySecondList : new List<TSecond>(list);
        }

        public IList<TFirst> GetBySecond(TSecond second)
        {
            return !_secondToFirst.TryGetValue(second, out var list) ? EmptyFirstList : new List<TFirst>(list);
        }
    }
}
