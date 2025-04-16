using System;
using System.Collections.Generic;

namespace Services
{
    [Serializable]
    public class Wrapper<T>
    {
        public List<T> Items;
    }
}