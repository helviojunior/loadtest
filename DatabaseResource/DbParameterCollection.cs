using System;
using System.Collections.Generic;
using System.Collections;

namespace SafeTrend.Data
{
    public class DbParameterCollection : IEnumerable, IDisposable
    {
        private List<DbParameter> elements;

        public DbParameterCollection()
        {
            this.elements = new List<DbParameter>();
        }

        public DbParameter Add(String parameterName, Type type)
        {
            return Add(new DbParameter(parameterName, type));
        }

        public DbParameter Add(String parameterName, Type type, Int32 size)
        {
            return Add(new DbParameter(parameterName, type, size));
        }

        public DbParameter Add(DbParameter m)
        {
            this.elements.Add(m);
            return m;
        }

        public void Clear()
        {
            this.elements.Clear();
        }

        public IEnumerator GetEnumerator()
        {
            return new DbParameterEnumerator(this);
        }

        public void Dispose()
        {
            if (elements != null)
                elements.Clear();
            elements = null;
        }

        private class DbParameterEnumerator : IEnumerator
        {
            private int position = -1;
            private DbParameterCollection m;

            public DbParameterEnumerator(DbParameterCollection m)
            {
                this.m = m;
            }

            public bool MoveNext()
            {
                if (position < m.elements.Count - 1)
                {
                    position++;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public void Reset()
            {
                position = -1;
            }

            public object Current
            {
                get
                {
                    return m.elements[position];
                }
            }
        }
    }
}
